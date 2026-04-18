using UniRx;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// プレイヤーの入力・カメラ制御・射撃を管理するクラス
/// </summary>
public class PlayerController : MonoBehaviour
{
    [Header("参照")]
    [SerializeField] private CharacterController _controller;
    [SerializeField] private Animator _animator;
    [SerializeField] private Transform _playerCameraRoot;
    [SerializeField] private PlayerInputs _inputs;
    [SerializeField] private GameObject _mainCamera;

    [Header("カメラの設定")]
    [Tooltip("マウスの感度")]
    [SerializeField, Min(0f)] private float _cameraSensitivity = 100.0f;
    [Tooltip("上方向の回転制限")]
    [SerializeField] private float _topClamp = 70.0f;
    [Tooltip("下方向の回転制限")]
    [SerializeField] private float _bottomClamp = -30.0f;
    [Tooltip("視点入力を無視する閾値")]
    [SerializeField, Min(0f)] private float _threshold = 0.01f;
    [Tooltip("移動速度")]
    [SerializeField, Min(0f)] private float _moveSpeed = 1f;
    [SerializeField] private float _sprintSpeed = 5.335f;
    [Tooltip("初期位置のオフセットとして加算されるワールド空間のオフセット")]
    [SerializeField] private Vector3 _offsetPostiion = Vector3.zero;

    private float _argH;
    private float _argV;

    // animation IDs
    private int _animIDSpeed;
    private int _animIDGrounded;
    private int _animIDJump;
    private int _animIDFreeFall;
    private int _animIDMotionSpeed;

    [Tooltip("How fast the character turns to face movement direction")]
    [Range(0.0f, 0.3f)]
    public float RotationSmoothTime = 0.12f;

    [Tooltip("Acceleration and deceleration")]
    public float SpeedChangeRate = 10.0f;

    public AudioSource AudioFootsteps;
    public AudioSource LandingAudio;
    public AudioSource AudioFoley;
    public AudioClip LandingAudioClip;
    public AudioClip[] FootstepAudioClips;
    [Range(0, 1)] public float FootstepAudioVolume = 0.5f;

    [Space(10)]
    [Tooltip("The height the player can jump")]
    public float JumpHeight = 1.2f;

    [Tooltip("The character uses its own gravity value. The engine default is -9.81f")]
    public float Gravity = -15.0f;

    [Space(10)]
    [Tooltip("Time required to pass before being able to jump again. Set to 0f to instantly jump again")]
    public float JumpTimeout = 0.50f;

    [Tooltip("Time required to pass before entering the fall state. Useful for walking down stairs")]
    public float FallTimeout = 0.15f;

    [Header("Player Grounded")]
    [Tooltip("If the character is grounded or not. Not part of the CharacterController built in grounded check")]
    public bool Grounded = true;

    [Tooltip("Useful for rough ground")]
    public float GroundedOffset = -0.14f;

    [Tooltip("The radius of the grounded check. Should match the radius of the CharacterController")]
    public float GroundedRadius = 0.28f;

    [SerializeField] private LayerMask _groundLayers;


    // player
    private float _speed;
    private float _animationBlend;
    private float _playerRotation = 0.0f;
    private float _rotationVelocity;
    private float _verticalVelocity;
    private float _terminalVelocity = 53.0f;

    // timeout deltatime
    private float _jumpTimeoutDelta;
    private float _fallTimeoutDelta;

    public void Initialize()
    {
        Vector3 eulerAngle = _playerCameraRoot.localRotation.eulerAngles;
        _argH = NormalizeAngle(eulerAngle.y);
        _argV = NormalizeAngle(eulerAngle.x);

        _animIDSpeed = Animator.StringToHash("Speed");
        _animIDGrounded = Animator.StringToHash("Grounded");
        _animIDJump = Animator.StringToHash("Jump");
        _animIDFreeFall = Animator.StringToHash("FreeFall");
        _animIDMotionSpeed = Animator.StringToHash("MotionSpeed");

        _jumpTimeoutDelta = JumpTimeout;
        _fallTimeoutDelta = FallTimeout;
    }
    public void OnUpdate()
    {
        JumpAndGravity();
        GroundedCheck();
        UpeateTransForm();
        UpdateCamera();
    }

