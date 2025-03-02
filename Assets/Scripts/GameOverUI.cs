using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameOverUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _resultTextMesh;
    [SerializeField] private Color _winColor;
    [SerializeField] private Color _loseColor;
    [SerializeField] private Color _tieColor;
    [SerializeField] private Button _rematchButton;

    private void Awake()
    {
        _rematchButton.onClick.AddListener(() =>
        {
            GameManager.Instance.RematchRpc();
        });
    }

    private void Start()
    {
        GameManager.Instance.OnGameWin += GameManager_OnGameWin;
        GameManager.Instance.OnRematch += GameManager_OnRematch;
        GameManager.Instance.OnGameTied += GameManager_OnGameTied;

        Hide();
    }

    private void GameManager_OnGameTied(object sender, System.EventArgs e)
    {
        _resultTextMesh.text = "GAME TIED!";
        _resultTextMesh.color = _tieColor;

        Show();
    }

    private void GameManager_OnRematch(object sender, System.EventArgs e)
    {
        Hide();
    }

    private void GameManager_OnGameWin(object sender, GameManager.OnGameWinEventArgs e)
    {
        if (e.winPlayerType == GameManager.Instance.GetLocalPlayerType())
        {
            _resultTextMesh.text = "YOU WIN!";
            _resultTextMesh.color = _winColor;
        }
        else
        {
            _resultTextMesh.text = "YOU LOSE!";
            _resultTextMesh.color = _loseColor;
        }

        Show();
    }

    private void Show()
    {
        gameObject.SetActive(true);
    }

    private void Hide() 
    { 
        gameObject.SetActive(false); 
    }
}
