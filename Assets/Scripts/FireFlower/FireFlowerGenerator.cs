using UnityEngine;

/// <summary>
/// クラゲに弾が当たった時に出すエフェクトを生成するクラス
/// </summary>
public class FireFlowerGenerator : PoolUser<FireFlower>
{
    /// <summary>
    /// エフェクトスポーン出す
    /// </summary>
    /// <param name="position">初期座標</param>
    /// <param name="alpha">エフェクトの透明度</param>
    public void Spawn(Vector3 position, float alpha)
    {
        base.Spawn(position, alpha);
    }

    protected override void Setup(FireFlower obj, params object[] args)
    {
        var position = (Vector3)args[0];
        var alpha = (float)args[1];
        obj.SetUp(position, alpha);
    }
}
