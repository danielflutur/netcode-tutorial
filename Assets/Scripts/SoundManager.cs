using UnityEngine;

public class SoundManager : MonoBehaviour
{
    [SerializeField] private Transform _placeSfxPrefab;
    [SerializeField] private Transform _winSfxPrefab;
    [SerializeField] private Transform _loseSfxPrefab;

    private void Start()
    {
        GameManager.Instance.OnPlacedObject += GameManager_OnPlacedObject;
        GameManager.Instance.OnGameWin += GameManager_OnGameWin;
    }

    private void GameManager_OnGameWin(object sender, GameManager.OnGameWinEventArgs e)
    {
        if (GameManager.Instance.GetLocalPlayerType() == e.winPlayerType)
        {
            var sfxTransform = Instantiate(_winSfxPrefab);
            Destroy(sfxTransform.gameObject, 5f);
        }
        else
        {
            var sfxTransform = Instantiate(_loseSfxPrefab);
            Destroy(sfxTransform.gameObject, 5f);
        }
    }

    private void GameManager_OnPlacedObject(object sender, System.EventArgs e)
    {
        var sfxTransform = Instantiate(_placeSfxPrefab);
        Destroy(sfxTransform.gameObject, 5f);
    }
}
