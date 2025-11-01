using UnityEngine;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule.Util;

// Radial Blur Effect Pass - Creates a radial blur emanating from screen center
// Perfect for conveying motion, speed boost, or high-velocity effects
public class RadialBlurPass : ScriptableRenderPass
{
    const string m_PassName = "RadialBlurPass";

    // Material used for the radial blur effect
    Material m_RadialBlurMaterial;

    // Shader property IDs for better performance
    static readonly int SamplesID = Shader.PropertyToID("_Samples");
    static readonly int DecayID = Shader.PropertyToID("_Decay");
    static readonly int DensityID = Shader.PropertyToID("_Density");
    static readonly int WeightID = Shader.PropertyToID("_Weight");
    static readonly int CenterXID = Shader.PropertyToID("_CenterX");
    static readonly int CenterYID = Shader.PropertyToID("_CenterY");
    static readonly int BlurStrengthID = Shader.PropertyToID("_BlurStrength");
    static readonly int EffectIntensityID = Shader.PropertyToID("_EffectIntensity");
    static readonly int BrightnessID = Shader.PropertyToID("_Brightness");

    public void Setup(Material material, float samples, float decay, float density, float weight, float centerX, float centerY, float blurStrength, float effectIntensity, float brightness)
    {
        m_RadialBlurMaterial = material;

        // Set shader properties
        if (m_RadialBlurMaterial != null)
        {
            m_RadialBlurMaterial.SetFloat(SamplesID, samples);
            m_RadialBlurMaterial.SetFloat(DecayID, decay);
            m_RadialBlurMaterial.SetFloat(DensityID, density);
            m_RadialBlurMaterial.SetFloat(WeightID, weight);
            m_RadialBlurMaterial.SetFloat(CenterXID, centerX);
            m_RadialBlurMaterial.SetFloat(CenterYID, centerY);
            m_RadialBlurMaterial.SetFloat(BlurStrengthID, blurStrength);
            m_RadialBlurMaterial.SetFloat(EffectIntensityID, effectIntensity);
            m_RadialBlurMaterial.SetFloat(BrightnessID, brightness);
        }

        // We need an intermediate texture since we're sampling the current color buffer
        requiresIntermediateTexture = true;
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        var resourceData = frameData.Get<UniversalResourceData>();

        // Safety check for back buffer
        if (resourceData.isActiveTargetBackBuffer)
        {
            Debug.LogError($"Skipping render pass. RadialBlurEffect requires an intermediate ColorTexture, can't use BackBuffer as texture input.");
            return;
        }

        // Get source texture
        var source = resourceData.activeColorTexture;

        // Create destination texture with same properties as source
        var destinationDesc = renderGraph.GetTextureDesc(source);
        destinationDesc.name = $"CameraColor-{m_PassName}";
        destinationDesc.clearBuffer = false;

        TextureHandle destination = renderGraph.CreateTexture(destinationDesc);

        // Perform the blit with radial blur material
        RenderGraphUtils.BlitMaterialParameters para = new(source, destination, m_RadialBlurMaterial, 0);
        renderGraph.AddBlitPass(para, passName: m_PassName);

        // Swap the camera color buffer to our processed texture
        resourceData.cameraColor = destination;
    }
}

public class RadialBlurRendererFeature : ScriptableRendererFeature
{
    [Header("Material")]
    [Tooltip("The radial blur effect material")]
    public Material radialBlurMaterial;

    [Header("Radial Blur Parameters")]
    [Tooltip("Number of samples for the blur effect")]
    [Range(8, 64)]
    public float samples = 24.0f;

    [Tooltip("Decay factor for sample weighting")]
    [Range(0.8f, 1f)]
    public float decay = 0.97f;

    [Tooltip("Density controls the blur spread")]
    [Range(0.1f, 2f)]
    public float density = 0.5f;

    [Tooltip("Initial weight for samples")]
    [Range(0.01f, 0.5f)]
    public float weight = 0.1f;

    [Tooltip("X position of the blur center (0-1)")]
    [Range(0f, 1f)]
    public float centerX = 0.5f;

    [Tooltip("Y position of the blur center (0-1)")]
    [Range(0f, 1f)]
    public float centerY = 0.5f;

    [Tooltip("Overall strength of the blur effect")]
    [Range(0f, 2f)]
    public float blurStrength = 1.0f;

    [Tooltip("Effect intensity (0 = off, 1 = full effect)")]
    [Range(0f, 1f)]
    public float effectIntensity = 0.0f;

    [Tooltip("Brightness control (1.0 = full blur effect, 0.0 = more natural/original look)")]
    [Range(0f, 1f)]
    public float brightness = 1.0f;


    [Header("Injection Point")]
    [Tooltip("When to inject the radial blur pass")]
    public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;

    RadialBlurPass m_Pass;

    public override void Create()
    {
        m_Pass = new RadialBlurPass();
        m_Pass.renderPassEvent = renderPassEvent;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        // Validation checks
        if (radialBlurMaterial == null)
        {
            Debug.LogWarning($"{this.name}: Radial Blur Material is null and will be skipped.");
            return;
        }

        // Setup and enqueue the pass
        m_Pass.Setup(radialBlurMaterial, samples, decay, density, weight, centerX, centerY, blurStrength, effectIntensity, brightness);
        renderer.EnqueuePass(m_Pass);
    }

    // Public access to material for external control (DOTween, etc.)
    public Material RadialBlurMaterial => radialBlurMaterial;
}
