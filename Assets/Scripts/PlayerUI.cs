using UnityEngine;

public class PlayerUI : MonoBehaviour
{
    [SerializeField] private GameObject _crossArrowGameObject;
    [SerializeField] private GameObject _circleArrowGameObject;
    [SerializeField] private GameObject _crossYouTextGameObject;
    [SerializeField] private GameObject _circleYouTextGameObject;

    private void Awake()
    {
        _crossArrowGameObject.SetActive(false);
        _circleArrowGameObject.SetActive(false);
        _crossYouTextGameObject.SetActive(false);
        _circleYouTextGameObject.SetActive(false);
    }

    private void Start()
    {
        GameManager.Instance.OnGameStarted += GameManager_OnGameStarted;
        GameManager.Instance.OnCurrentPlayablePlayerTypeChanged += GameManager_OnCurrentPlayablePlayerTypeChanged;
    }

    private void GameManager_OnCurrentPlayablePlayerTypeChanged(object sender, System.EventArgs e)
    {
        UpdateCurrentArrow();
    }

    private void GameManager_OnGameStarted(object sender, System.EventArgs e)
    {
        if (GameManager.Instance.GetLocalPlayerType() == GameManager.PlayerType.Cross)
        {
            _crossYouTextGameObject.SetActive(true);
        }
        else
        {
            _circleYouTextGameObject.SetActive(true);
        }

        UpdateCurrentArrow();
    }

    private void UpdateCurrentArrow()
    {
        if (GameManager.Instance.GetCurrentPlayablePlayerType() == GameManager.PlayerType.Cross)
        {
            _crossArrowGameObject.SetActive(true);
            _circleArrowGameObject.SetActive(false);
        }
        else
        {
            _crossArrowGameObject.SetActive(false);
            _circleArrowGameObject.SetActive(true);
        }
    }
}
