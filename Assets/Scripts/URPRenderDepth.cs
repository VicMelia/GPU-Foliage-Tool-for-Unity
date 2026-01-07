using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class URPRenderDepth : ScriptableRendererFeature
{
    class RenderDepthPass : ScriptableRenderPass
    {
        public Material depthMaterial;
        RenderTargetIdentifier source;
        RenderTargetHandle tempTexture;
        RenderTargetHandle sobelTexture;

        public RenderDepthPass(Material material)
        {
            this.depthMaterial = material;
            tempTexture.Init("_TempTexture");
            sobelTexture.Init("_SobelTexture");
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            source = renderingData.cameraData.renderer.cameraColorTarget;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get("Render Depth Texture");

            RenderTextureDescriptor opaqueDesc = renderingData.cameraData.cameraTargetDescriptor;
            opaqueDesc.depthBufferBits = 0;

            cmd.GetTemporaryRT(tempTexture.id, opaqueDesc, FilterMode.Bilinear);
            cmd.GetTemporaryRT(sobelTexture.id, opaqueDesc, FilterMode.Bilinear);

            Blit(cmd, source, tempTexture.Identifier(), depthMaterial, 0);
            Blit(cmd, tempTexture.Identifier(), sobelTexture.Identifier(), depthMaterial, 0);
            Blit(cmd, sobelTexture.Identifier(), source);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(tempTexture.id);
            cmd.ReleaseTemporaryRT(sobelTexture.id);
        }
    }

    RenderDepthPass renderDepthPass;
    public Material depthMaterial;

    public override void Create()
    {
        renderDepthPass = new RenderDepthPass(depthMaterial)
        {
            renderPassEvent = RenderPassEvent.AfterRenderingOpaques
        };
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        renderer.EnqueuePass(renderDepthPass);
    }
}