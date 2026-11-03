using UnityEngine;
using TMPro;
using UnityEngine.UI;
using StarterAssets;
using System.Collections.Generic;
using UnityEngine.InputSystem;


public class UIScript : MonoBehaviour
{
    [SerializeField] private TMP_Text CoinCountText;
    public AllomancyMovement allomancy;
    public ThirdPersonController thirdPersonController;

    [Header("Pause Menu")]
    [SerializeField] private GameObject pauseMenu;
    public bool IsPauseMenuActive = false;
    [SerializeField] private Slider SensitivitySlider;
    [SerializeField] private Image QuitButton;

    [SerializeField] private List<GameObject> UIElements; //List of UI elements you can select
    public GameObject SelectedUIElement;
    [SerializeField] private Image SensSelectedBorder;
    [SerializeField] private float sliderAdjustSpeed = 0.1f; // fraction of slider range per second
    [SerializeField] private float SliderAdjustJump = 0.5f;
    [SerializeField] TextMeshProUGUI SensitivityValueText;
    private int selectedIndex = -1;
    private CursorLockMode cursorLockBeforePause;
    public Sprite QuitButtonDefault;
    public Sprite QuitButtonHighlighted;

    // Opens or closes the pause menu, freezing time, gameplay input and the cursor lock to match.
    public void TogglePauseMenu()
    {
        pauseMenu.SetActive(!pauseMenu.activeSelf);
        IsPauseMenuActive = pauseMenu.activeSelf;

        Time.timeScale = IsPauseMenuActive ? 0f : 1f;
        if (thirdPersonController != null)
            thirdPersonController.enabled = !IsPauseMenuActive;

        if (IsPauseMenuActive)
        {
            cursorLockBeforePause = Cursor.lockState;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = !UsingGamepad();
        }
        else
        {
            Cursor.lockState = cursorLockBeforePause;
            Cursor.visible = cursorLockBeforePause != CursorLockMode.Locked;

            // Avoids a jump from the A press that closed the menu.
            if (thirdPersonController != null)
            {
                StarterAssetsInputs inputs = thirdPersonController.GetComponent<StarterAssetsInputs>();
                if (inputs != null)
                    inputs.JumpInput(false);
            }
        }

        if (IsPauseMenuActive && UsingGamepad())
            SelectElement(0);
        else
            ClearSelection();
    }

    // Falls back to finding the player's controller if it wasn't assigned in the Inspector.
    void Awake()
    {
        if (thirdPersonController == null)
            thirdPersonController = FindFirstObjectByType<ThirdPersonController>();
    }

    // Resets the menu selection and shows the starting coin count.
    void Start()
    {
        ClearSelection();
        SetStartCoinCount();

        SensitivitySlider.onValueChanged.AddListener(_ =>
        {
            UpdateSensitivity();
            UpdateSensitivityValueText();
        });
        UpdateSensitivityValueText();
    }

    // While paused: handles mouse hover/click, gamepad navigation and slider adjustment.
    void Update()
    {
        if (!IsPauseMenuActive)
            return;

        Cursor.visible = !UsingGamepad();
        HandleMouseHover();

        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame &&
            IsHovered(QuitButton.rectTransform, Mouse.current.position.ReadValue()))
        {
            TogglePauseMenu();
            return;
        }

        if (Gamepad.current == null)
            return;

        Gamepad pad = Gamepad.current;

        if (pad.buttonSouth.wasPressedThisFrame && SelectedUIElement == QuitButton.gameObject)
        {
            TogglePauseMenu();
            return;
        }

        if (pad.dpad.down.wasPressedThisFrame || pad.leftStick.down.wasPressedThisFrame)
            SelectElement(selectedIndex + 1);
        if (pad.dpad.up.wasPressedThisFrame || pad.leftStick.up.wasPressedThisFrame)
            SelectElement(selectedIndex < 0 ? 0 : selectedIndex - 1);

