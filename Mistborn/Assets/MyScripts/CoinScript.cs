using UnityEngine;

public class CoinScript : MonoBehaviour
{
    public float flySpeed = 5f; // Speed at which the coin flies towards the player
    private Transform playerTransform;
    private GameObject player;
    public bool suckActivated = false;
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogError("Cannot ignore coin-player collision: no GameObject tagged 'Player' was found.", this);
            return;
        }

        CharacterController playerCollider = player.GetComponent<CharacterController>();
        if (playerCollider == null)
        {
            Debug.LogError("Cannot ignore coin-player collision: the Player needs a CharacterController.", player);
            return;
        }

        Collider[] coinColliders = GetComponentsInChildren<Collider>();
        foreach (Collider coinCollider in coinColliders)
        {
            Physics.IgnoreCollision(playerCollider, coinCollider);
        }
    }

    void Update()
    {
        if (player != null && suckActivated)
        {
            FlytoPlayer(player.transform);
        }
    }

    public void FlytoPlayer(Transform playerTransform)
    {
        Vector3 direction = (playerTransform.position - transform.position).normalized;
        transform.position += direction * flySpeed * Time.deltaTime;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Destroy(this);
        }
    }

    void OnDestroy()
    {
        print("coin gone");
    }
}
