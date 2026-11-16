using UnityEngine;

// Builds a falling-ash particle system and keeps it centred on the player.
// Add this to any GameObject (for example the player); the shader field fills itself in on Reset.
public class AshfallEffect : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] CharacterController player;
    [SerializeField] Shader shader;

    [Header("Volume")]
    [SerializeField] Vector3 volumeSize = new Vector3(40f, 14f, 40f); // meters, centred horizontally on the player
    [SerializeField] int maxParticles = 10000;
    [SerializeField] float emissionRate = 200f;       // per second, even when standing still
    [SerializeField] float emissionPerMeter = 30f;    // extra per meter travelled, keeps ash ahead when moving fast
    [SerializeField] Vector2 lifetime = new Vector2(8f, 14f);

    [Header("Fast movement")]
    [SerializeField] float leadTime = 0.7f;           // spawn volume shifts ahead by velocity * leadTime
    [SerializeField] float maxLead = 14f;             // meters
    [SerializeField] float velocitySmoothing = 6f;

    [Header("Flakes")]
    [SerializeField] Vector2 flakeSize = new Vector2(0.03f, 0.1f);
    [SerializeField] Color ashColorA = new Color(0.1f, 0.09f, 0.085f, 0.9f);
    [SerializeField] Color ashColorB = new Color(0.32f, 0.3f, 0.28f, 0.7f);
    [SerializeField] float softParticleDistance = 0.3f;

    [Header("Motion")]
    [SerializeField] float fallSpeed = 0.6f;
    [SerializeField] Vector3 wind = new Vector3(0.5f, 0f, 0.2f);
    [SerializeField] float flutterStrength = 0.4f;
    [SerializeField] float flutterFrequency = 0.3f;
    [SerializeField] float spin = 1.5f;
    [SerializeField] bool killOnGround = true;

    ParticleSystem ps;
    Material material;
    Vector3 smoothedVelocity;

#if UNITY_EDITOR
    void Reset()
    {
        shader = Shader.Find("Custom/AshParticle");
    }
#endif

    void Awake()
    {
        if (player == null) player = GetComponentInParent<CharacterController>();
        if (player == null) player = FindFirstObjectByType<CharacterController>();
        if (shader == null) shader = Shader.Find("Custom/AshParticle");

        var go = new GameObject("AshfallParticles");
        go.transform.SetParent(transform, false);
        ps = go.AddComponent<ParticleSystem>();
        Configure();
    }

    void LateUpdate()
    {
        if (player == null || ps == null) return;
        // Stay world-aligned so the volume doesn't rotate with the player, and lead the player
        // so fresh ash is spawned where they're heading.
        Vector3 velocity = player.velocity;
        velocity.y = 0f;
        smoothedVelocity = Vector3.Lerp(smoothedVelocity, velocity, 1f - Mathf.Exp(-velocitySmoothing * Time.deltaTime));
        Vector3 lead = Vector3.ClampMagnitude(smoothedVelocity * leadTime, maxLead);
        ps.transform.SetPositionAndRotation(player.transform.position + lead, Quaternion.identity);
    }

    void OnDestroy()
    {
        if (material != null) Destroy(material);
    }

    void Configure()
    {
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = ps.main;
        main.duration = 10f;
        main.loop = true;
        main.prewarm = true;
        main.playOnAwake = true;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = maxParticles;
        main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime.x, lifetime.y);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(flakeSize.x, flakeSize.y);
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.startColor = new ParticleSystem.MinMaxGradient(ashColorA, ashColorB);
        main.gravityModifier = 0f;

        var emission = ps.emission;
        emission.rateOverTime = emissionRate;
        emission.rateOverDistance = emissionPerMeter;

        var shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = volumeSize;
        shape.position = new Vector3(0f, volumeSize.y * 0.5f, 0f);
        shape.rotation = Vector3.zero;

        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = new ParticleSystem.MinMaxCurve(wind.x * 0.5f, wind.x * 1.5f);
        velocity.y = new ParticleSystem.MinMaxCurve(-fallSpeed * 1.3f, -fallSpeed * 0.7f);
        velocity.z = new ParticleSystem.MinMaxCurve(wind.z * 0.5f, wind.z * 1.5f);

        var noise = ps.noise;
        noise.enabled = true;
        noise.strength = flutterStrength;
        noise.frequency = flutterFrequency;
        noise.scrollSpeed = 0.3f;
        noise.damping = true;
        noise.octaveCount = 1;
        noise.quality = ParticleSystemNoiseQuality.Medium;

        var rotation = ps.rotationOverLifetime;
        rotation.enabled = true;
        rotation.z = new ParticleSystem.MinMaxCurve(-spin, spin);

        var fade = new Gradient();
        fade.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.08f), new GradientAlphaKey(1f, 0.85f), new GradientAlphaKey(0f, 1f) });
        var colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(fade);

        var collision = ps.collision;
        collision.enabled = killOnGround;
        if (killOnGround)
        {
            collision.type = ParticleSystemCollisionType.World;
            collision.mode = ParticleSystemCollisionMode.Collision3D;
            collision.quality = ParticleSystemCollisionQuality.Low;
            collision.lifetimeLoss = 1f;
            collision.radiusScale = 0.5f;
        }

        var psRenderer = ps.GetComponent<ParticleSystemRenderer>();
        psRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        psRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        psRenderer.receiveShadows = false;
        if (shader != null)
        {
            material = new Material(shader) { name = "AshParticle (runtime)" };
            material.SetFloat("_SoftDistance", softParticleDistance);
            psRenderer.sharedMaterial = material;
        }
        else
        {
            Debug.LogWarning("AshfallEffect: shader 'Custom/AshParticle' not found.", this);
        }

        if (player != null)
            ps.transform.SetPositionAndRotation(player.transform.position, Quaternion.identity);
        ps.Play();
    }
}
