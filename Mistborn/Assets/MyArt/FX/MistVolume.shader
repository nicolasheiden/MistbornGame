Shader "Custom/MistVolume"
{
    Properties
    {
        _MistColor("Mist Color", Color) = (0.75, 0.78, 0.85, 1)
        _ShadowColor("Shadow/Ambient Color", Color) = (0.35, 0.38, 0.48, 1)
        _Density("Density (per meter)", Range(0, 2)) = 0.15
        _Coverage("Coverage (higher = more gaps)", Range(0, 0.9)) = 0.45
        _NoiseScale("Noise Scale", Float) = 0.3
        _Wind("Wind Velocity (xyz)", Vector) = (0.1, 0.02, 0.05, 0)
        _HeightFalloff("Height Falloff (meters)", Range(0.5, 50)) = 4
        _MaxDistance("Max Mist Distance (meters)", Range(5, 300)) = 60
        _EdgeSoftness("Box Edge Fade (meters)", Range(0.1, 30)) = 4
        _Steps("Raymarch Steps", Range(8, 96)) = 40

        [Header(Player Interaction)]
        _PlayerRadius("Influence Radius (meters)", Range(0.5, 15)) = 4
        _PlayerClear("Parting Strength", Range(0, 1)) = 0.7
        _PlayerSwirl("Swirl (radians)", Range(0, 8)) = 3
        _PlayerDrag("Wake Drag (seconds of travel)", Range(0, 2)) = 0.6
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            // Draw back faces so the volume still renders when the camera is inside the box.
            // Occlusion is done manually against the scene depth texture.
            Cull Front
            ZWrite Off
            ZTest Always
            Blend One OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Random.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _MistColor;
                half4 _ShadowColor;
                float _Density;
                float _Coverage;
                float _NoiseScale;
                float4 _Wind;
                float _HeightFalloff;
                float _MaxDistance;
                float _EdgeSoftness;
                float _Steps;
                float _PlayerRadius;
                float _PlayerClear;
                float _PlayerSwirl;
                float _PlayerDrag;
            CBUFFER_END

            // Set globally by MistPlayerInteraction.cs. _MistPlayerVel.w is 1 while the script is active.
            float4 _MistPlayerPos;
            float4 _MistPlayerVel;

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionHCS = TransformWorldToHClip(OUT.positionWS);
                return OUT;
            }

            float Hash(float3 p)
            {
                p = frac(p * 0.3183099 + 0.1);
                p *= 17.0;
                return frac(p.x * p.y * p.z * (p.x + p.y + p.z));
            }

            float ValueNoise(float3 x)
            {
                float3 i = floor(x);
                float3 f = frac(x);
                f = f * f * (3.0 - 2.0 * f);

                return lerp(
                    lerp(lerp(Hash(i + float3(0, 0, 0)), Hash(i + float3(1, 0, 0)), f.x),
                         lerp(Hash(i + float3(0, 1, 0)), Hash(i + float3(1, 1, 0)), f.x), f.y),
                    lerp(lerp(Hash(i + float3(0, 0, 1)), Hash(i + float3(1, 0, 1)), f.x),
                         lerp(Hash(i + float3(0, 1, 1)), Hash(i + float3(1, 1, 1)), f.x), f.y),
                    f.z);
            }

            float Fbm(float3 p)
            {
                float v = 0.0;
                float a = 0.5;
                for (int i = 0; i < 4; i++)
                {
                    v += a * ValueNoise(p);
                    p = p * 2.0 + 17.0;
                    a *= 0.5;
                }
                return v;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 camWS = _WorldSpaceCameraPos;
                float3 dirWS = normalize(IN.positionWS - camWS);

                // Object space is a unit cube (-0.5..0.5). Keeping rd un-normalized means the
                // ray parameter t is the same distance in world and object space.
                float3 ro = TransformWorldToObject(camWS);
                float3 rd = TransformWorldToObjectDir(dirWS, false);

                float3 invD = 1.0 / rd;
                float3 t0 = (-0.5 - ro) * invD;
                float3 t1 = (0.5 - ro) * invD;
                float3 tmin3 = min(t0, t1);
                float3 tmax3 = max(t0, t1);
                float tNear = max(max(max(tmin3.x, tmin3.y), tmin3.z), 0.0);
                float tFar = min(min(tmax3.x, tmax3.y), tmax3.z);

                // Stop the ray at opaque geometry so mist doesn't draw through walls.
                float2 screenUV = IN.positionHCS.xy / _ScaledScreenParams.xy;
                float rawDepth = SampleSceneDepth(screenUV);
                float eyeDepth = LinearEyeDepth(rawDepth, _ZBufferParams);
                float3 camForward = -UNITY_MATRIX_V[2].xyz;
                float sceneDist = eyeDepth / max(dot(dirWS, camForward), 1e-4);
                tFar = min(tFar, sceneDist);
                // Cap the march length so huge boxes don't saturate into flat opaque fog.
                tFar = min(tFar, _MaxDistance);

                if (tFar <= tNear)
                    return 0;

                int steps = (int)_Steps;
                float stepSize = (tFar - tNear) / steps;

                // Per-pixel jitter hides banding from the fixed step count.
                float jitter = InterleavedGradientNoise(IN.positionHCS.xy, 0);
                float t = tNear + stepSize * jitter;

                Light mainLight = GetMainLight();
                float cosTheta = dot(dirWS, mainLight.direction);
                // Henyey-Greenstein-ish forward scattering: brighter looking toward the light.
                float g = 0.5;
                float phase = (1 - g * g) / pow(1 + g * g - 2 * g * cosTheta, 1.5);
                phase *= 0.25;

                float3 wind = _Wind.xyz * _Time.y;
                float3 boxScale = float3(length(unity_ObjectToWorld._m00_m10_m20),
                                         length(unity_ObjectToWorld._m01_m11_m21),
                                         length(unity_ObjectToWorld._m02_m12_m22));
                float3 color = 0;
                float transmittance = 1.0;

                for (int s = 0; s < steps; s++)
                {
                    float3 posOS = ro + rd * t;
                    float3 posWS = camWS + dirWS * t;

                    // Player influence: 1 at the player, 0 at _PlayerRadius. Vertical distance counts
                    // half so the effect reaches a bit higher than it does sideways.
                    float3 toP = posWS - _MistPlayerPos.xyz;
                    float influence = (1.0 - smoothstep(0.0, _PlayerRadius, length(toP * float3(1, 0.5, 1)))) * _MistPlayerVel.w;
                    float speed01 = saturate(length(_MistPlayerVel.xyz) / 8.0);

                    // Swirl the sample point around the player and drag it along the velocity,
                    // so the noise pattern is stirred up and pulled into a wake.
                    float angle = influence * influence * _PlayerSwirl * (0.4 + speed01);
                    float sn, cs;
                    sincos(angle, sn, cs);
                    float3 samplePos = _MistPlayerPos.xyz + float3(toP.x * cs - toP.z * sn, toP.y, toP.x * sn + toP.z * cs);
                    samplePos -= _MistPlayerVel.xyz * (_PlayerDrag * influence);

                    float n = Fbm(samplePos * _NoiseScale + wind);
                    float density = saturate((n - _Coverage) * 2.5);

                    // Mist parts around the player.
                    density *= 1.0 - influence * _PlayerClear;

                    // Fade toward the box faces over _EdgeSoftness meters so the cube is never visible.
                    float3 q = saturate((0.5 - abs(posOS)) * boxScale / _EdgeSoftness);
                    q = q * q * (3.0 - 2.0 * q);
                    density *= q.x * q.y * q.z;

                    // Thickest at the bottom face of the box, decaying upward in meters.
                    float heightAboveBottom = (posOS.y + 0.5) * boxScale.y;
                    density *= exp(-heightAboveBottom / _HeightFalloff);

                    float sigma = density * _Density;
                    float absorb = 1.0 - exp(-sigma * stepSize);

                    float3 lit = lerp(_ShadowColor.rgb, _MistColor.rgb * mainLight.color, saturate(0.5 + phase));
                    color += transmittance * absorb * lit;
                    transmittance *= 1.0 - absorb;

                    if (transmittance < 0.01)
                        break;

                    t += stepSize;
                }

                // Premultiplied alpha (Blend One OneMinusSrcAlpha)
                return half4(color, 1.0 - transmittance);
            }
            ENDHLSL
        }
    }
}
