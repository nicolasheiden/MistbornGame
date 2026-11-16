Shader "Custom/AshParticle"
{
    Properties
    {
        _Color("Ash Color", Color) = (0.12, 0.11, 0.1, 0.8)
        _Stretch("Flake Elongation", Range(1, 3)) = 1.6
        _SoftDistance("Soft Particle Distance (m)", Range(0.01, 2)) = 0.3
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" "IgnoreProjector" = "True" }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _Stretch;
                float _SoftDistance;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 screenPos : TEXCOORD1;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.screenPos = ComputeScreenPos(OUT.positionHCS);
                OUT.color = IN.color;
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                // Procedural soft, elongated flake; rotation and size variety come from the particle system.
                float2 p = (IN.uv * 2.0 - 1.0) * float2(_Stretch, 1.0);
                float shape = 1.0 - smoothstep(0.35, 1.0, length(p));

                float2 screenUV = IN.screenPos.xy / IN.screenPos.w;
                float sceneEyeDepth = LinearEyeDepth(SampleSceneDepth(screenUV), _ZBufferParams);
                float softFade = saturate((sceneEyeDepth - IN.screenPos.w) / _SoftDistance);

                half4 col = _Color * IN.color;
                col.a *= shape * softFade;
                return col;
            }
            ENDHLSL
        }
    }
}
