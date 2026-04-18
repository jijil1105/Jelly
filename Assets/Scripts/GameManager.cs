#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UniRx;

public class GameManager : MonoBehaviour
{
    [SerializeField] private PlayerController _playerController;
    [SerializeField] private ComputeBehavior _computeBehavior;
    [SerializeField] private AudioManager _audioManager;
    [SerializeField] private UIManager _uiManager;
    [SerializeField] private EnterTrain _enterTrain;

    void Start()
    {
        Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
        Screen.fullScreen = true;
        Cursor.visible = false;
        _audioManager.Initialize();
        _playerController.Initialize();
        _computeBehavior.Initialize();
        _uiManager.Initialize();
        _enterTrain.Initialize();
        
        _enterTrain.OnEnterTrain.Subscribe(_ => _uiManager.SetVisibleEnterTrainUI(true)).AddTo(this);
    }

    void Update()
    {
        _audioManager.OnUpdate();
        _playerController.OnUpdate();
        _computeBehavior.OnUpdate(_audioManager.GetLogBands());
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
