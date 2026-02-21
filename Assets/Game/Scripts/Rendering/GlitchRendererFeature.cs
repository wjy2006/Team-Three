using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

public class GlitchRendererFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        public Material glitchMaterial;
        // 2D 渲染器中，建议默认设为 BeforeRenderingPostProcessing
        public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
    }

    public Settings settings = new Settings();
    private GlitchPass glitchPass;

    public override void Create()
    {
        if (settings.glitchMaterial != null)
        {
            glitchPass = new GlitchPass(settings.glitchMaterial)
            {
                renderPassEvent = settings.renderPassEvent
            };
        }
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (glitchPass == null || settings.glitchMaterial == null) return;

        // 排除掉不必要的相机（如反射相机）
        if (renderingData.cameraData.cameraType == CameraType.Game || renderingData.cameraData.cameraType == CameraType.SceneView)
        {
            renderer.EnqueuePass(glitchPass);
        }
    }

    class GlitchPass : ScriptableRenderPass
    {
        private Material material;

        public GlitchPass(Material mat)
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
            // 1. 获取 Volume 数据
            var volume = VolumeManager.instance.stack.GetComponent<GlitchVolume>();
            if (volume == null || !volume.IsActive()) return;

            // 2. 获取渲染资源
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            if (resourceData.isActiveTargetBackBuffer) return;

            TextureHandle activeTexture = resourceData.activeColorTexture;
            
            // 3. 配置材质
            material.SetFloat("_Intensity", volume.intensity.value);
            material.SetFloat("_TimeX", Time.time); // 传入时间驱动噪声

            // 4. 创建临时纹理用于 Blit
            TextureDesc desc = renderGraph.GetTextureDesc(activeTexture);
            desc.name = "_GlitchTempTexture";
            desc.clearBuffer = false;
            TextureHandle tempTexture = renderGraph.CreateTexture(desc);

            // 过程 A: 主纹理 -> 材质处理 -> 临时纹理
            using (var builder = renderGraph.AddRasterRenderPass<PassData>("Glitch_Apply", out var passData))
            {
                passData.src = activeTexture;
                passData.material = material;

                builder.UseTexture(passData.src, AccessFlags.Read);
                builder.SetRenderAttachment(tempTexture, 0, AccessFlags.Write);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    // Unity 6 必须使用 Blitter
                    Blitter.BlitTexture(context.cmd, data.src, new Vector4(1, 1, 0, 0), data.material, 0);
                });
            }

            // 过程 B: 临时纹理 -> 拷贝回主纹理
            using (var builder = renderGraph.AddRasterRenderPass<PassData>("Glitch_CopyBack", out var passData))
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