        if (SelectedUIElement == SensitivitySlider.gameObject)
        {
            float x = Mathf.Clamp(pad.dpad.x.ReadValue() + pad.leftStick.x.ReadValue(), -1f, 1f);
            if (x != 0f)
            {
                float range = SensitivitySlider.maxValue - SensitivitySlider.minValue;
                SensitivitySlider.value += x * range * sliderAdjustSpeed * Time.unscaledDeltaTime;
                UpdateSensitivity();
            }

            if (pad.rightTrigger.wasPressedThisFrame)
                SensitivitySlider.value += SliderAdjustJump;
            if (pad.leftTrigger.wasPressedThisFrame)
                SensitivitySlider.value -= SliderAdjustJump;
        }
    }

    // Shows the player's current coin count.
    public void UpdateCoinCount()
    {
        CoinCountText.text = "Coins: " + allomancy.coinCount.ToString();
    }

    // Shows the max coin count, used at game start.
    public void SetStartCoinCount()
    {
        CoinCountText.text = "Coins: " + allomancy.maxCoinCount.ToString();
    }
    
    // Applies the slider value as the gamepad look sensitivity.
    public void UpdateSensitivity()
    {
        if (thirdPersonController != null)
            thirdPersonController.updateSensitivity(SensitivitySlider.value);
    }
    // Updates the displayed sensitivity value next to the slider.
    public void UpdateSensitivityValueText()
    {
        SensitivityValueText.text = SensitivitySlider.value.ToString("F2");
    }

    private PlayerInput playerInput;

    // True when the player's active control scheme is the gamepad.
    private bool UsingGamepad()
    {
        if (thirdPersonController == null)
            return Gamepad.current != null;

        if (playerInput == null)
            playerInput = thirdPersonController.GetComponent<PlayerInput>();
        return playerInput != null && playerInput.currentControlScheme == "Gamepad";
    }

    // Selects the UIElements entry at the given index (wrapping around the list) and updates visuals.
    private void SelectElement(int index)
    {
        if (UIElements.Count == 0)
            return;

        selectedIndex = ((index % UIElements.Count) + UIElements.Count) % UIElements.Count;
        SelectedUIElement = UIElements[selectedIndex];
        UpdateSelectionVisuals();
    }

    // Selects the slider or Quit button under the mouse, or clears the selection.
    // Only reacts when the mouse moves so it doesn't override gamepad selection.
    private void HandleMouseHover()
    {
        if (Mouse.current == null || Mouse.current.delta.ReadValue() == Vector2.zero)
            return;

        Vector2 mousePosition = Mouse.current.position.ReadValue();

        if (IsHovered((RectTransform)SensitivitySlider.transform, mousePosition))
            SelectElement(UIElements.IndexOf(SensitivitySlider.gameObject));
        else if (IsHovered(QuitButton.rectTransform, mousePosition))
            SelectElement(UIElements.IndexOf(QuitButton.gameObject));
        else
            ClearSelection();
    }

    // True if the screen point is inside the given UI rect.
    private static bool IsHovered(RectTransform rect, Vector2 screenPoint)
    {
        Canvas canvas = rect.GetComponentInParent<Canvas>();
        Camera cam = canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay ? canvas.worldCamera : null;
        return RectTransformUtility.RectangleContainsScreenPoint(rect, screenPoint, cam);
    }

    // Deselects everything and resets the visuals.
    private void ClearSelection()
    {
        selectedIndex = -1;
        SelectedUIElement = null;
        UpdateSelectionVisuals();
    }

    // Shows the slider border and Quit button highlight for whichever element is selected.
    private void UpdateSelectionVisuals()
    {
        SensSelectedBorder.enabled = SelectedUIElement != null && SelectedUIElement == SensitivitySlider.gameObject;

        bool quitSelected = SelectedUIElement != null && SelectedUIElement == QuitButton.gameObject;
        Sprite quitSprite = quitSelected ? QuitButtonHighlighted : QuitButtonDefault;
        if (quitSprite != null)
            QuitButton.sprite = quitSprite;
    }

}
