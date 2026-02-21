using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

public class WorldGlitchRendererFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        public Material glitchMaterial;
        public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
    }

    public Settings settings = new Settings();
    private WorldGlitchPass pass;

    public override void Create()
    {
        if (settings.glitchMaterial != null)
        {
            pass = new WorldGlitchPass(settings.glitchMaterial)
            {
                renderPassEvent = settings.renderPassEvent
            };
        }
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (pass == null || settings.glitchMaterial == null) return;

        // 仅在 Game 窗口和 Scene 窗口渲染
        if (renderingData.cameraData.cameraType != CameraType.Game && 
            renderingData.cameraData.cameraType != CameraType.SceneView) 
            return;

        // ✅ 仅在 GameRoot 标记开启时入队
        if (GameRoot.I != null && GameRoot.I.IsGlitchWorld)
        {
            renderer.EnqueuePass(pass);
        }
    }

    class WorldGlitchPass : ScriptableRenderPass
    {
        private Material material;

        public WorldGlitchPass(Material mat)
        {
            this.material = mat;
        }

        private class PassData
        {
            public TextureHandle src;
            public Material material;
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            if (resourceData.isActiveTargetBackBuffer) return;

            TextureHandle activeTexture = resourceData.activeColorTexture;
            
            // ✅ 使用 unscaledTime：即使游戏暂停/对话中，故障依然在跳动
            material.SetFloat("_TimeX", Time.unscaledTime); 
            
            // 💡 注意：这里删除了 material.SetFloat("_Intensity", 1.0f);
            // 现在你可以直接在 Material 面板上手动调节 Intensity 的大小了

            TextureDesc desc = renderGraph.GetTextureDesc(activeTexture);
            desc.name = "_WorldGlitchTempTexture";
            desc.clearBuffer = false;
            TextureHandle tempTexture = renderGraph.CreateTexture(desc);

            using (var builder = renderGraph.AddRasterRenderPass<PassData>("WorldGlitch_Apply", out var passData))
            {
                passData.src = activeTexture;
                passData.material = material;

                builder.UseTexture(passData.src, AccessFlags.Read);
                builder.SetRenderAttachment(tempTexture, 0, AccessFlags.Write);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    Blitter.BlitTexture(context.cmd, data.src, new Vector4(1, 1, 0, 0), data.material, 0);
                });
            }

            using (var builder = renderGraph.AddRasterRenderPass<PassData>("WorldGlitch_CopyBack", out var passData))
            {
                passData.src = tempTexture;
                builder.UseTexture(passData.src, AccessFlags.Read);
                builder.SetRenderAttachment(activeTexture, 0, AccessFlags.Write);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    Blitter.BlitTexture(context.cmd, data.src, new Vector4(1, 1, 0, 0), 0, false);
                });
            }
        }
    }

    protected override void Dispose(bool disposing) { }
}