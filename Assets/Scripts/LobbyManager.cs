using System;
using System.Collections.Generic;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance { get; private set; }

    public static bool IsHost { get; private set; }
    public static string RelayJoinCode { get; private set; }

    public event EventHandler OnLeftLobby;

    public event EventHandler<LobbyEventArgs> OnJoinedLobby;
    public event EventHandler<LobbyEventArgs> OnJoinedLobbyUpdate;
    public event EventHandler<LobbyEventArgs> OnKickedFromLobby;
    public event EventHandler<LobbyEventArgs> OnLobbyStartGame;
    public class LobbyEventArgs : EventArgs
    {
        public Lobby lobby;
    }

    public event EventHandler<OnLobbyListChangedEventArgs> OnLobbyListChanged;
    public class OnLobbyListChangedEventArgs : EventArgs
    {
        public List<Lobby> lobbyList;
    }

    public const string KEY_PLAYER_NAME = "PlayerName";
    public const string KEY_PLAYER_CHARACTER = "Character";
    public const string KEY_GAME_MODE = "GameMode";
    public const string KEY_START_GAME = "StartGame";
    public const string KEY_RELAY_JOIN_CODE = "RelayJoinCode";
    private const float HEARTBEAT_TIMER_MAX = 15f;
    private const float LOBBY_POOL_TIMER_MAX = 1.1f;
    private const float REFRESH_LOBBY_LIST_TIMER_MAX = 5f;

    private Lobby _joinedLobby;
    private string _playerName;
    private bool _alreadyStartedGame;
    private float _heartbeatTimer;
    private float _lobbyPollTimer;
    private float refreshLobbyListTimer = 5f;

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        HandleRefreshLobbyList();
        HandleLobbyHeartbeat();
        HandleLobbyPolling();
    }

    public async void Authenticate(string playerName)
    {
        _playerName = playerName.Replace(" ", "_").Trim();
        InitializationOptions initializationOptions = new InitializationOptions();
        initializationOptions.SetProfile(_playerName);

        await UnityServices.InitializeAsync(initializationOptions);

        AuthenticationService.Instance.SignedIn += () => {
            Debug.Log("Signed in! " + AuthenticationService.Instance.PlayerId);

            RefreshLobbyList();
        };

        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    private void HandleRefreshLobbyList()
    {
        if (UnityServices.State == ServicesInitializationState.Initialized && AuthenticationService.Instance.IsSignedIn)
        {
            refreshLobbyListTimer -= Time.deltaTime;
            if (refreshLobbyListTimer < 0f)
            {
                refreshLobbyListTimer = REFRESH_LOBBY_LIST_TIMER_MAX;

                RefreshLobbyList();
            }
        }
    }

    private async void HandleLobbyHeartbeat()
    {
        if (IsLobbyHost())
        {
            _heartbeatTimer -= Time.deltaTime;

            if (_heartbeatTimer < 0f)
            {
                _heartbeatTimer = HEARTBEAT_TIMER_MAX;
                await LobbyService.Instance.SendHeartbeatPingAsync(_joinedLobby.Id);
            }
        }
    }

    private async void HandleLobbyPolling()
    {
        if (_joinedLobby != null)
        {
            _lobbyPollTimer -= Time.deltaTime;
            if (_lobbyPollTimer < 0f)
            {
                _lobbyPollTimer = LOBBY_POOL_TIMER_MAX;

                _joinedLobby = await LobbyService.Instance.GetLobbyAsync(_joinedLobby.Id);

                OnJoinedLobbyUpdate?.Invoke(this, new LobbyEventArgs { lobby = _joinedLobby });

                if (!IsLobbyHost())
                {
                    if (_joinedLobby.Data[KEY_RELAY_JOIN_CODE].Value != "")
                    {
                        JoinGame(_joinedLobby.Data[KEY_RELAY_JOIN_CODE].Value);
                    }
                }

                if (!_alreadyStartedGame)
                {
                    if (IsLobbyHost())
                    {
                        if (_joinedLobby.Players.Count == 2)
                        {
                            // Two players have joined, start game
                            //StartGame();
                        }
                    }
                }


                if (!IsPlayerInLobby())
                {
                    // Player was kicked out of this lobby
                    Debug.Log("Kicked from Lobby!");

                    OnKickedFromLobby?.Invoke(this, new LobbyEventArgs { lobby = _joinedLobby });

                    _joinedLobby = null;
                }
            }
        }
    }

    public Lobby GetJoinedLobby()
    {
        return _joinedLobby;
    }

    public bool IsLobbyHost()
    {
        return _joinedLobby != null && _joinedLobby.HostId == AuthenticationService.Instance.PlayerId;
    }

    private bool IsPlayerInLobby()
    {
        if (_joinedLobby != null && _joinedLobby.Players != null)
        {
            foreach (Player player in _joinedLobby.Players)
            {
                if (player.Id == AuthenticationService.Instance.PlayerId)
                {
                    return true;
                }
            }
        }
        return false;
    }

    private Player GetPlayer()
    {
        return new Player(AuthenticationService.Instance.PlayerId, null, new Dictionary<string, PlayerDataObject> {
            { KEY_PLAYER_NAME, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, _playerName) },
        });
    }

    public async void CreateLobby(string lobbyName, int maxPlayers, bool isPrivate)
    {
        Player player = GetPlayer();

        CreateLobbyOptions options = new CreateLobbyOptions
        {
            Player = player,
            IsPrivate = isPrivate,
            Data = new Dictionary<string, DataObject> {
                { KEY_RELAY_JOIN_CODE, new DataObject(DataObject.VisibilityOptions.Member, "") }
            }
        };

        Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);

        _joinedLobby = lobby;

        OnJoinedLobby?.Invoke(this, new LobbyEventArgs { lobby = lobby });

        Debug.Log("Created Lobby " + lobby.Name);
    }

    public async void RefreshLobbyList()
    {
        try
        {
            QueryLobbiesOptions options = new QueryLobbiesOptions();
            options.Count = 25;

            // Filter for open lobbies only
            options.Filters = new List<QueryFilter> {
                new QueryFilter(
                    field: QueryFilter.FieldOptions.AvailableSlots,
                    op: QueryFilter.OpOptions.GT,
                    value: "0")
            };

            // Order by newest lobbies first
            options.Order = new List<QueryOrder> {
                new QueryOrder(
                    asc: false,
                    field: QueryOrder.FieldOptions.Created)
            };

            QueryResponse lobbyListQueryResponse = await LobbyService.Instance.QueryLobbiesAsync();

            OnLobbyListChanged?.Invoke(this, new OnLobbyListChangedEventArgs { lobbyList = lobbyListQueryResponse.Results });
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    public async void JoinLobbyByCode(string lobbyCode)
    {
        Player player = GetPlayer();

        Lobby lobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode, new JoinLobbyByCodeOptions
        {
            Player = player
        });

        _joinedLobby = lobby;

        OnJoinedLobby?.Invoke(this, new LobbyEventArgs { lobby = lobby });
    }

    public async void JoinLobby(Lobby lobby)
    {
        Player player = GetPlayer();

        _joinedLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobby.Id, new JoinLobbyByIdOptions
        {
            Player = player
        });

        OnJoinedLobby?.Invoke(this, new LobbyEventArgs { lobby = lobby });
    }

    public async void QuickJoinLobby()
    {
        try
        {
            QuickJoinLobbyOptions options = new QuickJoinLobbyOptions();

            Lobby lobby = await LobbyService.Instance.QuickJoinLobbyAsync(options);
            _joinedLobby = lobby;

            OnJoinedLobby?.Invoke(this, new LobbyEventArgs { lobby = lobby });
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    public async void LeaveLobby()
    {
        if (_joinedLobby != null)
        {
            try
            {
                await LobbyService.Instance.RemovePlayerAsync(_joinedLobby.Id, AuthenticationService.Instance.PlayerId);

                _joinedLobby = null;

                OnLeftLobby?.Invoke(this, EventArgs.Empty);
            }
            catch (LobbyServiceException e)
            {
                Debug.Log(e);
            }
        }
    }

    public async void KickPlayer(string playerId)
    {
        if (IsLobbyHost())
        {
            try
            {
                await LobbyService.Instance.RemovePlayerAsync(_joinedLobby.Id, playerId);
            }
            catch (LobbyServiceException e)
            {
                Debug.Log(e);
            }
        }
    }

    public async void StartGame()
    {
        try
        {
            Debug.Log("StartGame");

            Lobby lobby = await LobbyService.Instance.UpdateLobbyAsync(_joinedLobby.Id, new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject> {
                    { KEY_START_GAME, new DataObject(DataObject.VisibilityOptions.Public, "1") }
                }
            });

            _joinedLobby = lobby;

            IsHost = true;
            _alreadyStartedGame = true;
            SceneManager.LoadScene(1);

            OnLobbyStartGame?.Invoke(this, new LobbyEventArgs { lobby = _joinedLobby });
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    private void JoinGame(string relayJoinCode)
    {
        Debug.Log("JoinGame " + relayJoinCode);
        if (string.IsNullOrEmpty(relayJoinCode))
        {
            Debug.Log("Invalid Relay code, wait");
            return;
        }

        IsHost = false;
        RelayJoinCode = relayJoinCode;
        SceneManager.LoadScene(1);
        _alreadyStartedGame = true;
        OnLobbyStartGame?.Invoke(this, new LobbyEventArgs { lobby = _joinedLobby });
    }

    public async void SetRelayJoinCode(string relayJoinCode)
    {
        try
        {
            Debug.Log("SetRelayJoinCode " + relayJoinCode);

            Lobby lobby = await LobbyService.Instance.UpdateLobbyAsync(_joinedLobby.Id, new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject> {
                    { KEY_RELAY_JOIN_CODE, new DataObject(DataObject.VisibilityOptions.Member, relayJoinCode) }
                }
            });

            _joinedLobby = lobby;
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }
}
