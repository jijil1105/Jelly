using UnityEngine;

/// <summary>
/// ’e‚ð”­ŽË‚·‚éƒNƒ‰ƒX
/// </summary>
public class Shooter : PoolUser<Bullet>
{
    [SerializeField] private float _interval;
    private float _elapsedTime = 0;

    public override void Initialize()
    {
        base.Initialize();
        _elapsedTime = _interval;
    }

    public void Spawn(Vector3 direction)
    {
        if (_elapsedTime > _interval)
        {
            base.Spawn(direction);
        }
    }

    public void OnUpdate()
    {
        _elapsedTime += Time.deltaTime;
    }

    protected override void Setup(Bullet obj, params object[] args)
    {
        _elapsedTime = 0;
        var dir = (Vector3)args[0];
        obj.SetUp(transform.position, dir);
    }
}
