using UnityEngine;
using UnityEngine.InputSystem;
using UniRx;
using System;

/// <summary>
/// ユーザーの入力を取りまとめるクラス
/// </summary>
public class PlayerInputs : MonoBehaviour
{
    public Vector2 Move { get; private set; }
    public Vector2 Look { get; private set; }
    public bool IsSprint { get; private set; }
    public bool IsJump;
    /// <summary>
    /// プレイヤー移動
    /// </summary>
    public void OnMove(InputValue value)
    {
        Debug.Log($"Move: {value.Get<Vector2>()}");
        Move = value.Get<Vector2>();
    }

    /// <summary>
    /// マウス移動
    /// </summary>
    public void OnLook(InputValue value)
    {
        Look = value.Get<Vector2>();
    }

    public void OnSprint(InputValue value)
    {
        IsSprint = value.isPressed;
    }

    public void OnJump(InputValue value)
    {
        //IsJump = value.isPressed;
    }

    /// <summary>
    /// 左クリック時
    /// </summary>
    public void OnAttack(InputValue value)
    {
    }

    private void OnDestroy()
    {

    }
}
