using UnityEditorInternal;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    internal class SerializedUniversalRenderPipelineAsset
    {
        public SerializedProperty rendererDataProp { get; }
        public SerializedProperty defaultRendererProp { get; }

        public SerializedProperty requireDepthTextureProp { get; }
        public SerializedProperty requireOpaqueTextureProp { get; }
        public SerializedProperty opaqueDownsamplingProp { get; }
        public SerializedProperty supportsTerrainHolesProp { get; }
        public SerializedProperty enableLODCrossFadeProp { get; }
        public SerializedProperty lodCrossFadeDitheringTypeProp { get; }
        public SerializedProperty storeActionsOptimizationProperty { get; }

        public SerializedProperty hdr { get; }
        public SerializedProperty hdrColorBufferPrecisionProp { get; }
        public SerializedProperty msaa { get; }
        public SerializedProperty renderScale { get; }
        public SerializedProperty upscalingFilter { get; }
        public SerializedProperty fsrOverrideSharpness { get; }
        public SerializedProperty fsrSharpness { get; }

        public SerializedProperty mainLightRenderingModeProp { get; }
        public SerializedProperty mainLightShadowsSupportedProp { get; }
        public SerializedProperty mainLightShadowmapResolutionProp { get; }

        public SerializedProperty shEvalModeProp { get; }

        internal SerializedProperty lightProbeSystem;
        internal SerializedProperty probeVolumeTextureSize;
        internal SerializedProperty probeVolumeBlendingTextureSize;
        internal SerializedProperty supportProbeVolumeGPUStreaming;
        internal SerializedProperty supportProbeVolumeDiskStreaming;
        internal SerializedProperty supportProbeVolumeScenarios;
        internal SerializedProperty supportProbeVolumeScenarioBlending;
        internal SerializedProperty probeVolumeSHBands;

        public SerializedProperty additionalLightsRenderingModeProp { get; }
        public SerializedProperty additionalLightsPerObjectLimitProp { get; }
        public SerializedProperty additionalLightShadowsSupportedProp { get; }
        public SerializedProperty additionalLightShadowmapResolutionProp { get; }

        public SerializedProperty additionalLightsShadowResolutionTierLowProp { get; }
        public SerializedProperty additionalLightsShadowResolutionTierMediumProp { get; }
        public SerializedProperty additionalLightsShadowResolutionTierHighProp { get; }

        public SerializedProperty additionalLightCookieResolutionProp { get; }
        public SerializedProperty additionalLightCookieFormatProp { get; }

        public SerializedProperty reflectionProbeBlendingProp { get; }
        public SerializedProperty reflectionProbeBoxProjectionProp { get; }

        public SerializedProperty shadowDistanceProp { get; }
        public SerializedProperty shadowCascadeCountProp { get; }
        public SerializedProperty shadowCascade2SplitProp { get; }
        public SerializedProperty shadowCascade3SplitProp { get; }
        public SerializedProperty shadowCascade4SplitProp { get; }
        public SerializedProperty shadowCascadeBorderProp { get; }
        public SerializedProperty shadowDepthBiasProp { get; }
        public SerializedProperty shadowNormalBiasProp { get; }
        public SerializedProperty softShadowsSupportedProp { get; }
        public SerializedProperty softShadowQualityProp { get; }
        public SerializedProperty conservativeEnclosingSphereProp { get; }

        public SerializedProperty srpBatcher { get; }
        public SerializedProperty supportsDynamicBatching { get; }
        public SerializedProperty mixedLightingSupportedProp { get; }
        public SerializedProperty useRenderingLayers { get; }
        public SerializedProperty supportsLightCookies { get; }
        public SerializedProperty debugLevelProp { get; }

        public SerializedProperty volumeFrameworkUpdateModeProp { get; }
        public SerializedProperty volumeProfileProp { get; }

        public SerializedProperty colorGradingMode { get; }
        public SerializedProperty colorGradingLutSize { get; }
        public SerializedProperty allowPostProcessAlphaOutput { get; }
        public SerializedProperty useFastSRGBLinearConversion { get; }
        public SerializedProperty supportDataDrivenLensFlare { get; }
        public SerializedProperty supportScreenSpaceLensFlare { get; }

        public SerializedProperty gpuResidentDrawerMode { get; }
        public SerializedProperty smallMeshScreenPercentage { get; }
        public SerializedProperty gpuResidentDrawerEnableOcclusionCullingInCameras { get; }

#if ADAPTIVE_PERFORMANCE_2_0_0_OR_NEWER
        public SerializedProperty useAdaptivePerformance { get; }
#endif
        public UniversalRenderPipelineAsset asset { get; }
        public SerializedObject serializedObject { get; }

        public EditorPrefBoolFlags<EditorUtils.Unit> state;

        public SerializedUniversalRenderPipelineAsset(SerializedObject serializedObject)
        {
            asset = serializedObject.targetObject as UniversalRenderPipelineAsset;
            this.serializedObject = serializedObject;

            requireDepthTextureProp = serializedObject.FindProperty("m_RequireDepthTexture");
            requireOpaqueTextureProp = serializedObject.FindProperty("m_RequireOpaqueTexture");
            opaqueDownsamplingProp = serializedObject.FindProperty("m_OpaqueDownsampling");
            supportsTerrainHolesProp = serializedObject.FindProperty("m_SupportsTerrainHoles");
            enableLODCrossFadeProp = serializedObject.FindProperty("m_EnableLODCrossFade");
            lodCrossFadeDitheringTypeProp = serializedObject.FindProperty("m_LODCrossFadeDitheringType");

            hdr = serializedObject.FindProperty("m_SupportsHDR");
            hdrColorBufferPrecisionProp = serializedObject.FindProperty("m_HDRColorBufferPrecision");
            msaa = serializedObject.FindProperty("m_MSAA");
            renderScale = serializedObject.FindProperty("m_RenderScale");
            upscalingFilter = serializedObject.FindProperty("m_UpscalingFilter");
            fsrOverrideSharpness = serializedObject.FindProperty("m_FsrOverrideSharpness");
            fsrSharpness = serializedObject.FindProperty("m_FsrSharpness");

            shEvalModeProp = serializedObject.FindProperty("m_ShEvalMode");

            lightProbeSystem = serializedObject.FindProperty("m_LightProbeSystem");
            probeVolumeTextureSize = serializedObject.FindProperty("m_ProbeVolumeMemoryBudget");
            probeVolumeBlendingTextureSize = serializedObject.FindProperty("m_ProbeVolumeBlendingMemoryBudget");
            supportProbeVolumeGPUStreaming = serializedObject.FindProperty("m_SupportProbeVolumeGPUStreaming");
            supportProbeVolumeDiskStreaming = serializedObject.FindProperty("m_SupportProbeVolumeDiskStreaming");
            supportProbeVolumeScenarios = serializedObject.FindProperty("m_SupportProbeVolumeScenarios");
            supportProbeVolumeScenarioBlending = serializedObject.FindProperty("m_SupportProbeVolumeScenarioBlending");
            probeVolumeSHBands = serializedObject.FindProperty("m_ProbeVolumeSHBands");

            mainLightRenderingModeProp = serializedObject.FindProperty("m_MainLightRenderingMode");
            mainLightShadowsSupportedProp = serializedObject.FindProperty("m_MainLightShadowsSupported");
            mainLightShadowmapResolutionProp = serializedObject.FindProperty("m_MainLightShadowmapResolution");

            additionalLightsRenderingModeProp = serializedObject.FindProperty("m_AdditionalLightsRenderingMode");
            additionalLightsPerObjectLimitProp = serializedObject.FindProperty("m_AdditionalLightsPerObjectLimit");
            additionalLightShadowsSupportedProp = serializedObject.FindProperty("m_AdditionalLightShadowsSupported");
            additionalLightShadowmapResolutionProp = serializedObject.FindProperty("m_AdditionalLightsShadowmapResolution");

            additionalLightsShadowResolutionTierLowProp = serializedObject.FindProperty("m_AdditionalLightsShadowResolutionTierLow");
            additionalLightsShadowResolutionTierMediumProp = serializedObject.FindProperty("m_AdditionalLightsShadowResolutionTierMedium");
            additionalLightsShadowResolutionTierHighProp = serializedObject.FindProperty("m_AdditionalLightsShadowResolutionTierHigh");

            additionalLightCookieResolutionProp = serializedObject.FindProperty("m_AdditionalLightsCookieResolution");
            additionalLightCookieFormatProp = serializedObject.FindProperty("m_AdditionalLightsCookieFormat");

            reflectionProbeBlendingProp = serializedObject.FindProperty("m_ReflectionProbeBlending");
            reflectionProbeBoxProjectionProp = serializedObject.FindProperty("m_ReflectionProbeBoxProjection");

            shadowDistanceProp = serializedObject.FindProperty("m_ShadowDistance");

            shadowCascadeCountProp = serializedObject.FindProperty("m_ShadowCascadeCount");
            shadowCascade2SplitProp = serializedObject.FindProperty("m_Cascade2Split");
            shadowCascade3SplitProp = serializedObject.FindProperty("m_Cascade3Split");
            shadowCascade4SplitProp = serializedObject.FindProperty("m_Cascade4Split");
            shadowCascadeBorderProp = serializedObject.FindProperty("m_CascadeBorder");
            shadowDepthBiasProp = serializedObject.FindProperty("m_ShadowDepthBias");
            shadowNormalBiasProp = serializedObject.FindProperty("m_ShadowNormalBias");
            softShadowsSupportedProp = serializedObject.FindProperty("m_SoftShadowsSupported");
            softShadowQualityProp = serializedObject.FindProperty("m_SoftShadowQuality");
            conservativeEnclosingSphereProp = serializedObject.FindProperty("m_ConservativeEnclosingSphere");

            srpBatcher = serializedObject.FindProperty("m_UseSRPBatcher");
            supportsDynamicBatching = serializedObject.FindProperty("m_SupportsDynamicBatching");
            mixedLightingSupportedProp = serializedObject.FindProperty("m_MixedLightingSupported");
            useRenderingLayers = serializedObject.FindProperty("m_SupportsLightLayers");
            supportsLightCookies = serializedObject.FindProperty("m_SupportsLightCookies");
            debugLevelProp = serializedObject.FindProperty("m_DebugLevel");

            volumeFrameworkUpdateModeProp = serializedObject.FindProperty("m_VolumeFrameworkUpdateMode");
            volumeProfileProp = serializedObject.FindProperty("m_VolumeProfile");

            storeActionsOptimizationProperty = serializedObject.FindProperty("m_StoreActionsOptimization");

            colorGradingMode = serializedObject.FindProperty("m_ColorGradingMode");
            colorGradingLutSize = serializedObject.FindProperty("m_ColorGradingLutSize");

            allowPostProcessAlphaOutput = serializedObject.FindProperty("m_AllowPostProcessAlphaOutput");
            useFastSRGBLinearConversion = serializedObject.FindProperty("m_UseFastSRGBLinearConversion");
            supportDataDrivenLensFlare = serializedObject.FindProperty("m_SupportDataDrivenLensFlare");
            supportScreenSpaceLensFlare = serializedObject.FindProperty("m_SupportScreenSpaceLensFlare");

            gpuResidentDrawerMode = serializedObject.FindProperty("m_GPUResidentDrawerMode");
            smallMeshScreenPercentage = serializedObject.FindProperty("m_SmallMeshScreenPercentage");
            gpuResidentDrawerEnableOcclusionCullingInCameras = serializedObject.FindProperty("m_GPUResidentDrawerEnableOcclusionCullingInCameras");

#if ADAPTIVE_PERFORMANCE_2_0_0_OR_NEWER
            useAdaptivePerformance = serializedObject.FindProperty("m_UseAdaptivePerformance");
#endif
            string Key = "Universal_Shadow_Setting_Unit:UI_State";
            state = new EditorPrefBoolFlags<EditorUtils.Unit>(Key);
        }

        /// <summary>
        /// Refreshes the serialized object
        /// </summary>
        public void Update()
        {
            serializedObject.Update();
        }

        /// <summary>
        /// Applies the modified properties of the serialized object
        /// </summary>
        public void Apply()
        {
            serializedObject.ApplyModifiedProperties();
        }
    }
}
