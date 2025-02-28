using System;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    public event EventHandler<OnClickedOnGridPositionEventArgs> OnClickedGridPosition;
    public class OnClickedOnGridPositionEventArgs : EventArgs
    {
        public int x; 
        public int y;
        public PlayerType playerType;
    }

    public enum PlayerType
    {
        None,
        Cross,
        Circle
    }

    private PlayerType _localPlayerType;
    private PlayerType _currentPlayablePlayerType;


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    public override void OnNetworkSpawn()
    {

        if (NetworkManager.Singleton.LocalClientId == 0)
        {
            _localPlayerType = PlayerType.Cross;
        }
        else
        {
            _localPlayerType = PlayerType.Circle;
        }

        if (IsServer)
        {
            _currentPlayablePlayerType = PlayerType.Cross;
        }
    }

    [Rpc(SendTo.Server)]
    public void ClickedOnGridPositionRpc(int x, int y, PlayerType playerType)
    {
        Debug.Log($"ClickedOnGridPosition! {x}, {y}");
        if (playerType != _currentPlayablePlayerType)
        {
            return;
        }

        OnClickedGridPosition?.Invoke(this, new OnClickedOnGridPositionEventArgs
        {
            x = x,
            y = y,
            playerType = playerType
        });

        switch (_currentPlayablePlayerType)
        {
            default:
            case PlayerType.Cross:
                _currentPlayablePlayerType = PlayerType.Circle;
                break;
            case PlayerType.Circle:
                _currentPlayablePlayerType = PlayerType.Cross;
                break;
        }
    }

    public PlayerType GetLocalPlayerType()
    {
        return _localPlayerType;
    }
}
