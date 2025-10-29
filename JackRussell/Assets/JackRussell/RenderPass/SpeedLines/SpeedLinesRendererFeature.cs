using UnityEngine;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule.Util;

// Speed Lines Effect Pass - Creates radial speed lines emanating from screen center
// Perfect for conveying motion, speed boost, or high-velocity effects
public class SpeedLinesPass : ScriptableRenderPass
{
    const string m_PassName = "SpeedLinesPass";

    // Material used for the speed lines effect
    Material m_SpeedLinesMaterial;

    // Shader property IDs for better performance
    static readonly int IntensityID = Shader.PropertyToID("_Intensity");
    static readonly int SpeedID = Shader.PropertyToID("_Speed");
    static readonly int LineCountID = Shader.PropertyToID("_LineCount");
    static readonly int InnerRadiusID = Shader.PropertyToID("_InnerRadius");
    static readonly int OuterRadiusID = Shader.PropertyToID("_OuterRadius");
    static readonly int LineWidthID = Shader.PropertyToID("_LineWidth");
    static readonly int FadeStrengthID = Shader.PropertyToID("_FadeStrength");
    static readonly int ColorID = Shader.PropertyToID("_Color");

    public void Setup(Material material, float intensity, float animationSpeed, float lineCount,
                     float innerRadius, float outerRadius, float lineWidth, float fadeStrength, Color speedLinesColor)
    {
        m_SpeedLinesMaterial = material;

        // Set shader properties
        if (m_SpeedLinesMaterial != null)
        {
            m_SpeedLinesMaterial.SetFloat(IntensityID, intensity);
            m_SpeedLinesMaterial.SetFloat(SpeedID, animationSpeed);
            m_SpeedLinesMaterial.SetFloat(LineCountID, lineCount);
            m_SpeedLinesMaterial.SetFloat(InnerRadiusID, innerRadius);
            m_SpeedLinesMaterial.SetFloat(OuterRadiusID, outerRadius);
            m_SpeedLinesMaterial.SetFloat(LineWidthID, lineWidth);
            m_SpeedLinesMaterial.SetFloat(FadeStrengthID, fadeStrength);
            m_SpeedLinesMaterial.SetColor(ColorID, speedLinesColor);
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
            Debug.LogError($"Skipping render pass. SpeedLinesEffect requires an intermediate ColorTexture, can't use BackBuffer as texture input.");
            return;
        }

        // Get source texture
        var source = resourceData.activeColorTexture;

        // Create destination texture with same properties as source
        var destinationDesc = renderGraph.GetTextureDesc(source);
        destinationDesc.name = $"CameraColor-{m_PassName}";
        destinationDesc.clearBuffer = false;

        TextureHandle destination = renderGraph.CreateTexture(destinationDesc);

        // Perform the blit with speed lines material
        RenderGraphUtils.BlitMaterialParameters para = new(source, destination, m_SpeedLinesMaterial, 0);
        renderGraph.AddBlitPass(para, passName: m_PassName);

        // Swap the camera color buffer to our processed texture
        resourceData.cameraColor = destination;
    }
}

public class SpeedLinesRendererFeature : ScriptableRendererFeature
{
    [Header("Material")]
    [Tooltip("The speed lines effect material")]
    public Material speedLinesMaterial;

    [Header("Speed Lines Parameters")]
    [Tooltip("Overall intensity of the speed lines effect")]
    [Range(0f, 2f)]
    public float intensity = 1.0f;

    [Tooltip("Animation speed of the moving lines")]
    [Range(0f, 10f)]
    public float animationSpeed = 3.0f;

    [Tooltip("Number of radial speed lines")]
    [Range(8, 64)]
    public float lineCount = 24;

    [Header("Radial Settings")]
    [Tooltip("Inner radius where speed lines start")]
    [Range(0f, 1f)]
    public float innerRadius = 0.1f;

    [Tooltip("Outer radius where speed lines end")]
    [Range(0f, 2f)]
    public float outerRadius = 1.5f;

    [Tooltip("Width of individual speed lines")]
    [Range(0.01f, 0.1f)]
    public float lineWidth = 0.03f;

    [Tooltip("Fade strength from center to edges")]
    [Range(0f, 5f)]
    public float fadeStrength = 2.0f;

    [Header("Visual")]
    [Tooltip("Color of the speed lines")]
    public Color speedLinesColor = Color.white;

    [Header("Injection Point")]
    [Tooltip("When to inject the speed lines pass")]
    public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;

    SpeedLinesPass m_Pass;

    public override void Create()
    {
        m_Pass = new SpeedLinesPass();
        m_Pass.renderPassEvent = renderPassEvent;
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        // Validation checks
        if (speedLinesMaterial == null)
        {
            Debug.LogWarning($"{this.name}: Speed Lines Material is null and will be skipped.");
            return;
        }

        // Setup and enqueue the pass
        m_Pass.Setup(speedLinesMaterial, intensity, animationSpeed, lineCount,
                    innerRadius, outerRadius, lineWidth, fadeStrength, speedLinesColor);
        renderer.EnqueuePass(m_Pass);
    }

    // Public access to material for external control (DOTween, etc.)
    public Material SpeedLinesMaterial => speedLinesMaterial;
}