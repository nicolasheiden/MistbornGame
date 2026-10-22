using UnityEngine;
using TMPro;


public class UIScript : MonoBehaviour
{
    [SerializeField] private TMP_Text CoinCountText;
    public AllomancyMovement allomancy;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
        SetStartCoinCount();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void UpdateCoinCount()
    {
        CoinCountText.text = "Coins: " + allomancy.coinCount.ToString();
    }

    public void SetStartCoinCount()
    {
         CoinCountText.text = "Coins: " + allomancy.maxCoinCount.ToString();
    }
    

}
