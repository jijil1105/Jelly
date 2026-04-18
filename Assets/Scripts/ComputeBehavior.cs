using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// クラゲ、蝶、蝶の軌跡の座標、弾の当たり判定を計算するクラス
/// </summary>
public class ComputeBehavior : MonoBehaviour
{
    [Header("GPUインスタンシングに使うメッシュとマテリアル")]
    [Tooltip("GPUインスタンシングで使うクラゲのメッシュ")]
    [SerializeField] private Mesh _jellyFishMesh;
    [Tooltip("クラゲのインスタンシング用マテリアル（GPU Instancing を有効にすること）")]
    [SerializeField] private Material _jellyFishMaterial;

    [Tooltip("蝶のメッシュ")]
    [SerializeField] private Mesh _butterflyMesh;
    [Tooltip("蝶のインスタンシング用マテリアル")]
    [SerializeField] private Material _butterflyMaterial;
    [Tooltip("蝶の軌跡レンダリング用マテリアル")]
    [SerializeField] private Material _butterflyTrailMaterial;

    [Header("コンピュートシェーダー")]
    [Tooltip("クラゲと蝶の座標・回転を計算する ComputeShader")]
    [SerializeField] private ComputeShader _computeShader;
    [Tooltip("バッファをクリアするための ComputeShader")]
    [SerializeField] private ComputeShader _clearShader;

    [Header("シミュレーションのパラメータ")]
    [Tooltip("インスタンス数")]
    [SerializeField] private int _instanceCount = 1024;
    [Tooltip("時間のスケール（アニメーション速度）")]
    [SerializeField] private float _timeScale = 1f;
    [Tooltip("シミュレーション領域のサイズ（ワールド単位）")]
    [SerializeField] private float _boundsSize = 100f;
    [Tooltip("軌跡の長さ（サンプル数）")]
    [SerializeField] private int _trailLength = 32;
    [Tooltip("蝶の軌跡を構成するセグメントあたりの頂点数")]
    [SerializeField] private int _butterflyVertsPerSeg = 4;
    [Tooltip("蝶の軌跡サンプリング間隔")]
    [SerializeField] private float _butterflyTrailSampleInterval = 0.2f;
    [Tooltip("クラゲや蝶のフレーム間での進行方向の変化率")]
    [SerializeField] private float _smoothFactor = 0.2f;
    [Tooltip("spectrum の値にかけ合わせて調整する振幅係数")]
    [SerializeField] private float _amp = 1.0f;

    // クラゲ、蝶、蝶の軌跡、弾のデータを保持するバッファ
    private GraphicsBuffer _jellyFishBuffer;
    private GraphicsBuffer _butterflyBuffer;
    private GraphicsBuffer _butterflyTrailBuffer;

    private int _kernel;
    // 蝶の軌跡用の総頂点数
    private int _totalButterflyTrailVerts;

    // バグも起きてないので可動性重視でpadding無し。
    [StructLayout(LayoutKind.Sequential)]
    struct JellyFishData
    {
        public Vector3 jellyFishPos;
        public Vector3 jellyFishDir;
        public Vector3 jellyFishColor;
        public float size;
        public uint dead;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct ButterflyData
    {
        public Vector3 butterflyPos;
        public Vector3 butterflyDir;
        public Vector3 butterflyColor;
        public float size;
        public uint trailWriteIndex;
        public float trailAccum;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct TrailPoint
    {
        public Vector3 pos;
        public float time;
    };

    struct BulletData
    {
        public Vector3 position;
        public uint isHit;
    }

    public void Initialize()
    {
        // コンピュートシェーダにセットするバッファを初期化
        InitializeBuffer();
        // バッファをクリア
        ClearGpuBuffers(_instanceCount, _trailLength);
        // コンピュートシェーダーにバッファと定数をセット
        SetupComputeShader();
        // GPUインスタンシングに使うマテリアルにバッファと定数をセット
        SetupGpuInstancingMaterials();

        int segs = Mathf.Max(1, _trailLength - 1);
        _totalButterflyTrailVerts = _instanceCount * segs * _butterflyVertsPerSeg;
    }

    public void OnUpdate(float[] getLogBands)
    {
        var spectrum = getLogBands[5] * _amp;
        _jellyFishMaterial.SetFloat("_Spectrum", spectrum);

        // コンピュートシェーダーの時間を更新
        _computeShader.SetFloat("_Time", Time.time);
        _computeShader.SetFloat("_TimeScale", _timeScale);
        _computeShader.SetFloat("_DeltaTime", Time.deltaTime);

        // クラゲ、蝶の座標と回転、弾の当たり判定を計算
        _computeShader.Dispatch(_kernel, Mathf.CeilToInt(_instanceCount / 64.0f), 1, 1);
    }

    public void OnLateUpdate()
    {
        // 指定した範囲内にクラゲを描画
        var bounds = new Bounds(transform.position, Vector3.one * _boundsSize);
        Graphics.DrawMeshInstancedProcedural(_jellyFishMesh, 0, _jellyFishMaterial, bounds, _instanceCount, null,
            ShadowCastingMode.Off, false, gameObject.layer);
        
        // 指定した範囲内に蝶を描画
        Graphics.DrawMeshInstancedProcedural(_butterflyMesh, 0, _butterflyMaterial, bounds, _instanceCount, null,
            ShadowCastingMode.Off, false, gameObject.layer);

        Graphics.DrawProcedural(_butterflyTrailMaterial, bounds, MeshTopology.Points, _totalButterflyTrailVerts);
    }

    private void InitializeBuffer()
    {
        // クラゲ用のバッファを作成
        int stride = Marshal.SizeOf(typeof(JellyFishData));
        _jellyFishBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _instanceCount, stride);

        // 蝶用のバッファを作成
        stride = Marshal.SizeOf(typeof(ButterflyData));
        _butterflyBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _instanceCount, stride);

