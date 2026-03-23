using UnityEngine.Rendering;

[System.Serializable, VolumeComponentMenu("Post-processing/Dot Painting")]
public class DotPaintingVolume : VolumeComponent, IPostProcessComponent
{
    public BoolParameter isActive = new BoolParameter(false);

    // ドット絵シェーダーのプロパティ
    public ClampedFloatParameter dotSize = new ClampedFloatParameter(8f, 1f, 256f); // _DotSize
    public ClampedFloatParameter posterizeLevels = new ClampedFloatParameter(6f, 2f, 100f); // _PosterizeLevels

    public bool IsActive() => isActive.value;
    public bool IsTileCompatible() => false;
}
