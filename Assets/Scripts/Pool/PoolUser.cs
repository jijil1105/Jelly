using UnityEngine;

/// <summary>
/// プールからオブジェクトを取得して初期化する基底クラス。
/// </summary>
/// <typeparam name="T">プールで管理するオブジェクト型（PooledObject を継承）</typeparam>
public abstract class PoolUser<T> : MonoBehaviour where T : PooledObject<T>
{
    [SerializeField] private ObjectPool<T> _pool;

    /// <inheritdoc/>
    public T[] GetActiveObjects() => _pool.GetActiveObjects();
    /// <inheritdoc/>
    public int PoolCount => _pool.PoolCount;

    public virtual void Initialize()
    {
        _pool.Initialize();
    }

    public virtual void Spawn(params object[] args)
    {
        if (_pool.TryGet(out var obj))
        {
            Setup(obj, args);
        }
    }

    protected abstract void Setup(T obj, params object[] args);
}
