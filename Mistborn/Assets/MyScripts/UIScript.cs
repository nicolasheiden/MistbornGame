using UnityEngine;
using TMPro;


public class UIScript : MonoBehaviour
{
    [SerializeField] private TMP_Text CoinCountText;
    public AllomancyMovement allomancy;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        UpdateCoinCount(allomancy.maxCoinCount); // Initialize the coin count display at the start
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void UpdateCoinCount(int newCoinCount)
    {
        CoinCountText.text = "Coins: " + newCoinCount.ToString();
    }
    

}
