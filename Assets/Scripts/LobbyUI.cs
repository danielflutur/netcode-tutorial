using TMPro;
using Unity.Services.Authentication;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUI : MonoBehaviour
{
    public static LobbyUI Instance { get; private set; }

    [SerializeField] private Transform _playerTemplate;
    [SerializeField] private Transform _container;
    [SerializeField] private TextMeshProUGUI _lobbyNameText;
    [SerializeField] private TextMeshProUGUI _playerCountText;
    [SerializeField] private Button _leaveLobbyButton;
    [SerializeField] private Button _readyButton;

    private void Awake()
    {
        Instance = this;

        _playerTemplate.gameObject.SetActive(false);

        _leaveLobbyButton.onClick.AddListener(() => {
            LobbyManager.Instance.LeaveLobby();
        });

        _readyButton.onClick.AddListener(() => {
            LobbyManager.Instance.StartGame();
        });
    }

    private void Start()
    {
        LobbyManager.Instance.OnJoinedLobby += UpdateLobby_Event;
        LobbyManager.Instance.OnJoinedLobbyUpdate += UpdateLobby_Event;
        LobbyManager.Instance.OnLeftLobby += LobbyManager_OnLeftLobby;
        LobbyManager.Instance.OnKickedFromLobby += LobbyManager_OnLeftLobby;

        Hide();
    }

    private void LobbyManager_OnLeftLobby(object sender, System.EventArgs e)
    {
        ClearLobby();
        Hide();
    }

    private void UpdateLobby_Event(object sender, LobbyManager.LobbyEventArgs e)
    {
        UpdateLobby();
    }

    private void UpdateLobby()
    {
        UpdateLobby(LobbyManager.Instance.GetJoinedLobby());
    }

    private void UpdateLobby(Lobby lobby)
    {
        ClearLobby();

        foreach (Player player in lobby.Players)
        {
            Transform playerTransform = Instantiate(_playerTemplate, _container);
            playerTransform.gameObject.SetActive(true);
            var playerTemplateUI = playerTransform.GetComponent<PlayerTemplateUI>();

            playerTemplateUI.SetKickPlayerButtonVisible(
                LobbyManager.Instance.IsLobbyHost() &&
                player.Id != AuthenticationService.Instance.PlayerId
            );

            playerTemplateUI.UpdatePlayer(player);
        }

        _lobbyNameText.text = lobby.Name;
        _playerCountText.text = lobby.Players.Count + "/" + lobby.MaxPlayers;

        Show();
    }

    private void ClearLobby()
    {
        foreach (Transform child in _container)
        {
            if (child == _playerTemplate) continue;
            Destroy(child.gameObject);
        }
    }

    private void Hide()
    {
        gameObject.SetActive(false);
    }

    private void Show()
    {
        gameObject.SetActive(true);
     
        if (!LobbyManager.Instance.IsLobbyHost())
        {
            _readyButton.gameObject.SetActive(false);
        }
    }
}
