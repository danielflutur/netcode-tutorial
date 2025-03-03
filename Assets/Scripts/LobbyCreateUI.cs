using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyCreateUI : MonoBehaviour
{
    public static LobbyCreateUI Instance { get; private set; }

    [SerializeField] private TMP_InputField _lobbyNameInput;
    [SerializeField] private TMP_InputField _maxPlayerCountInput;
    [SerializeField] private TextMeshProUGUI _privateStateText;
    [SerializeField] private Button _privateStateButton;
    [SerializeField] private Button _createButton;

    private bool _isPrivate;
    private TMP_Text _buttonText;

    private void Awake()
    {
        Instance = this;

        _createButton.onClick.AddListener(() => {
            LobbyManager.Instance.CreateLobby(
                _lobbyNameInput.text,
                int.Parse(_maxPlayerCountInput.text),
                _isPrivate
            );
            Hide();
        });
        
        _privateStateButton.onClick.AddListener(() => {
            _isPrivate = !_isPrivate;
            _buttonText = _privateStateButton.GetComponentInChildren<TMP_Text>();
            UpdateText();
        });

        Hide();
    }

    private void UpdateText()
    {
        _buttonText.text = _isPrivate ? "PRIVATE" : "PUBLIC";
    }

    private void Hide()
    {
        gameObject.SetActive(false);
    }

    public void Show()
    {
        gameObject.SetActive(true);
        _isPrivate = true;
        _maxPlayerCountInput.text = "";
        _lobbyNameInput.text = "";
    }
}
