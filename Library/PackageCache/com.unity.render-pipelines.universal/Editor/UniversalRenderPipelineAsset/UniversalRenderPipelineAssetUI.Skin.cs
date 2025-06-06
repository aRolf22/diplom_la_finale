using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    internal partial class UniversalRenderPipelineAssetUI
    {
        internal static class Styles
        {
            // Groups
            public static GUIContent renderingSettingsText = EditorGUIUtility.TrTextContent("Rendering", "Settings that control the core part of the pipeline rendered frame.");
            public static GUIContent qualitySettingsText = EditorGUIUtility.TrTextContent("Quality", "Settings that control the quality level of the Render pipeline, improving performance and graphics quality.");
            public static GUIContent lightingSettingsText = EditorGUIUtility.TrTextContent("Lighting", "Settings that affect the lighting in the Scene");
            public static GUIContent shadowSettingsText = EditorGUIUtility.TrTextContent("Shadows", "Settings that configure how shadows look and behave, and can be used to balance between the visual quality and performance of shadows.");
            public static GUIContent postProcessingSettingsText = EditorGUIUtility.TrTextContent("Post-processing", "Settings that allow for fine tuning of post-processing effects in the Scene when this Render Pipeline Asset is in use.");
            public static GUIContent volumeSettingsText = EditorGUIUtility.TrTextContent("Volumes", "Settings related to usage of Volume Components.");
            public static GUIContent advancedSettingsText = EditorGUIUtility.TrTextContent("Advanced");
            public static GUIContent adaptivePerformanceText = EditorGUIUtility.TrTextContent("Adaptive Performance");

            // Rendering
            public static GUIContent rendererHeaderText = EditorGUIUtility.TrTextContent("Renderer List", "Lists all the renderers available to this Render Pipeline Asset.");
            public static GUIContent rendererDefaultText = EditorGUIUtility.TrTextContent("Default", "This renderer is currently the default for the render pipeline.");
            public static GUIContent rendererSetDefaultText = EditorGUIUtility.TrTextContent("Set Default", "Makes this renderer the default for the render pipeline.");
            public static GUIContent rendererSettingsText = EditorGUIUtility.TrIconContent("_Menu", "Opens settings for this renderer.");
            public static GUIContent rendererMissingText = EditorGUIUtility.TrIconContent("console.warnicon.sml", "Renderer missing. Click this to select a new renderer.");
            public static GUIContent rendererDefaultMissingText = EditorGUIUtility.TrIconContent("console.erroricon.sml", "Default renderer missing. Click this to select a new renderer.");
            public static GUIContent requireDepthTextureText = EditorGUIUtility.TrTextContent("Depth Texture", "If enabled the pipeline will generate camera's depth that can be bound in shaders as _CameraDepthTexture.");
            public static GUIContent requireOpaqueTextureText = EditorGUIUtility.TrTextContent("Opaque Texture", "If enabled the pipeline will copy the screen to texture after opaque objects are drawn. For transparent objects this can be bound in shaders as _CameraOpaqueTexture.");
            public static GUIContent opaqueDownsamplingText = EditorGUIUtility.TrTextContent("Opaque Downsampling", "The downsampling method that is used for the opaque texture");
            public static GUIContent supportsTerrainHolesText = EditorGUIUtility.TrTextContent("Terrain Holes", "When disabled, Universal Rendering Pipeline removes all Terrain hole Shader variants when you build for the Unity Player. This decreases build time.");
            public static GUIContent srpBatcher = EditorGUIUtility.TrTextContent("SRP Batcher", "If enabled, the render pipeline uses the SRP batcher.");
            public static GUIContent storeActionsOptimizationText = EditorGUIUtility.TrTextContent("Store Actions", "Sets the store actions policy on tile based GPUs. Affects render targets memory usage and will impact performance.");
            public static GUIContent dynamicBatching = EditorGUIUtility.TrTextContent("Dynamic Batching", "If enabled, the render pipeline will batch drawcalls with few triangles together by copying their vertex buffers into a shared buffer on a per-frame basis.");
            public static GUIContent debugLevel = EditorGUIUtility.TrTextContent("Debug Level", "Controls the level of debug information generated by the render pipeline. When Profiling is selected, the pipeline provides detailed profiling tags.");

            // Quality
            public static GUIContent hdrText = EditorGUIUtility.TrTextContent("HDR", "Controls the global HDR settings.");
            public static GUIContent hdrColorBufferPrecisionText = EditorGUIUtility.TrTextContent("HDR Precision", "Controls the precision of the camera color buffer in HDR rendering. 32-bits is the default. 64-bits can reduce banding artifacts at the cost of memory and performance.");
            public static GUIContent msaaText = EditorGUIUtility.TrTextContent("Anti Aliasing (MSAA)", "Controls the global anti aliasing settings.");
            public static GUIContent renderScaleText = EditorGUIUtility.TrTextContent("Render Scale", "Scales the camera render target allowing the game to render at a resolution different than native resolution. UI is always rendered at native resolution.");
            public static GUIContent upscalingFilterText = EditorGUIUtility.TrTextContent("Upscaling Filter", "Controls the type of filter used for upscaling when render scale is lower than 1.0.");
            public static GUIContent fsrOverrideSharpness = EditorGUIUtility.TrTextContent("Override FSR Sharpness", "Overrides the FSR sharpness value for the render pipeline asset.");
            public static GUIContent fsrSharpnessText = EditorGUIUtility.TrTextContent("FSR Sharpness", "Controls the intensity of the sharpening filter used by FidelityFX Super Resolution.");
            public static GUIContent enableLODCrossFadeText = EditorGUIUtility.TrTextContent("LOD Cross Fade", "Controls whether LOD Cross Fade enabled or disabled.");
            public static GUIContent lodCrossFadeDitheringTypeText = EditorGUIUtility.TrTextContent("LOD Cross Fade Dithering Type", "Controls the LOD Cross Fade Dithering Type that will be used to draw Renderer LOD when LODGroup has CrossFade Fade Mode selected.");
            public static GUIContent shEvalModeText = EditorGUIUtility.TrTextContent("SH Evaluation Mode", "Defines the Spherical Harmonic (SH) lighting evaluation type (per vertex, per pixel, or mixed).");
            public static readonly string stpRequiresRenderGraph = "STP is selected but Render Graph is not enabled. STP requires Render Graph in order to function. Unity will fall back to the Automatic option.";
            public static readonly string stpMobilePlatformWarning = "STP is selected for use on a mobile platform. STP is only supported on modern compute-capable hardware and its performance overhead may make it impractical on lower-end devices.";

            // Main light
            public static GUIContent mainLightRenderingModeText = EditorGUIUtility.TrTextContent("Main Light", "Main light is the brightest directional light.");
            public static GUIContent supportsMainLightShadowsText = EditorGUIUtility.TrTextContent("Cast Shadows", "If enabled, the main light can be a shadow casting light.");
            public static GUIContent mainLightShadowmapResolutionText = EditorGUIUtility.TrTextContent("Shadow Resolution", "Resolution of the main light shadowmap texture. If cascades are enabled, cascades will be packed into an atlas and this setting controls the maximum shadows atlas resolution.");

            // Probe volumes
            public static readonly GUIContent lightProbeSystemContent = EditorGUIUtility.TrTextContent("Light Probe System", "What system to use for Light Probes.");
            public static readonly GUIContent probeVolumeMemoryBudget = EditorGUIUtility.TrTextContent("Memory Budget", "Determines the width and height of the 3D textures used to store lighting data from probes. Depth is fixed.");
            public static readonly GUIContent probeVolumeBlendingMemoryBudget = EditorGUIUtility.TrTextContent("Blending Memory Budget", "Determines the width and height of the 3D textures used to store light scenario blending data from probes. Depth is fixed.");
            public static readonly GUIContent supportProbeVolumeGPUStreaming = EditorGUIUtility.TrTextContent("Enable GPU Streaming", "Enable steaming of Cells for Adaptive Probe Volumes.");
            public static readonly GUIContent supportProbeVolumeDiskStreaming = EditorGUIUtility.TrTextContent("Enable Disk Streaming", "Enable steaming of Cells from disk for Adaptive Probe Volumes.");
            public static readonly GUIContent supportProbeVolumeScenarios = EditorGUIUtility.TrTextContent("Enable Lighting Scenarios", "Enable Lighting Scenario Baking for Adaptive Probe Volumes.");
            public static readonly GUIContent supportProbeVolumeScenarioBlending = EditorGUIUtility.TrTextContent("Enable Lighting Scenario Blending", "Enable Lighting Scenario Blending for Adaptive Probe Volumes.\nNote: Lighting Scenario Blending requires Compute Shader support.");
            public static readonly GUIContent probeVolumeSHBands = EditorGUIUtility.TrTextContent("SH Bands", "The number of Spherical Harmonic bands used by Adaptive Probe Volumes to store lighting data. Choosing L2 provides better quality but with higher memory and runtime costs.");

            // Additional lights
            public static GUIContent addditionalLightsRenderingModeText = EditorGUIUtility.TrTextContent("Additional Lights", "Additional lights support.");
            public static GUIContent perObjectLimit = EditorGUIUtility.TrTextContent("Per Object Limit", "Maximum amount of additional lights. These lights are sorted and culled per-object.");
            public static GUIContent supportsAdditionalShadowsText = EditorGUIUtility.TrTextContent("Cast Shadows", "If enabled, shadows will be supported for spot and point lights.");
            public static GUIContent additionalLightsShadowmapResolution = EditorGUIUtility.TrTextContent("Shadow Atlas Resolution", "All additional lights are packed into a single shadowmap atlas. This setting controls the atlas size.");
            public static GUIContent additionalLightsShadowResolutionTiers = EditorGUIUtility.TrTextContent("Shadow Resolution Tiers", $"Additional Lights Shadow Resolution Tiers. Rounded to the next power of two, and clamped to be at least {UniversalAdditionalLightData.AdditionalLightsShadowMinimumResolution}.");
            public static GUIContent[] additionalLightsShadowResolutionTierNames =
            {
                new GUIContent("Low"),
                new GUIContent("Medium"),
                new GUIContent("High")
            };
            public static GUIContent additionalLightsCookieResolution = EditorGUIUtility.TrTextContent("Cookie Atlas Resolution", "All additional lights are packed into a single cookie atlas. This setting controls the atlas size.");
            public static GUIContent additionalLightsCookieFormat = EditorGUIUtility.TrTextContent("Cookie Atlas Format", "All additional lights are packed into a single cookie atlas. This setting controls the atlas format.");

            // Reflection Probes
            public static GUIContent reflectionProbesSettingsText = EditorGUIUtility.TrTextContent("Reflection Probes");
            public static GUIContent reflectionProbeBlendingText = EditorGUIUtility.TrTextContent("Probe Blending", "If enabled smooth transitions will be created between reflection probes.");
            public static GUIContent reflectionProbeBoxProjectionText = EditorGUIUtility.TrTextContent("Box Projection", "If enabled reflections appear based on the object’s position within the probe’s box, while still using a single probe as the source of the reflection.");

            // Additional lighting settings
            public static GUIContent mixedLightingSupportLabel = EditorGUIUtility.TrTextContent("Mixed Lighting", "Makes the render pipeline include mixed-lighting Shader Variants in the build.");
            public static GUIContent useRenderingLayers = EditorGUIUtility.TrTextContent("Use Rendering Layers", "When enabled, rendering layers are used to select which lights an object is affected by. When using the Deferred rendering path, this option allocates an extra render target.");
            public static GUIContent supportsLightCookies = EditorGUIUtility.TrTextContent("Light Cookies", "Makes the render pipeline include light cookies Shader Variants in the build.");

            // Shadow settings
            public static GUIContent shadowWorkingUnitText = EditorGUIUtility.TrTextContent("Working Unit", "The unit in which Unity measures the shadow cascade distances. The exception is Max Distance, which will still be in meters.");
            public static GUIContent shadowDistanceText = EditorGUIUtility.TrTextContent("Max Distance", "Maximum shadow rendering distance.");
            public static GUIContent shadowCascadesText = EditorGUIUtility.TrTextContent("Cascade Count", "Number of cascade splits used for directional shadows.");
            public static GUIContent shadowDepthBias = EditorGUIUtility.TrTextContent("Depth Bias", "Controls the distance at which the shadows will be pushed away from the light. Useful for avoiding false self-shadowing artifacts.");
            public static GUIContent shadowNormalBias = EditorGUIUtility.TrTextContent("Normal Bias", "Controls distance at which the shadow casting surfaces will be shrunk along the surface normal. Useful for avoiding false self-shadowing artifacts.");
            public static GUIContent supportsSoftShadows = EditorGUIUtility.TrTextContent("Soft Shadows", "If enabled pipeline will perform shadow filtering. Otherwise all lights that cast shadows will fallback to perform a single shadow sample.");
            public static GUIContent conservativeEnclosingSphere = EditorGUIUtility.TrTextContent("Conservative Enclosing Sphere", "Enable this option to improve shadow frustum culling and prevent Unity from excessively culling shadows in the corners of the shadow cascades. Disable this option only for compatibility purposes of existing projects created in previous Unity versions.");

            public static GUIContent softShadowsQuality = EditorGUIUtility.TrTextContent("Quality", "Default shadow quality setting for Lights.");
            public static GUIContent[] softShadowsQualityAssetOptions =
            {
                EditorGUIUtility.TrTextContent(nameof(SoftShadowQuality.Low)),
                EditorGUIUtility.TrTextContent(nameof(SoftShadowQuality.Medium)),
                EditorGUIUtility.TrTextContent(nameof(SoftShadowQuality.High))
            };
            public static int[] softShadowsQualityAssetValues =  { (int)SoftShadowQuality.Low, (int)SoftShadowQuality.Medium, (int)SoftShadowQuality.High };

            // Post-processing
            public static GUIContent colorGradingMode = EditorGUIUtility.TrTextContent("Grading Mode", "Defines how color grading will be applied. Operators will react differently depending on the mode.");
            public static GUIContent colorGradingLutSize = EditorGUIUtility.TrTextContent("LUT size", "Sets the size of the internal and external color grading lookup textures (LUTs).");
            public static GUIContent allowPostProcessAlphaOutput = EditorGUIUtility.TrTextContent("Alpha Processing", "When enabled, post-processing outputs alpha channel if available. Alpha 0 preserves the original color. Otherwise post-processing applies to the alpha channel as well. Results may vary depending on the effect.");
            public static GUIContent useFastSRGBLinearConversion = EditorGUIUtility.TrTextContent("Fast sRGB/Linear conversions", "Use faster, but less accurate approximation functions when converting between the sRGB and Linear color spaces.");
            public static GUIContent supportDataDrivenLensFlare = EditorGUIUtility.TrTextContent("Data Driven Lens Flare", "When enabled, URP allocates shader variants and memory for Data Driven Lens Flare effect.");
            public static GUIContent supportScreenSpaceLensFlare = EditorGUIUtility.TrTextContent("Screen Space Lens Flare", "When enabled, URP allocates shader variants and memory for Screen Space Lens Flare effect.");
            public static string alphaOutputWarning = "Camera back-buffer format does not support alpha channel. Final output will be opaque.";
            public static string colorGradingModeWarning = "HDR rendering is required to use the high dynamic range color grading mode. The low dynamic range will be used instead.";
            public static string colorGradingModeWithHDROutput = "With the current configuration, Unity uses the HDR color grading mode when outputting to an HDR display.";
            public static string colorGradingModeSpecInfo = "The high dynamic range color grading mode works best on platforms that support floating point textures.";
            public static string colorGradingLutSizeWarning = "The minimal recommended LUT size for the high dynamic range color grading mode is 32. Using lower values will potentially result in color banding and posterization effects.";

            // Volumes
            public static GUIContent volumeFrameworkUpdateMode = EditorGUIUtility.TrTextContent("Volume Update Mode", "Select how Unity updates Volumes: every frame or when triggered via scripting. In the Editor, Unity updates Volumes every frame when not in the Play mode.");
            public static GUIContent volumeProfileLabel = EditorGUIUtility.TrTextContent("Volume Profile", "Settings that will override the values defined in the Default Volume Profile set in the Render Pipeline Global settings. Local Volumes inside scenes may override these settings further.");
            public static System.Lazy<GUIStyle> volumeProfileContextMenuStyle = new(() => new GUIStyle(CoreEditorStyles.contextMenuStyle) { margin = new RectOffset(0, 1, 3, 0) });

            // GPU Resident Drawer
            public static GUIContent gpuResidentDrawerMode = EditorGUIUtility.TrTextContent("GPU Resident Drawer", "Enables draw submission through the GPU Resident Drawer, which can improve CPU performance");
            public static GUIContent smallMeshScreenPercentage = EditorGUIUtility.TrTextContent("Small-Mesh Screen-Percentage", "Default minimum screen percentage (0-20%) gpu-driven Renderers can cover before getting culled. If a Renderer is part of a LODGroup, this will be ignored.");
            public static GUIContent gpuResidentDrawerEnableOcclusionCullingInCameras = EditorGUIUtility.TrTextContent("GPU Occlusion Culling", "Enables GPU occlusion culling in Game and SceneView cameras.");

            // Adaptive performance settings
            public static GUIContent useAdaptivePerformance = EditorGUIUtility.TrTextContent("Use adaptive performance", "Allows Adaptive Performance to adjust rendering quality during runtime");

            // Renderer List Messages
            public static GUIContent rendererListDefaultMessage =
                EditorGUIUtility.TrTextContent("Cannot remove Default Renderer",
                    "Removal of the Default Renderer is not allowed. To remove, set another Renderer to be the new Default and then remove.");

            public static GUIContent rendererMissingDefaultMessage =
                EditorGUIUtility.TrTextContent("Missing Default Renderer\nThere is no default renderer assigned, so Unity can’t perform any rendering. Set another renderer to be the new Default, or assign a renderer to the Default slot.");
            public static GUIContent rendererMissingMessage =
                EditorGUIUtility.TrTextContent("Missing Renderer(s)\nOne or more renderers are either missing or unassigned.  Switching to these renderers at runtime can cause issues.");
            public static GUIContent lightlayersUnsupportedMessage =
                EditorGUIUtility.TrTextContent("Some Graphics API(s) in the Player Graphics APIs list are incompatible with Light Layers.  Switching to these Graphics APIs at runtime can cause issues: ");
            public static GUIContent rendererUnsupportedAPIMessage =
                EditorGUIUtility.TrTextContent("Some Renderer(s) in the Renderer List are incompatible with the Player Graphics APIs list.  Switching to these renderers at runtime can cause issues.\n\n");
            public static GUIContent brgShaderStrippingErrorMessage =
                EditorGUIUtility.TrTextContent("\"BatchRendererGroup Variants\" setting must be \"Keep All\". To fix, modify Graphics settings and set \"BatchRendererGroup Variants\" to \"Keep All\".");
            public static GUIContent staticBatchingInfoMessage =
                EditorGUIUtility.TrTextContent("Static Batching is not recommended when using GPU draw submission modes, performance may improve if Static Batching is disabled in Player Settings.");
            public static GUIContent lightModeErrorMessage =
                EditorGUIUtility.TrTextContent("Rendering Path must be set to Forward+ for correct lighting and reflections. One or more entries in the RendererList are not set to this mode.");
            public static GUIContent renderGraphNotEnabledErrorMessage =
                EditorGUIUtility.TrTextContent("Render Graph must be enabled to use occlusion culling.");

            // Dropdown menu options
            public static string[] mainLightOptions = { "Disabled", "Per Pixel" };
            public static string[] volumeFrameworkUpdateOptions = { "Every Frame", "Via Scripting" };
            public static string[] opaqueDownsamplingOptions = { "None", "2x (Bilinear)", "4x (Box)", "4x (Bilinear)" };
        }
    }
}
