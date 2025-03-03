using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class LobbyTemplateUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _lobbyNameText;
    [SerializeField] private TextMeshProUGUI _playersText;

    private Lobby _lobby;

    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(() => {
            LobbyManager.Instance.JoinLobby(_lobby);
        });
    }

    public void UpdateLobby(Lobby lobby)
    {
        _lobby = lobby;

        _lobbyNameText.text = lobby.Name;
        _playersText.text = lobby.Players.Count + "/" + lobby.MaxPlayers;
    }
}
