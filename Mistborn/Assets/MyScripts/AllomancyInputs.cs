using UnityEngine;
using UnityEngine.InputSystem;
public class AllomancyInputs : MonoBehaviour
{
    [SerializeField] private AllomancyMovement allomancy;
    [SerializeField] private UIScript uiScript;



    void Update()
    {
        bool steelPushHeld = Mouse.current != null && Mouse.current.leftButton.isPressed;
        bool ironPullPressed = Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame;
        bool ironPullReleased = Mouse.current != null && Mouse.current.rightButton.wasReleasedThisFrame;
        bool spawnCoinPressed = Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
        bool suckCoinsPressed = Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;

        if (Gamepad.current != null)
        {
            steelPushHeld |= Gamepad.current.rightTrigger.isPressed;
            ironPullPressed |= Gamepad.current.leftTrigger.wasPressedThisFrame;
            ironPullReleased |= Gamepad.current.leftTrigger.wasReleasedThisFrame;
            spawnCoinPressed |= Gamepad.current.buttonNorth.wasPressedThisFrame;
        }

        if (steelPushHeld)
        {
            TrySteelPush();
        }

        if (ironPullPressed)
            TryIronPull();

        if (ironPullReleased && allomancy != null)
            allomancy.StopIronPull();

        //duralumin activation here

        if (spawnCoinPressed)
        {
            TrySpawnCoin();
            uiScript.UpdateCoinCount();
        }

        if (suckCoinsPressed)
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