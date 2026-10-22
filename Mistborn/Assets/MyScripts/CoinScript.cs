using UnityEngine;

public class CoinScript : MonoBehaviour
{
    public float flySpeed = 5f; // Speed at which the coin flies towards the player
    private Transform playerTransform;
    private GameObject player;
    public bool suckActivated = false;
    private UIScript uIScript;
    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player");
        if (player == null)
        {
            Debug.LogError("Cannot find a GameObject tagged 'Player'.", this);
            return;
        }

        AllomancyMovement movement = player.GetComponent<AllomancyMovement>();
        if (movement == null || movement.AllomanticLineOrigin == null)
        {
            Debug.LogError("Player needs an AllomancyMovement with its LinePoint assigned.", player);
            return;
        }

        playerTransform = movement.AllomanticLineOrigin;

        uIScript = FindAnyObjectByType<UIScript>();
        if (uIScript == null)
        {
            Debug.LogError("Cannot find UIScript in the active scene.", this);
        }
    }

    void Update()
    {
        if (playerTransform != null && suckActivated)
        {
            FlytoPlayer(playerTransform);
        }
    }

    public void FlytoPlayer(Transform playerTransform)
    {
        Vector3 direction = (playerTransform.position - transform.position).normalized;
        transform.position += direction * flySpeed * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!suckActivated || other.isTrigger || !other.CompareTag("Player"))
            return;

        AllomancyMovement movement = other.GetComponentInParent<AllomancyMovement>();
        if (movement == null)
        {
            Debug.LogError("Player is missing AllomancyMovement.", other);
            return;
        }

        movement.AddCoin(1);
        if (uIScript != null)
            uIScript.UpdateCoinCount();

        Destroy(gameObject);
    }

    void OnDestroy()
    {
        uIScript.UpdateCoinCount();
    }
}
