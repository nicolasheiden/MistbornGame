using UnityEngine;

public class CoinScript : MonoBehaviour
{
    void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag("Player");
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
}
