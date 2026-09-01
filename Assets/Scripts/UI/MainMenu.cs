using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MainMenu : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private TMP_InputField roomCodeInput;
    [SerializeField] private TMP_Text roomCodeDisplay;
    [SerializeField] private Button createRoomButton;
    [SerializeField] private Button joinRoomButton;

    private void Start()
    {
        if (createRoomButton != null)
            createRoomButton.onClick.AddListener(OnCreateRoomClicked);

        if (joinRoomButton != null)
            joinRoomButton.onClick.AddListener(OnJoinRoomClicked);
    }

    private async void OnCreateRoomClicked()
    {
        SetButtonsInteractable(false);

        string joinCode = await NetworkService.Instance.CreateRoomAsync();

        if (!string.IsNullOrEmpty(joinCode))
        {
            Debug.Log($"Комната успешно создана с кодом: {joinCode}");
            if (roomCodeDisplay != null)
                roomCodeDisplay.text = $"Код: {joinCode}";
        }
        else
        {
            SetButtonsInteractable(true);
        }
    } 

    private async void OnJoinRoomClicked()
    {
        string code = roomCodeInput != null ? roomCodeInput.text.Trim() : "";
        SetButtonsInteractable(false);

        bool success = await NetworkService.Instance.JoinRoomAsync(code);

        if (!success)
        {
            SetButtonsInteractable(true);
        }
    }

    private void SetButtonsInteractable(bool interactable)
    {
        if (createRoomButton != null) createRoomButton.interactable = interactable;
        if (joinRoomButton != null) joinRoomButton.interactable = interactable;
    }

    private void OnDestroy()
    {
        if (createRoomButton != null)
            createRoomButton.onClick.RemoveListener(OnCreateRoomClicked);

        if (joinRoomButton != null)
            joinRoomButton.onClick.RemoveListener(OnJoinRoomClicked);
    }
}