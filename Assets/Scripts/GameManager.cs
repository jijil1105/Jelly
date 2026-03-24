#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private PlayerController _playerController;
    [SerializeField] private ComputeBehavior _computeBehavior;
    [SerializeField] private AudioManager _audioManager;
    [SerializeField] private WaitUI _waitUI;

    void Start()
    {
        Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
        Screen.fullScreen = true;
        Cursor.visible = false;
        _audioManager.Initialize();
        _playerController.Initialize();
        _computeBehavior.Initialize(_playerController.BulletPoolCount);
        _waitUI.Initialized();
    }

    void Update()
    {
        if(Time.time > Utility.EndTime)
        {
            Quit();
        }

        if (Time.time > Utility.StartTime)
        {
            _waitUI.SetVisible(false);
        }
        else 
        {
            _waitUI.OnUpdate();
        }

        _audioManager.OnUpdate();
        _playerController.OnUpdate();
        _computeBehavior.OnUpdate(_audioManager.GetLogBands(), _playerController.GetActiveBullets());
    }

    void LateUpdate()
    {
        _computeBehavior.OnLateUpdate();
    }

    private void Quit()
    {
#if UNITY_EDITOR
        EditorApplication.isPlaying = false; // エディタでの停止
#else
        Application.Quit(); // ビルドでの終了
#endif
    }
}
