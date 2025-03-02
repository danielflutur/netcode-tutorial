using System;
using System.Collections.Generic;
using Unity.Mathematics;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;
using static GameManager;

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

    public event EventHandler OnGameStarted;
    public event EventHandler OnCurrentPlayablePlayerTypeChanged;
    public event EventHandler OnRematch;
    public event EventHandler OnGameTied;
    public event EventHandler OnScoreChanged;
    public event EventHandler OnPlacedObject;

    public event EventHandler<OnGameWinEventArgs> OnGameWin;
    public class OnGameWinEventArgs : EventArgs
    {
        public Line line;
        public PlayerType winPlayerType;
    }

    public enum PlayerType
    {
        None,
        Cross,
        Circle
    }

    public enum Orientation
    {
        Horizontal,
        Vertical,
        DiagonalA,
        DiagonalB
    }

    public struct Line
    {
        public List<Vector2Int> gridVector2IntList;
        public Vector2Int centerGridPosition;
        public Orientation orientation;
    }

    private PlayerType _localPlayerType;
    private NetworkVariable<PlayerType> _currentPlayablePlayerType = new NetworkVariable<PlayerType>();
    private PlayerType[,] _playerTypeArray;
    private List<Line> _lines;
    private NetworkVariable<int> _playerCrossScore = new NetworkVariable<int>();
    private NetworkVariable<int> _playerCircleScore = new NetworkVariable<int>();

    private void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("More than one GameManager instance!");
        }
            
        Instance = this;

        _playerTypeArray = new PlayerType[3, 3];
        _lines = new List<Line>
        {
            new Line
            {
                gridVector2IntList = new List<Vector2Int>
                {
                    new Vector2Int(0,0),
                    new Vector2Int(1,0),
                    new Vector2Int(2,0),
                },
                centerGridPosition = new Vector2Int(1,0),
                orientation = Orientation.Horizontal,
            },
            new Line
            {
                gridVector2IntList = new List<Vector2Int>
                {
                    new Vector2Int(0,1),
                    new Vector2Int(1,1),
                    new Vector2Int(2,1),
                },
                centerGridPosition = new Vector2Int(1,1),
                orientation = Orientation.Horizontal,
            },
            new Line
            {
                gridVector2IntList = new List<Vector2Int>
                {
                    new Vector2Int(0,2),
                    new Vector2Int(1,2),
                    new Vector2Int(2,2),
                },
                centerGridPosition = new Vector2Int(1,2),
                orientation = Orientation.Horizontal,
            },
            new Line
            {
                gridVector2IntList = new List<Vector2Int>
                {
                    new Vector2Int(0,0),
                    new Vector2Int(0,1),
                    new Vector2Int(0,2),
                },
                centerGridPosition = new Vector2Int(0,1),
                orientation = Orientation.Vertical,
            },
            new Line
            {
                gridVector2IntList = new List<Vector2Int>
                {
                    new Vector2Int(1,0),
                    new Vector2Int(1,1),
                    new Vector2Int(1,2),
                },
                centerGridPosition = new Vector2Int(1,1),
                orientation = Orientation.Vertical,
            },
            new Line
            {
                gridVector2IntList = new List<Vector2Int>
                {
                    new Vector2Int(2,0),
                    new Vector2Int(2,1),
                    new Vector2Int(2,2),
                },
                centerGridPosition = new Vector2Int(2,1),
                orientation = Orientation.Vertical,
            },
            new Line
            {
                gridVector2IntList = new List<Vector2Int>
                {
                    new Vector2Int(0,0),
                    new Vector2Int(1,1),
                    new Vector2Int(2,2),
                },
                centerGridPosition = new Vector2Int(1,1),
                orientation = Orientation.DiagonalA,
            },
            new Line
            {
                gridVector2IntList = new List<Vector2Int>
                {
                    new Vector2Int(0,2),
                    new Vector2Int(1,1),
                    new Vector2Int(2,0),
                },
                centerGridPosition = new Vector2Int(1,1),
                orientation = Orientation.DiagonalB,
            }
        };
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
            NetworkManager.Singleton.OnClientConnectedCallback += NetworkManager_OnClientConnectedCallback;
        }

        _currentPlayablePlayerType.OnValueChanged += (PlayerType oldPlayerType, PlayerType newPlayerType) =>
        {
            OnCurrentPlayablePlayerTypeChanged?.Invoke(this, EventArgs.Empty);
        };

        _playerCrossScore.OnValueChanged += (int prevScore, int newScore) =>
        {
            OnScoreChanged?.Invoke(this, EventArgs.Empty);
        };


        _playerCircleScore.OnValueChanged += (int prevScore, int newScore) =>
        {
            OnScoreChanged?.Invoke(this, EventArgs.Empty);
        };
    }

    private void NetworkManager_OnClientConnectedCallback(ulong obj)
    {
        if (NetworkManager.Singleton.ConnectedClientsList.Count == 2)
        {
            _currentPlayablePlayerType.Value = PlayerType.Cross;
            TriggerOnGameStartedRpc();
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void TriggerOnGameStartedRpc()
    {
        OnGameStarted?.Invoke(this, EventArgs.Empty);
    }

    [Rpc(SendTo.Server)]
    public void ClickedOnGridPositionRpc(int x, int y, PlayerType playerType)
    {
        Debug.Log($"ClickedOnGridPosition! {x}, {y}");
        if (playerType != _currentPlayablePlayerType.Value)
        {
            return;
        }

        if (_playerTypeArray[x,y] != PlayerType.None)
        {
            return;
        }

        _playerTypeArray[x,y] = playerType;
        TriggerOnObjectPlacedRpc();

        OnClickedGridPosition?.Invoke(this, new OnClickedOnGridPositionEventArgs
        {
            x = x,
            y = y,
            playerType = playerType
        });

        switch (_currentPlayablePlayerType.Value)
        {
            default:
            case PlayerType.Cross:
                _currentPlayablePlayerType.Value = PlayerType.Circle;
                break;
            case PlayerType.Circle:
                _currentPlayablePlayerType.Value = PlayerType.Cross;
                break;
        }

        TestWinner();
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void TriggerOnObjectPlacedRpc()
    {
        OnPlacedObject?.Invoke(this, EventArgs.Empty);
    }

    private bool TestWinnerLine(PlayerType aPlayerType, PlayerType bPlayerType, PlayerType cPlayerType)
    {
        return
            aPlayerType != PlayerType.None &&
            aPlayerType == bPlayerType &&
            bPlayerType == cPlayerType;
    }


    private bool TestWinnerLine(Line line)
    {
        return
            TestWinnerLine(
                _playerTypeArray[line.gridVector2IntList[0].x, line.gridVector2IntList[0].y],
                _playerTypeArray[line.gridVector2IntList[1].x, line.gridVector2IntList[1].y],
                _playerTypeArray[line.gridVector2IntList[2].x, line.gridVector2IntList[2].y]
                );
    }

    private void TestWinner()
    {
        for (int i = 0; i < _lines.Count; i++)
        {
            var line = _lines[i];
            if (TestWinnerLine(line))
            {
                _currentPlayablePlayerType.Value = PlayerType.None;
                var winPlayerType = _playerTypeArray[line.centerGridPosition.x, line.centerGridPosition.y];
                
                switch (winPlayerType)
                {
                    case PlayerType.Cross:
                        _playerCrossScore.Value++;
                        break;
                    case PlayerType.Circle:
                        _playerCircleScore.Value++;
                        break;
                }

                TriggerOnGameWinRpc(i, winPlayerType);
                
                return;
            }
        }

        bool isTie = true;
        for (int i = 0; i < _playerTypeArray.GetLength(0); i++)
        {
            for (int j = 0; j < _playerTypeArray.GetLength(1); j++)
            {
                if (_playerTypeArray[i, j] == PlayerType.None)
                {
                    isTie = false;
                    break;
                }
            }
        }

        if (isTie)
        {
            TriggerOnGameTiedRpc();
        }

    }

    [Rpc(SendTo.ClientsAndHost)]
    private void TriggerOnGameTiedRpc()
    {
        OnGameTied?.Invoke(this, EventArgs.Empty);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void TriggerOnGameWinRpc(int lineIndex, PlayerType winPlayerType)
    {
        var line = _lines[lineIndex];
        OnGameWin?.Invoke(this, new OnGameWinEventArgs
        {
            line = line,
            winPlayerType = winPlayerType,
        });
    }

    public PlayerType GetLocalPlayerType()
    {
        return _localPlayerType;
    }

    public PlayerType GetCurrentPlayablePlayerType()
    {
        return _currentPlayablePlayerType.Value;
    }

    [Rpc(SendTo.Server)]
    public void RematchRpc()
    {
        for (int i = 0; i < _playerTypeArray.GetLength(0); i++)
        {
            for (int j = 0; j < _playerTypeArray.GetLength(1); j++)
            {
                _playerTypeArray[i, j] = PlayerType.None;
            }
        }

        _currentPlayablePlayerType.Value = PlayerType.Cross;
        TriggerOnRematchRpc();
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void TriggerOnRematchRpc()
    {
        OnRematch?.Invoke(this, EventArgs.Empty);
    }

    public void GetScores(out int playerCrossScore, out int playerCircleScore)
    {
        playerCrossScore = _playerCrossScore.Value;
        playerCircleScore = _playerCircleScore.Value;
    }
}
