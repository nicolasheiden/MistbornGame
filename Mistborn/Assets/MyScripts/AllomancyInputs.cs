using UnityEngine;
using UnityEngine.InputSystem;
public class AllomancyInputs : MonoBehaviour
{
    [SerializeField] private AllomancyMovement allomancy;



    void Update()
    {
        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            TrySteelPush();
        }

        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
        {
            TryIronPull();
        }
        //duralumin activation here
        
    }


    void TrySteelPush()
    {
        if (allomancy != null && allomancy.selectedMetalObject != null)
            allomancy.SteelPush(allomancy.selectedMetalObject);
    }

    void TryIronPull()
    {
        //allomancy.IronPull(allomancy.selectedMetalObject);
    }

    void TryActivateDuralumin()
    {
        //allomancy.ActivateDuralumin();
    }


}