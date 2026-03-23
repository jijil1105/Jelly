using UniRx;
using UnityEngine;

/// <summary>
/// プレイヤーの入力・カメラ制御・射撃を管理するクラス
/// </summary>
public class PlayerController : MonoBehaviour
{
    [Header("参照")]
    [Tooltip("カメラのルート Transform（ローカル回転を操作して視点を制御）")]
    [SerializeField] private Transform _playerCameraRoot;
    [Tooltip("入力を受け取るコンポーネント（PlayerInputs）")]
    [SerializeField] private PlayerInputs _inputs;
    [Tooltip("弾の生成・管理を行う Shooter コンポーネント")]
    [SerializeField] private Shooter _shooter;

    [Header("カメラの設定")]
    [Tooltip("マウスの感度")]
    [SerializeField, Min(0f)] private float _cameraSensitivity = 100.0f;
    [Tooltip("上方向の回転制限")]
    [SerializeField] private float _topClamp = 70.0f;
    [Tooltip("下方向の回転制限")]
    [SerializeField] private float _bottomClamp = -30.0f;
    [Tooltip("視点入力を無視する閾値")]
    [SerializeField, Min(0f)] private float _threshold = 0.01f;
    [Tooltip("プレイヤーの円運動の半径（ワールド単位）")]
    [SerializeField, Min(0f)] private float _radius = 1f;
    [Tooltip("移動速度")]
    [SerializeField, Min(0f)] private float _moveSpeed = 1f;
    [Tooltip("初期位置のオフセットとして加算されるワールド空間のオフセット")]
    [SerializeField] private Vector3 _offsetPostiion = Vector3.zero;

    private float _argH;
    private float _argV;

    /// <inheritdoc/>
    public int BulletPoolCount => _shooter.PoolCount;
    /// <inheritdoc/>
    public Bullet[] GetActiveBullets() => _shooter.GetActiveObjects();

    public void Initialize()
    {
        Vector3 eulerAngle = _playerCameraRoot.localRotation.eulerAngles;
        _argH = NormalizeAngle(eulerAngle.y);
        _argV = NormalizeAngle(eulerAngle.x);
        _shooter.Initialize();
        float time = Time.time * _moveSpeed;
        transform.position = new Vector3(Mathf.Cos(time), Mathf.Sin(time), Mathf.Sin(time)) * _radius + _offsetPostiion;

        _inputs.OnAttackObservable.Subscribe(_ => { _shooter.Spawn(-_playerCameraRoot.forward); }).AddTo(this);
    }
    public void OnUpdate()
    {
        UpdateCamera();
        _shooter.OnUpdate();
    }

    private void UpdateCamera()
    {
        // カメラを移動
        float time = Time.time * _moveSpeed;
        transform.position = new Vector3(Mathf.Cos(time), Mathf.Sin(time), Mathf.Sin(time)) * _radius + _offsetPostiion;

        // カメラの角度を更新
        Vector2 look = _inputs.Look;

        if (look.sqrMagnitude < _threshold * _threshold) return;

        _argH += look.x * _cameraSensitivity * Time.deltaTime;
        _argV += look.y * _cameraSensitivity * Time.deltaTime;

        _argH = NormalizeAngle(_argH);
        _argV = Mathf.Clamp(NormalizeAngle(_argV), _bottomClamp, _topClamp);
        _playerCameraRoot.localEulerAngles = new Vector3(_argV, _argH, 0f);
    }

    /// <summary>
    /// 角度を-180〜180 の間に収めます
    /// </summary>
    private static float NormalizeAngle(float angle)
    {
        while (angle < -180f) angle += 360f;
        while (angle > 180f) angle -= 360f;
        return angle;
    }
}