        // 蝶のトレイル用のバッファを作成
        stride = Marshal.SizeOf(typeof(TrailPoint));
        _butterflyTrailBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, _instanceCount * _trailLength, Marshal.SizeOf(typeof(TrailPoint)));
    }

    private void SetupComputeShader()
    {
        // コンピュートシェーダーにバッファと定数をセット
        _kernel = _computeShader.FindKernel("CSMain");
        _computeShader.SetBuffer(_kernel, "_jellyFishBuffer", _jellyFishBuffer);
        _computeShader.SetBuffer(_kernel, "_butterflyBuffer", _butterflyBuffer);
        _computeShader.SetBuffer(_kernel, "_butterflyTrailBuffer", _butterflyTrailBuffer);
        _computeShader.SetInt("_TrailLength", _trailLength);
        _computeShader.SetInt("_MaxCount", _instanceCount);
        _computeShader.SetFloat("_ButterflyTrailSampleInterval", _butterflyTrailSampleInterval);
        _computeShader.SetFloat("_SmoothFactor", _smoothFactor);
    }

    private void SetupGpuInstancingMaterials()
    {
        // クラゲのシェーダーにバッファをセット
        _jellyFishMaterial.SetBuffer("_jellyFishBuffer", _jellyFishBuffer);

        // 蝶のシェーダーにバッファをセット
        _butterflyMaterial.SetBuffer("_butterflyBuffer", _butterflyBuffer);

        // 蝶のトレイルシェーダーにバッファをセット
        _butterflyTrailMaterial.SetBuffer("_butterflyBuffer", _butterflyBuffer);
        _butterflyTrailMaterial.SetBuffer("_butterflyTrailBuffer", _butterflyTrailBuffer);
        _butterflyTrailMaterial.SetInt("_TrailLength", _trailLength);
        _butterflyTrailMaterial.SetInt("_VertsPerSeg", _butterflyVertsPerSeg);
        _butterflyTrailMaterial.SetInt("_MaxButterfly", _instanceCount);
    }

    void ClearGpuBuffers(int instanceCount, int trailLength)
    {
        int clearKernel = _clearShader.FindKernel("ClearBuffers");
        int clearTrailKernel = _clearShader.FindKernel("ClearTrailPoints");

        // 定数をセット
        int totalTrailPoints = instanceCount * trailLength;
        _clearShader.SetInt("_TotalTrailPoints", totalTrailPoints);
        _clearShader.SetInt("_InstanceCount", instanceCount);

        // バッファをセット
        _clearShader.SetBuffer(clearKernel, "_jellyFishBuffer", _jellyFishBuffer);
        _clearShader.SetBuffer(clearKernel, "_butterflyBuffer", _butterflyBuffer);
        _clearShader.SetBuffer(clearTrailKernel, "_butterflyTrailBuffer", _butterflyTrailBuffer);

        int threads = 64;
        int groupsInstancing = Mathf.CeilToInt((float)instanceCount / threads);
        int groupsTrail = Mathf.CeilToInt((float)totalTrailPoints / threads);

        _clearShader.Dispatch(clearKernel, Mathf.Max(1, groupsInstancing), 1, 1);
        _clearShader.Dispatch(clearTrailKernel, Mathf.Max(1, groupsTrail), 1, 1);
    }


    private void OnDestroy()
    {
        _jellyFishBuffer?.Dispose();
        _butterflyBuffer?.Dispose();
        _butterflyTrailBuffer?.Dispose();
    }
}