    private void UpeateTransForm()
    {
        float targetSpeed = _inputs.IsSprint ? _sprintSpeed : _moveSpeed;

        if (_inputs.Move == Vector2.zero) targetSpeed = 0.0f;

        float currentHorizontalSpeed = new Vector3(_controller.velocity.x, 0.0f, _controller.velocity.z).magnitude;

        _speed = targetSpeed;

        _animationBlend = Mathf.Lerp(_animationBlend, targetSpeed, Time.deltaTime * SpeedChangeRate);
        if (_animationBlend < 0.01f) _animationBlend = 0f;

        Vector3 inputDirection = new Vector3(_inputs.Move.x, 0.0f, _inputs.Move.y).normalized;

        if (_inputs.Move != Vector2.zero)
        {
            _playerRotation = Mathf.Atan2(inputDirection.x, inputDirection.z) * Mathf.Rad2Deg + _mainCamera.transform.eulerAngles.y;
            float rotation = Mathf.SmoothDampAngle(transform.eulerAngles.y, _playerRotation, ref _rotationVelocity, RotationSmoothTime);

            transform.rotation = Quaternion.Euler(0.0f, rotation, 0.0f);
        }


        Vector3 targetDirection = Quaternion.Euler(0.0f, _playerRotation, 0.0f) * Vector3.forward;

        // move the player
        _controller.Move(targetDirection.normalized * (_speed * Time.deltaTime) + new Vector3(0.0f, _verticalVelocity, 0.0f) * Time.deltaTime);
        _animator.SetFloat(_animIDSpeed, _animationBlend);
        _animator.SetFloat(_animIDMotionSpeed, _inputs.Move.magnitude);
    }

    private void UpdateCamera()
    {
        _mainCamera.transform.position = _playerCameraRoot.position;
        // カメラの角度を更新
        Vector2 look = _inputs.Look;

        if (look.sqrMagnitude < _threshold * _threshold) return;

        _argH += look.x * _cameraSensitivity * Time.deltaTime;
        _argV -= look.y * _cameraSensitivity * Time.deltaTime;

        _argH = NormalizeAngle(_argH);
        _argV = Mathf.Clamp(NormalizeAngle(_argV), _bottomClamp, _topClamp);
        _mainCamera.transform.eulerAngles = new Vector3(_argV, _argH, 0f);
    }

    private void JumpAndGravity()
    {
        if (Grounded)
        {
            _fallTimeoutDelta = FallTimeout;

            _animator.SetBool(_animIDJump, false);
            _animator.SetBool(_animIDFreeFall, false);

            if (_verticalVelocity < 0.0f)
            {
                _verticalVelocity = -2f;
            }

            if (_inputs.IsJump && _jumpTimeoutDelta <= 0.0f)
            {
                _verticalVelocity = Mathf.Sqrt(JumpHeight * -2f * Gravity);

                _animator.SetBool(_animIDJump, true);
            }

            if (_jumpTimeoutDelta >= 0.0f)
            {
                _jumpTimeoutDelta -= Time.deltaTime;
            }
        }
        else
        {
            // reset the jump timeout timer
            _jumpTimeoutDelta = JumpTimeout;

            // fall timeout
            if (_fallTimeoutDelta >= 0.0f)
            {
                _fallTimeoutDelta -= Time.deltaTime;
            }
            else
            {
                _animator.SetBool(_animIDFreeFall, true);
            }
            _inputs.IsJump = false;
        }

        if (_verticalVelocity < _terminalVelocity)
        {
            _verticalVelocity += Gravity * Time.deltaTime;
        }
    }

    private void GroundedCheck()
    {
        Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y + GroundedOffset, transform.position.z);
        Grounded = Physics.CheckSphere(spherePosition, GroundedRadius, _groundLayers, QueryTriggerInteraction.Ignore);
        _animator.SetBool(_animIDGrounded, Grounded);
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

    private void OnFootstep(AnimationEvent animationEvent)
    {
        if (animationEvent.animatorClipInfo.weight > 0.5f)
        {

            if (AudioFootsteps != null)
                AudioFootsteps.Play();
            if (AudioFoley != null)
                AudioFoley.Play();
        }
    }

    private void OnLand(AnimationEvent animationEvent)
    {
        if (animationEvent.animatorClipInfo.weight > 0.5f)
        {
            if (LandingAudio != null)
                LandingAudio.Play();

        }
    }
}
