using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// プレハブのインスタンスを生成し、再利用を管理する基底クラス。
/// </summary>
/// <typeparam name="T">管理するオブジェクトの型。PooledObjectを継承している必要がある。</typeparam>
public abstract class ObjectPool<T> : MonoBehaviour where T : PooledObject<T>
{
    [SerializeField] private T _prefab;
    [SerializeField] private int _poolCount = 10;

    private readonly Queue<T> _pool = new();
    private readonly List<T> _allObjects = new();

    /// <summary>
    /// プールする数
    /// </summary>
    public int PoolCount => _poolCount;

    public virtual void Initialize()
    {
        for (int i = 0; i < _poolCount; i++)
        {
            var obj = Instantiate(_prefab, transform);
            obj.gameObject.SetActive(false);
            _allObjects.Add(obj);
            _pool.Enqueue(obj);
            obj.Initialize(Return);
        }
    }

    /// <summary>
    /// Poolしてるオブジェクトを取得
    /// </summary>
    /// <param name="obj">取得したオブジェクト</param>
    /// <returns>取得に成功したらtrue</returns>
    public bool TryGet(out T obj)
    {
        if (_pool.Count > 0)
        {
            obj = _pool.Dequeue();
            return true;
        }
        obj = null;
        return false;
    }

    /// <summary>
    /// 再度Poolする
    /// </summary>
    /// <param name="obj">返すオブジェクト</param>
    public void Return(T obj)
    {
        _pool.Enqueue(obj);
    }

    /// <summary>
    /// アクティブなオブジェクトを取得
    /// </summary>
    /// <returns>アクティブなオブジェクト</returns>
    public T[] GetActiveObjects()
    {
        return _allObjects.Where(o => o.gameObject.activeSelf).ToArray();
    }
}
