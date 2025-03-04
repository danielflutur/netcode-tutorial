using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
    private const float REFRESH_LOBBY_LIST_TIMER_MAX = 5f;

    private Lobby _joinedLobby;
    private string _playerName;
    private float _heartbeatTimer;
    private float refreshLobbyListTimer = 5f;

    private LobbyEventCallbacks _lobbyEventCallbacks;


    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        HandleRefreshLobbyList();
        HandleLobbyHeartbeat();
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

    public Lobby GetJoinedLobby()
    {
        return _joinedLobby;
    }

    public bool IsLobbyHost()
    {
        return _joinedLobby != null && _joinedLobby.HostId == AuthenticationService.Instance.PlayerId;
    }

    private Player GetPlayer()
    {
        return new Player(AuthenticationService.Instance.PlayerId, null, new Dictionary<string, PlayerDataObject> {
            { KEY_PLAYER_NAME, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, _playerName) },
        });
    }

    private async Task SetupLobbyEvents(Lobby lobby)
    {
        if (_lobbyEventCallbacks != null)
        {
            _lobbyEventCallbacks.LobbyChanged -= OnLobbyChanged;
            _lobbyEventCallbacks.KickedFromLobby -= OnKickedFromLobbyEvent;
        }

        _lobbyEventCallbacks = new LobbyEventCallbacks();
        _lobbyEventCallbacks.LobbyChanged += OnLobbyChanged;
        _lobbyEventCallbacks.KickedFromLobby += OnKickedFromLobbyEvent;

        await LobbyService.Instance.SubscribeToLobbyEventsAsync(lobby.Id, _lobbyEventCallbacks);
    }

    public async void CreateLobby(string lobbyName, bool isPrivate)
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

        Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, 2, options);

        await SetupLobbyEvents(lobby);

        _joinedLobby = lobby;

        OnJoinedLobby?.Invoke(this, new LobbyEventArgs { lobby = lobby });

        Debug.Log("Created Lobby " + lobby.Name);
    }

    private async void OnLobbyChanged(ILobbyChanges changes)
    {
        if (_joinedLobby != null)
        {
            changes.ApplyToLobby(_joinedLobby);

            if (changes.PlayerJoined.Changed || changes.PlayerLeft.Changed)
            {
                _joinedLobby = await LobbyService.Instance.GetLobbyAsync(_joinedLobby.Id);
                OnJoinedLobbyUpdate?.Invoke(this, new LobbyEventArgs { lobby = _joinedLobby });
            }

            if (!IsLobbyHost() && changes.Data.Changed && CheckLobbyDataChanged(changes.Data.Value, KEY_START_GAME, "1") )
            {
                JoinGame(_joinedLobby.Data[KEY_RELAY_JOIN_CODE].Value);
            }
        }
    }

    private bool CheckLobbyDataChanged(Dictionary<string, ChangedOrRemovedLobbyValue<DataObject>> lobbyData, string key, string verifiedValue)
    {
        return lobbyData.ContainsKey(key) 
            && lobbyData[key].ChangeType == LobbyValueChangeType.Changed
            && lobbyData[key].Value.Value == verifiedValue;
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

        await SetupLobbyEvents(_joinedLobby);

        Debug.Log("Lobby joined by code " + lobbyCode);

        OnJoinedLobby?.Invoke(this, new LobbyEventArgs { lobby = lobby });
    }

    public async void JoinLobby(Lobby lobby)
    {
        Player player = GetPlayer();

        _joinedLobby = await LobbyService.Instance.JoinLobbyByIdAsync(lobby.Id, new JoinLobbyByIdOptions
        {
            Player = player
        });

        await SetupLobbyEvents(_joinedLobby);

        Debug.Log($"Player joined the lobby: {lobby.Name}");

        OnJoinedLobby?.Invoke(this, new LobbyEventArgs { lobby = lobby });
    }

    public async void QuickJoinLobby()
    {
        try
        {
            QuickJoinLobbyOptions options = new QuickJoinLobbyOptions();

            Lobby lobby = await LobbyService.Instance.QuickJoinLobbyAsync(options);
            _joinedLobby = lobby;

            await SetupLobbyEvents(_joinedLobby);

            Debug.Log($"Player quick joined the lobby: {lobby.Name}");

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
            }
            catch (LobbyServiceException e)
            {
                Debug.Log(e);
            }
        }
    }

    private void OnKickedFromLobbyEvent()
    {
        _joinedLobby = null;
        _lobbyEventCallbacks = null;

        OnLeftLobby?.Invoke(this, EventArgs.Empty);
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

    public void StartGame()
    {
        try
        {
            Debug.Log("StartGame");
            IsHost = true;
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
                    { KEY_RELAY_JOIN_CODE, new DataObject(DataObject.VisibilityOptions.Member, relayJoinCode) },
                    { KEY_START_GAME, new DataObject(DataObject.VisibilityOptions.Member, "1") }
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
