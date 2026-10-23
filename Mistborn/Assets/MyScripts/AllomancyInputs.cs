using UnityEngine;
using UnityEngine.InputSystem;
public class AllomancyInputs : MonoBehaviour
{
    [SerializeField] private AllomancyMovement allomancy;
    [SerializeField] private UIScript uiScript;



    void Update()
    {
        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            TrySteelPush();
        }

        if (Mouse.current == null)
        {
            if (allomancy != null)
                allomancy.StopIronPull();
        }
        else
        {
            if (Mouse.current.rightButton.wasPressedThisFrame)
                TryIronPull();

            if (Mouse.current.rightButton.wasReleasedThisFrame && allomancy != null)
                allomancy.StopIronPull();
        }
        //duralumin activation here

        if(Keyboard.current.eKey.wasPressedThisFrame)
        {
            TrySpawnCoin();
            uiScript.UpdateCoinCount();
        }

        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            allomancy.SuckCoins();
        }
        
    }


    void TrySteelPush()
    {
        if (allomancy != null && allomancy.selectedMetalObject != null)
            allomancy.SteelPush(allomancy.selectedMetalObject);
    }

    void TryIronPull()
    {
        if (allomancy == null)
            return;

        if (allomancy.PullObject != null)
            allomancy.IronPull(allomancy.PullObject);
        else
            allomancy.StopIronPull();
    }

    void TryActivateDuralumin()
    {
        //allomancy.ActivateDuralumin();
    }

    void TrySpawnCoin()
    {
        allomancy.SpawnCoin();
    }

}