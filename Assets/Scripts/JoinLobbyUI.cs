using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class JoinLobbyUI : MonoBehaviour
{
    public static JoinLobbyUI Instance {  get; private set; }

    [SerializeField] private TMP_InputField _joinCodeInput;
    [SerializeField] private Button _joinButton;

    private void Awake()
    {
        Instance = this;

        _joinButton.onClick.AddListener(() =>
        {
            LobbyManager.Instance.JoinLobbyByCode(_joinCodeInput.text);
            Hide();
        });

        Hide();
    }

    private void Hide()
    {
        gameObject.SetActive(false);
    }

    public void Show()
    {
        gameObject.SetActive(true);
        _joinCodeInput.text = string.Empty;
    }
}
