using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

/// <summary>
/// URP Renderer Feature that applies a pixelation effect by snapping each fragment
/// to a configurable pixel-block grid. Add this to your URP Renderer asset.
/// </summary>
[DisallowMultipleRendererFeature("Pixelation Effect")]
public class PixelationRendererFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        [Range(1, 16)]
        [Tooltip("Size of each pixel block in screen pixels. Higher = more pixelated.")]
        public int pixelSize = 4;
    }

    public Settings settings = new Settings();

    private PixelationPass _pass;
    private Material _material;

    private static readonly int s_PixelSizeId = Shader.PropertyToID("_PixelSize");

    // ── Lifecycle ─────────────────────────────────────────────────────────────

    public override void Create()
    {
        Shader shader = Shader.Find("Custom/Pixelation");
        if (shader == null)
        {
            Debug.LogWarning("[PixelationRendererFeature] Shader 'Custom/Pixelation' not found.");
            return;
        }

        _material = CoreUtils.CreateEngineMaterial(shader);
        _pass = new PixelationPass();
        _pass.renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (_material == null || _pass == null) return;

        _material.SetInt(s_PixelSizeId, settings.pixelSize);
        _pass.Setup(_material);
        renderer.EnqueuePass(_pass);
    }

    protected override void Dispose(bool disposing)
    {
        CoreUtils.Destroy(_material);
    }

    // ── Render Pass ───────────────────────────────────────────────────────────

    private class PixelationPass : ScriptableRenderPass
    {
        private Material _material;

        public void Setup(Material material)
        {
            _material = material;
            requiresIntermediateTexture = true;
        }

        private class PassData
        {
            public TextureHandle source;
            public Material material;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (_material == null) return;

            var resourceData = frameData.Get<UniversalResourceData>();

            // Skip if rendering directly to the backbuffer.
            if (resourceData.isActiveTargetBackBuffer) return;

            TextureHandle srcHandle = resourceData.activeColorTexture;

            TextureDesc desc = renderGraph.GetTextureDesc(srcHandle);
            desc.name = "PixelationResult";
            desc.clearBuffer = false;
            TextureHandle dstHandle = renderGraph.CreateTexture(desc);

            using (var builder = renderGraph.AddRasterRenderPass<PassData>("Pixelation Effect", out var passData))
            {
                passData.source = srcHandle;
                passData.material = _material;

                builder.UseTexture(srcHandle);
                builder.SetRenderAttachment(dstHandle, 0);

                builder.SetRenderFunc(static (PassData data, RasterGraphContext ctx) =>
                {
                    Blitter.BlitTexture(ctx.cmd, data.source, new Vector4(1f, 1f, 0f, 0f), data.material, 0);
                });
            }

            // Redirect camera color to the pixelated result.
            resourceData.cameraColor = dstHandle;
        }
    }
}
