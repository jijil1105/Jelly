using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

/// <summary>
/// ドット絵風のポストプロセスをレンダーパイプラインに追加する ScriptableRendererFeature。
/// ボリューム（DotPaintingVolume）で有効化された場合にパスを登録して処理を行う。
/// </summary>
public class DotPaintingFeature : ScriptableRendererFeature
{
    /// <summary>
    /// ドット描画を行うレンダーパスの実装。
    /// </summary>
    class DotPaintingPass : ScriptableRenderPass
    {
        private const string ShaderPath = "Custom/DotPaintingPostProcess";
        private Material _material;

        /// <summary>
        /// マテリアルを取得
        /// </summary>
        public Material Material => _material ?? (_material = CoreUtils.CreateEngineMaterial(ShaderPath));

        /// <summary>
        /// マテリアルを破棄
        /// </summary>
        public void Cleanup() => CoreUtils.Destroy(_material);

        private class PassData
        {
            public Material material;
            public TextureHandle source;
        }

        /// <summary>
        /// RenderGraph に対してこのパスの登録と描画処理を行う。
        /// カメラのアクティブなカラーテクスチャを読み取り元として、一時テクスチャへドット処理を描画する。
        /// 最後に一時テクスチャをカメラの色バッファへブリットする。
        /// </summary>
        /// <param name="renderGraph">現在の RenderGraph インスタンス</param>
        /// <param name="frameData">フレーム固有のコンテキスト（UniversalResourceData 等を含む）</param>
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (Material == null) return;

            
            var resourceData = frameData.Get<UniversalResourceData>();
            // カメラが現在描画しているアクティブなカラーテクスチャ
            var sourceHandle = resourceData.activeColorTexture;
            var desc = renderGraph.GetTextureDesc(sourceHandle);
            desc.name = "DotPaintingTemp";
            desc.clearBuffer = false;
            desc.msaaSamples = MSAASamples.None;
            desc.depthBufferBits = 0;
            // sourceHandleを元にドット絵の効果を加えたテクスチャをtempHandleに書き込む
            var tempHandle = renderGraph.CreateTexture(desc);
            
            // Dot絵の処理を行う
            using (var builder = renderGraph.AddRasterRenderPass<PassData>("DotPaintingPass", out var passData))
            {
                // ドット絵マテリアルとカメラが描画をソースに設定
                passData.material = Material;
                passData.source = sourceHandle;

                // tempHandleを書き込みターゲットに設定
                builder.SetRenderAttachment(tempHandle, 0, AccessFlags.Write);
                // カメラが描画をソースに設定
                builder.UseTexture(sourceHandle, AccessFlags.Read);
                
                builder.SetRenderFunc(static (PassData data, RasterGraphContext ctx) =>
                {
                    var cmd = ctx.cmd;
                    var mat = data.material;
                    var src = data.source;

                    // カメラ描画にDotの効果を加える
                    mat.SetTexture("_MainTex", src);
                    Blitter.BlitTexture(cmd, src, Vector2.one, mat, 0);
                });
            }
            // 一時テクスチャの内容をカメラの色バッファへ戻す
            renderGraph.AddBlitPass(tempHandle, sourceHandle, Vector2.one, Vector2.zero, passName: "BlitDotPaintingToCamera");
        }
    }

    /// <summary>
    /// ボリュームからマテリアルプロパティを設定するユーティリティ。
    /// ボリュームの値をマテリアルに転送する。
    /// </summary>
    /// <param name="mat">設定対象のマテリアル</param>
    public void SetMaterialPropertiesFromVolume(Material mat, DotPaintingVolume vol)
    {
        if (mat == null || vol == null) return;
        mat.SetFloat("_DotSize", vol.dotSize.value);
        mat.SetFloat("_PosterizeLevels", vol.posterizeLevels.value);
    }

    DotPaintingPass m_ScriptablePass;

    /// <summary>
    /// フィーチャー作成時にレンダーパスを生成する。
    /// </summary>
    public override void Create()
    {
        m_ScriptablePass = new DotPaintingPass
        {
            renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing
        };
    }

    /// <summary>
    /// レンダーパスをレンダラーに登録する。
    /// ボリュームが存在し有効な場合のみパスを Enqueue する。
    /// </summary>
    /// <param name="renderer">現在の ScriptableRenderer</param>
    /// <param name="renderingData">フレームのレンダリングデータ</param>
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        var stack = VolumeManager.instance.stack;
        var vol = stack.GetComponent<DotPaintingVolume>();
        if (vol == null || !vol.IsActive()) return;
        SetMaterialPropertiesFromVolume(m_ScriptablePass.Material, vol);
        renderer.EnqueuePass(m_ScriptablePass);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            // 動的に生成したマテリアルはリークするので明示的に破棄
            m_ScriptablePass?.Cleanup();
        }
    }
}
