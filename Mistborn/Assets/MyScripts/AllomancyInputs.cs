using UnityEngine;
using UnityEngine.InputSystem;
public class AllomancyInputs : MonoBehaviour
{
    [SerializeField] private AllomancyMovement allomancy;
    [SerializeField] private UIScript uiScript;



    void Update()
    {
        // Captured before toggling so the press that closes the menu isn't also used as a gameplay input.
        bool wasPaused = uiScript != null && uiScript.IsPauseMenuActive;

        bool steelPushHeld = Mouse.current != null && Mouse.current.leftButton.isPressed;
        bool ironPullPressed = Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame;
        bool ironPullReleased = Mouse.current != null && Mouse.current.rightButton.wasReleasedThisFrame;
        bool spawnCoinPressed = Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
        bool suckCoinsPressed = Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame;
        bool togglePauseMenuPressed = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;

        if (Gamepad.current != null)
        {
            steelPushHeld |= Gamepad.current.rightTrigger.isPressed;
            ironPullPressed |= Gamepad.current.leftTrigger.wasPressedThisFrame;
            ironPullReleased |= Gamepad.current.leftTrigger.wasReleasedThisFrame;
            spawnCoinPressed |= Gamepad.current.buttonNorth.wasPressedThisFrame;
            suckCoinsPressed |= Gamepad.current.buttonEast.wasPressedThisFrame;
            togglePauseMenuPressed |= Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame;

            if (wasPaused)
                togglePauseMenuPressed |= Gamepad.current.buttonEast.wasPressedThisFrame;
        }

        if (togglePauseMenuPressed)
        {
            TryTogglePauseMenu();
        }

        // Release is still handled while paused so a pull doesn't stay stuck on resume.
        if (ironPullReleased && allomancy != null)
            allomancy.StopIronPull();

        if (wasPaused || (uiScript != null && uiScript.IsPauseMenuActive))
            return;

        if (steelPushHeld)
        {
            TrySteelPush();
        }

        if (ironPullPressed)
            TryIronPull();

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

    void TryTogglePauseMenu()
    {
        if (uiScript != null)
            uiScript.TogglePauseMenu();
    }

}