using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class PlayerTemplateUI : MonoBehaviour
{
    [SerializeField] private TMPro.TextMeshProUGUI _playerNameText;
    [SerializeField] private Button _kickPlayerButton;

    private Player _player;

    private void Awake()
    {
        _kickPlayerButton.onClick.AddListener(KickPlayer);
    }

    public void SetKickPlayerButtonVisible(bool visible)
    {
        _kickPlayerButton.gameObject.SetActive(visible);
    }
    public void UpdatePlayer(Player player)
    {
        _player = player;
        _playerNameText.text = player.Data[LobbyManager.KEY_PLAYER_NAME].Value;
    }

    private void KickPlayer()
    {
        if (_player != null)
        {
            LobbyManager.Instance.KickPlayer(_player.Id);
        }
    }
}
