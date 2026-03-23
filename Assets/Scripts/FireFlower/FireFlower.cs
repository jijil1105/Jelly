using UnityEngine;
using UnityEngine.VFX;

/// <summary>
/// クラゲに当たった時に出すエフェクト
/// </summary>
public class FireFlower : PooledObject<FireFlower>
{
    [SerializeField] VisualEffect _effect;

    /// <summary>
    /// 初期座標とエフェクトの透明度を設定し、エフェクトを出す
    /// </summary>
    /// <param name="position">初期座標</param>
    /// <param name="alpha">エフェクトの透明度</param>
    public void SetUp(Vector3 position, float alpha)
    {
        base.SetUp(position);
        _effect.SetFloat("OutputAlpha", alpha);
        _effect.SendEvent("Play");
    }

    protected override void ReturnToPool()
    {
        base.ReturnToPool();
        _effect.SendEvent("Stop");
    }
}
