using UnityEngine;

[RequireComponent(typeof(Cloth))]
public class MistcloakWind : MonoBehaviour
{
    [Header("Player")]
    [SerializeField] CharacterController player;
    [SerializeField] float maxSpeed = 20f;          // speed at which the speed effect is maxed out

    [Header("Ambient wind")]
    [SerializeField] Vector3 ambientWindDirection = new Vector3(1f, 0f, 0.3f);
    [SerializeField] float ambientStrength = 3f;
    [SerializeField] float gustStrength = 2f;
    [SerializeField] float gustSpeed = 0.6f;

    [Header("Speed wind")]
    [SerializeField] float speedWindMultiplier = 1.2f; // acceleration per m/s, pointing opposite to movement
    [SerializeField] float baseTurbulence = 10f;
    [SerializeField] float speedTurbulence = 60f;      // extra turbulence at max speed
    [SerializeField] float smoothing = 6f;

    [Header("Lift")]
    [SerializeField] float liftBase = 4f;              // constant upward acceleration (gravity is ~9.81)
    [SerializeField] float liftSpeed = 8f;             // extra upward acceleration at max speed

    [SerializeField] float velocitySmoothing = 4f;     // lower = calmer reaction to speed changes
    [SerializeField] float maxAcceleration = 25f;      // hard cap on the wind applied to the cloth

    Cloth cloth;
    Vector3 smoothedAcceleration;
    Vector3 smoothedVelocity;
    float smoothedTurbulence;

    void Awake()
    {
        cloth = GetComponent<Cloth>();
        if (player == null) player = GetComponentInParent<CharacterController>();
    }

    void Update()
    {
        Vector3 rawVelocity = player != null ? player.velocity : Vector3.zero;
        smoothedVelocity = Vector3.Lerp(smoothedVelocity, rawVelocity, 1f - Mathf.Exp(-velocitySmoothing * Time.deltaTime));
        Vector3 velocity = smoothedVelocity;
        float speed01 = Mathf.Clamp01(velocity.magnitude / maxSpeed);

        float noise = Mathf.PerlinNoise(Time.time * gustSpeed, 0f);
        float ambient = ambientStrength + (noise - 0.5f) * 2f * gustStrength;

        // Headwind: air pushes the cloak against the direction of travel.
        Vector3 target = ambientWindDirection.normalized * ambient
                       - velocity * speedWindMultiplier
                       + Vector3.up * (liftBase + liftSpeed * speed01);
        float blend = 1f - Mathf.Exp(-smoothing * Time.deltaTime);
        smoothedAcceleration = Vector3.ClampMagnitude(Vector3.Lerp(smoothedAcceleration, target, blend), maxAcceleration);

        float targetTurbulence = (baseTurbulence + speedTurbulence * speed01) * (0.5f + noise);
        smoothedTurbulence = Mathf.Lerp(smoothedTurbulence, targetTurbulence, blend);

        cloth.externalAcceleration = smoothedAcceleration;
        cloth.randomAcceleration = new Vector3(1f, 0.5f, 1f) * smoothedTurbulence;
    }
}
