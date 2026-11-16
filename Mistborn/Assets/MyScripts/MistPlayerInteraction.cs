using UnityEngine;

// Publishes the player's position and velocity to MistVolume.shader as global shader vectors.
public class MistPlayerInteraction : MonoBehaviour
{
    [SerializeField] CharacterController player;
    [SerializeField] float heightOffset = 1f;       // aim the effect at the player's torso
    [SerializeField] float velocitySmoothing = 5f;  // lower = longer-lasting wake

    static readonly int PosId = Shader.PropertyToID("_MistPlayerPos");
    static readonly int VelId = Shader.PropertyToID("_MistPlayerVel");

    Vector3 smoothedVelocity;

    void Awake()
    {
        if (player == null) player = GetComponentInParent<CharacterController>();
        if (player == null) player = FindFirstObjectByType<CharacterController>();
    }

    void LateUpdate()
    {
        if (player == null) return;

        smoothedVelocity = Vector3.Lerp(smoothedVelocity, player.velocity, 1f - Mathf.Exp(-velocitySmoothing * Time.deltaTime));
        Vector3 pos = player.transform.position + Vector3.up * heightOffset;

        Shader.SetGlobalVector(PosId, pos);
        Shader.SetGlobalVector(VelId, new Vector4(smoothedVelocity.x, smoothedVelocity.y, smoothedVelocity.z, 1f));
    }

    void OnDisable()
    {
        Shader.SetGlobalVector(VelId, Vector4.zero);
    }
}
