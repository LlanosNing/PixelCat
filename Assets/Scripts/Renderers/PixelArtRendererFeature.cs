using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Unity 2022+ Compatible Version
public class PixelArtRendererFeature : ScriptableRendererFeature
{
    [System.Serializable]
    public class Settings
    {
        [Header("Resolution")]
        [Tooltip("Target render height in pixels (width calculated from aspect ratio)")]
        public int targetHeight = 180;

        [Header("Color Quantization")]
        [Tooltip("Number of color steps per channel (lower = more retro)")]
        [Range(2, 32)]
        public int colorSteps = 8;

        [Header("Dithering")]
        [Tooltip("Dither strength for smoother color transitions")]
        [Range(0f, 1f)]
        public float ditherStrength = 0.2f;

        [Header("Render Settings")]
        [Tooltip("When to apply the effect")]
        public RenderPassEvent renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;

        [Tooltip("Controls the sharpness of the pixels (0 = Smooth/Bilinear, 1 = Sharp/Point)")]
        [Range(0f, 1f)]
        public float sharpness = 1.0f;

        [Header("Shader")]
        [Tooltip("Assign the PixelArt shader here")]
        public Shader pixelArtShader;
    }

    public Settings settings = new Settings();
    private PixelArtRenderPass renderPass;
    private Material pixelArtMaterial;

    public override void Create()
    {
        name = "Pixel Art Effect";

        // Create material
        if (settings.pixelArtShader != null)
        {
            pixelArtMaterial = CoreUtils.CreateEngineMaterial(settings.pixelArtShader);
        }
        else
        {
            // Try to find shader automatically
            Shader shader = Shader.Find("Hidden/PixelArt");
            if (shader != null)
            {
                pixelArtMaterial = CoreUtils.CreateEngineMaterial(shader);
            }
            else
            {
                Debug.LogWarning("[PixelArt] Shader not found. Please assign the shader in the Renderer Feature settings.");
                return;
            }
        }

        renderPass = new PixelArtRenderPass(settings);
        renderPass.renderPassEvent = settings.renderPassEvent;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (renderPass == null || pixelArtMaterial == null)
            return;

        renderPass.Setup(pixelArtMaterial, settings);
        renderer.EnqueuePass(renderPass);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            CoreUtils.Destroy(pixelArtMaterial);
        }
    }
}
public class PixelArtRenderPass : ScriptableRenderPass
{
    private Material material;
    private PixelArtRendererFeature.Settings settings;

    private static readonly int ColorStepsID = Shader.PropertyToID("_ColorSteps");
    private static readonly int DitherStrengthID = Shader.PropertyToID("_DitherStrength");
    private static readonly int SharpnessID = Shader.PropertyToID("_Sharpness");

    public PixelArtRenderPass(PixelArtRendererFeature.Settings settings)
    {
        this.settings = settings;
        profilingSampler = new ProfilingSampler("PixelArt Effect");
    }

    public void Setup(Material mat, PixelArtRendererFeature.Settings newSettings)
    {
        this.material = mat;
        this.settings = newSettings;
        this.renderPassEvent = settings.renderPassEvent;
    }

    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        var descriptor = renderingData.cameraData.cameraTargetDescriptor;
        descriptor.depthBufferBits = 0;
    }

    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        if (material == null)
            return;

        var cameraData = renderingData.cameraData;
        if (cameraData.camera.cameraType != CameraType.Game)
            return;

        CommandBuffer cmd = CommandBufferPool.Get("PixelArt Effect");

        // Get camera dimensions
        int screenWidth = cameraData.camera.scaledPixelWidth;
        int screenHeight = cameraData.camera.scaledPixelHeight;

        // Calculate low-res dimensions
        int lowResHeight = settings.targetHeight;
        int lowResWidth = Mathf.RoundToInt(lowResHeight * ((float)screenWidth / screenHeight));

        // Ensure minimum size
        lowResWidth = Mathf.Max(lowResWidth, 1);
        lowResHeight = Mathf.Max(lowResHeight, 1);

        // Set shader properties
        material.SetFloat(ColorStepsID, settings.colorSteps);
        material.SetFloat(DitherStrengthID, settings.ditherStrength);
        material.SetFloat(SharpnessID, settings.sharpness);

        // Get source
        var renderer = cameraData.renderer;
        var source = renderer.cameraColorTargetHandle;

        // Create temporary render textures
        int tempLowResID = Shader.PropertyToID("_TempLowRes");

        // Allocate low-res texture with Bilinear filtering (shader will handle sharpness)
        cmd.GetTemporaryRT(tempLowResID, lowResWidth, lowResHeight, 0, FilterMode.Bilinear, RenderTextureFormat.ARGB32);

        // Downsample to low resolution with color quantization (Pass 0)
        cmd.Blit(source, tempLowResID, material, 0);

        // Upsample back to screen with sharpness control (Pass 1)
        cmd.Blit(tempLowResID, source, material, 1);

        // Cleanup
        cmd.ReleaseTemporaryRT(tempLowResID);

        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    public override void OnCameraCleanup(CommandBuffer cmd)
    {
    }

    public void Dispose()
    {
    }
}

