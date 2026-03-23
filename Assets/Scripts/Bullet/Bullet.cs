using UnityEngine;

/// <summary>
/// Poolで管理される弾を表現したクラス
/// </summary>
public class Bullet : PooledObject<Bullet>
{
    [SerializeField] private float _speed = 10f;

    /// <summary>
    /// 初期座標と角度を設定
    /// </summary>
    /// <param name="position">初期座標</param>
    /// <param name="direction">初期角度</param>
    public void SetUp(Vector3 position, Vector3 direction)
    {
        base.SetUp(position);
        if (direction.sqrMagnitude > 0f)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }
    
    protected override void Update()
    {
        base.Update();
        transform.position += transform.forward * _speed * Time.deltaTime;
    }
}
