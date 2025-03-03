using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoginLobbyUI : MonoBehaviour
{
    [SerializeField] private TMP_InputField _playerName;
    [SerializeField] private Button _startButton;

    private void Hide()
    {
        gameObject.SetActive(false);
    }

    private void Awake()
    {
        _startButton.onClick.AddListener(() =>
        {
            LobbyManager.Instance.Authenticate(_playerName.text);
            Hide();
        });

        _startButton.interactable = false;
    }

    private void Update()
    {
        if (_playerName.text != string.Empty)
        {
            _startButton.interactable = true;
        }
    }
}
