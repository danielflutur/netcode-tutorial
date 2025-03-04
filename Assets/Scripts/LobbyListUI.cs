using System.Collections.Generic;
using System;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class LobbyListUI : MonoBehaviour
{
    public static LobbyListUI Instance { get; private set; }

    [SerializeField] private Transform _lobbyTemplate;
    [SerializeField] private Transform _container;
    [SerializeField] private Button _refreshButton;
    [SerializeField] private Button _createLobbyButton;
    [SerializeField] private Button _joinLobbyButton;

    private void Awake()
    {
        Instance = this;

        _lobbyTemplate.gameObject.SetActive(false);

        _refreshButton.onClick.AddListener(RefreshButtonClick);
        _createLobbyButton.onClick.AddListener(CreateLobbyButtonClick);
        _joinLobbyButton.onClick.AddListener(JoinLobbyButtonClick);
    }

    private void Start()
    {
        LobbyManager.Instance.OnLobbyListChanged += LobbyManager_OnLobbyListChanged;
        LobbyManager.Instance.OnJoinedLobby += LobbyManager_OnJoinedLobby;
        LobbyManager.Instance.OnLeftLobby += LobbyManager_OnLeftLobby;
    }

    private void LobbyManager_OnLeftLobby(object sender, EventArgs e)
    {
        Show();
    }

    private void LobbyManager_OnJoinedLobby(object sender, LobbyManager.LobbyEventArgs e)
    {
        Hide();
    }

    private void LobbyManager_OnLobbyListChanged(object sender, LobbyManager.OnLobbyListChangedEventArgs e)
    {
        UpdateLobbyList(e.lobbyList);
    }

    private void UpdateLobbyList(List<Lobby> lobbyList)
    {
        foreach (Transform child in _container)
        {
            if (child == _lobbyTemplate) continue;

            Destroy(child.gameObject);
        }

        foreach (Lobby lobby in lobbyList)
        {
            var lobbyTransform = Instantiate(_lobbyTemplate, _container);
            lobbyTransform.gameObject.SetActive(true);
            var lobbyTemplateUI = lobbyTransform.GetComponent<LobbyTemplateUI>();
            lobbyTemplateUI.UpdateLobby(lobby);
        }
    }

    private void RefreshButtonClick()
    {
        LobbyManager.Instance.RefreshLobbyList();
    }

    private void CreateLobbyButtonClick()
    {
        LobbyCreateUI.Instance.Show();
    }

    private void JoinLobbyButtonClick()
    {
        JoinLobbyUI.Instance.Show();
    }

    private void Hide()
    {
        gameObject.SetActive(false);
    }

    private void Show()
    {
        gameObject.SetActive(true);
    }
}
