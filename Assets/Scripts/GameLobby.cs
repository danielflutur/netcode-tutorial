using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using UnityEngine;

public class NewMonoBehaviourScript : MonoBehaviour
{


    private async void Start()
    {
        await UnityServices.InitializeAsync();

        AuthenticationService.Instance.SignedIn += () =>
        {
            Debug.Log($"Signed In {AuthenticationService.Instance.PlayerId}");
        };

        await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    private async void CreateLobby()
    {
        try
        {
            string lobbyName = "My Lobby";
            int maxPlayers = 2;

            var lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers);

        }
        catch (LobbyServiceException error)
        {
            Debug.LogError(error);
        }
    }

    private async void ListLobbies()
    {
        try
        {
            var queryResponse = await LobbyService.Instance.QueryLobbiesAsync();

            Debug.Log(queryResponse.Results.Count);

            foreach (var lobby in queryResponse.Results)
            {
                Debug.Log($"Lobby name: {lobby.Name}, {lobby.MaxPlayers}");
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError(e);
        }
        
    }
}
