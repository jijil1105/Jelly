using UnityEngine;
using System;

/// <summary>
/// プールで管理されるオブジェクトの基底クラス。
/// </summary>
/// <typeparam name="T">プールで管理する型。</typeparam>
public abstract class PooledObject<T> : MonoBehaviour where T : MonoBehaviour
{
    [SerializeField] private float _lifeTime = 2f;

    private float _elapsedTime = 0f;
    private Action<T> _returnToPool = null;

    public virtual void Initialize(Action<T> returnToPool)
    {
        _returnToPool = returnToPool;
    }

    /// <summary>
    /// プールオブジェクトの基底処理、初期座標の設定と経過時間を初期化をし、オブジェクトをアクティブにする
    /// </summary>
    /// <param name="position"></param>
    public virtual void SetUp(Vector3 position)
    {
        transform.position = position;
        _elapsedTime = 0f;
        gameObject.SetActive(true);
    }

    protected virtual void Update()
    {
        _elapsedTime += Time.deltaTime;
        if (_elapsedTime > _lifeTime)
        {
            ReturnToPool();
        }
    }

    protected virtual void ReturnToPool()
    {
        _elapsedTime = 0f;
        gameObject.SetActive(false);
        _returnToPool?.Invoke(this as T);
    }

    /// <summary>
    /// Poolに返す
    /// </summary>
    public void Return()
    {
        ReturnToPool();
    }
}
