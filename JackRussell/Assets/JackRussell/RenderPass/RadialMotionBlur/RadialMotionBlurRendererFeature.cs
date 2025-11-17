using UnityEngine;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule.Util;

// Radial Motion Blur Pass - Creates a radial blur effect emanating from a center point
public class RadialMotionBlurPass : ScriptableRenderPass
{
    const string m_PassName = "RadialMotionBlurPass";

    // Material used in the blit operation
    Material m_BlitMaterial;
    
    // Shader property IDs for performance
    static readonly int s_BlurStrengthID = Shader.PropertyToID("_BlurStrength");
    static readonly int s_BlurCenterID = Shader.PropertyToID("_BlurCenter");
    static readonly int s_SampleCountID = Shader.PropertyToID("_SampleCount");
    
    // Effect parameters
    float m_BlurStrength;
    Vector2 m_BlurCenter;
    int m_SampleCount;

    // Function to transfer settings from the renderer feature to the render pass
    public void Setup(Material mat, float blurStrength, Vector2 blurCenter, int sampleCount)
    {
        m_BlitMaterial = mat;
        m_BlurStrength = blurStrength;
        m_BlurCenter = blurCenter;
        m_SampleCount = Mathf.Clamp(sampleCount, 4, 32);

        // The pass will read the current color texture. That needs to be an intermediate texture.
        // It's not supported to use the BackBuffer as input texture.
        requiresIntermediateTexture = true;
    }

    public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
    {
        // Get the resource data containing all texture handles
        var resourceData = frameData.Get<UniversalResourceData>();

        // Safety check - we need an intermediate texture
        if (resourceData.isActiveTargetBackBuffer)
        {
            Debug.LogError($"Skipping render pass. RadialMotionBlurRendererFeature requires an intermediate ColorTexture, we can't use the BackBuffer as a texture input.");
            return;
        }

        // Get source texture
        var source = resourceData.activeColorTexture;
        
        // Create destination texture with same properties as source
        var destinationDesc = renderGraph.GetTextureDesc(source);
        destinationDesc.name = $"CameraColor-{m_PassName}";
        destinationDesc.clearBuffer = false;
        TextureHandle destination = renderGraph.CreateTexture(destinationDesc);

        // Set shader properties
        if (m_BlitMaterial != null)
        {
            m_BlitMaterial.SetFloat(s_BlurStrengthID, m_BlurStrength);
            m_BlitMaterial.SetVector(s_BlurCenterID, m_BlurCenter);
            m_BlitMaterial.SetInt(s_SampleCountID, m_SampleCount);
        }

        // Setup blit parameters
        RenderGraphUtils.BlitMaterialParameters para = new(source, destination, m_BlitMaterial, 0);
        renderGraph.AddBlitPass(para, passName: m_PassName);

        // Swap the camera color to our new texture
        // This optimization avoids an extra blit back to the original color target
        resourceData.cameraColor = destination;
    }
}

public class RadialMotionBlurRendererFeature : ScriptableRendererFeature
{    
    [System.Serializable]
    public class RadialBlurSettings
    {
        [Tooltip("The material/shader used for the radial motion blur effect.")]
        public Material material;
        
        [Tooltip("The event where to inject the pass.")]
        public RenderPassEvent renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
        
        [Tooltip("Strength of the blur effect. Higher values create more pronounced blur.")]
        [Range(0f, 0.1f)]
        public float blurStrength = 0.02f;
        
        [Tooltip("Center point of the radial blur in screen space (0,0 = bottom-left, 1,1 = top-right).")]
        public Vector2 blurCenter = new Vector2(0.5f, 0.5f);
        
        [Tooltip("Number of samples taken for the blur. More samples = smoother blur but lower performance.")]
        [Range(4, 32)]
        public int sampleCount = 12;
    }
    
    public RadialBlurSettings settings = new RadialBlurSettings();

    RadialMotionBlurPass m_Pass;

    // Initialize the pass - called on serialization
    public override void Create()
    {
        m_Pass = new RadialMotionBlurPass();
        m_Pass.renderPassEvent = settings.renderPassEvent;
    }

    // Enqueue the pass in the renderer - called once per camera
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        // Early exit if there's no material
        if (settings.material == null)
        {
            Debug.LogWarning(this.name + " material is null and will be skipped.");
            return;
        }

        // Setup pass with current settings
        m_Pass.Setup(
            settings.material, 
            settings.blurStrength, 
            settings.blurCenter, 
            settings.sampleCount
        );
        
        renderer.EnqueuePass(m_Pass);
    }
}