using UnityEngine;
using UnityEngine.InputSystem;
using UniRx;
using System;

/// <summary>
/// ユーザーの入力を取りまとめるクラス
/// </summary>
public class PlayerInputs : MonoBehaviour
{
    private readonly Subject<Unit> _onAttackSubject = new();

    /// <summary>
    /// 左クリック時
    /// </summary>
    public IObservable<Unit> OnAttackObservable => _onAttackSubject.AsObservable();
    public Vector2 Look { get; private set; }

    /// <summary>
    /// マウス移動
    /// </summary>
    public void OnLook(InputValue value)
    {
        if(Time.time > Utility.StartTime)
        {
            Look = value.Get<Vector2>();
        }
    }

    /// <summary>
    /// 左クリック時
    /// </summary>
    public void OnAttack(InputValue value)
    {
        if(value.isPressed)
        {
            _onAttackSubject.OnNext(Unit.Default);
        }
    }

    private void OnDestroy()
    {
        _onAttackSubject?.Dispose();
    }
}
