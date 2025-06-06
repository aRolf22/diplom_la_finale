#ifndef SHADERPASS
#error SHADERPASS must be defined
#endif

#ifndef UNIVERSAL_SHADERPASS_INCLUDED
#error ShaderPass has to be included
#endif


#if (SHADERPASS == SHADERPASS_FORWARD)

float4 VFXCalcPixelOutputForward(const SurfaceData surfaceData, const InputData inputData)
{
    #if VFX_MATERIAL_TYPE_SIX_WAY_SMOKE
    float4 color = UniversalFragmentSixWay(inputData, surfaceData);
    #else
    float4 color = UniversalFragmentPBR(inputData, surfaceData);
    #endif
    color.rgb = MixFog(color.rgb, inputData.fogCoord);

#if IS_OPAQUE_PARTICLE
    bool isTransparent = false;
#else
    bool isTransparent = true;
#endif
    color.a = OutputAlpha(color.a, isTransparent);
    return color;
}

#ifndef VFX_SHADERGRAPH

#if VFX_MATERIAL_TYPE_SIX_WAY_SMOKE
#define SurfaceData SixWaySurfaceData
#endif

float4 VFXGetPixelOutputForward(const VFX_VARYING_PS_INPUTS i, float3 normalWS, const VFXUVData uvData, bool frontFace)
{
    SurfaceData surfaceData;
    InputData inputData;

    VFXGetURPLitData(surfaceData, inputData, i, normalWS, uvData, frontFace, (uint2)0);
    return VFXCalcPixelOutputForward(surfaceData, inputData);
}

#else

float4 VFXGetPixelOutputForwardShaderGraph(const VFX_VARYING_PS_INPUTS i, SurfaceData surfaceData, float3 normalWS)
{
    float3 posRWS = VFXGetPositionRWS(i);
    float4 posSS = i.VFX_VARYING_POSCS;
    PositionInputs posInput = GetPositionInput(posSS.xy, _ScreenSize.zw, posSS.z, posSS.w, posRWS, (uint2)0);

    VFXUVData uvData = (VFXUVData)0;
    InputData inputData = VFXGetInputData(i, posInput, normalWS, true);

    return VFXCalcPixelOutputForward(surfaceData, inputData);
}
#endif

#elif (SHADERPASS == SHADERPASS_GBUFFER)

#ifndef VFX_SHADERGRAPH
void VFXComputePixelOutputToGBuffer(const VFX_VARYING_PS_INPUTS i, const float3 normalWS, const VFXUVData uvData, out FragmentOutput gBuffer)
{
    SurfaceData surfaceData;
    InputData inputData;
    VFXGetURPLitData(surfaceData, inputData, i, normalWS, uvData, true, (uint2)0);

    BRDFData brdfData;
    InitializeBRDFData(surfaceData.albedo, surfaceData.metallic, surfaceData.specular, surfaceData.smoothness, surfaceData.alpha, brdfData);

    half3 color = GlobalIllumination(brdfData, inputData.bakedGI, surfaceData.occlusion, inputData.positionWS, inputData.normalWS, inputData.viewDirectionWS);
    gBuffer = BRDFDataToGbuffer(brdfData, inputData, surfaceData.smoothness, surfaceData.emission + color, surfaceData.occlusion);
}

#else
void VFXComputePixelOutputToGBufferShaderGraph(const VFX_VARYING_PS_INPUTS i, SurfaceData surfaceData, const float3 normalWS, out FragmentOutput gBuffer)
{
    float3 posRWS = VFXGetPositionRWS(i);
    float4 posSS = i.VFX_VARYING_POSCS;
    PositionInputs posInput = GetPositionInput(posSS.xy, _ScreenSize.zw, posSS.z, posSS.w, posRWS, (uint2)0);

    VFXUVData uvData = (VFXUVData)0;
    InputData inputData = VFXGetInputData(i, posInput, normalWS, true);

    BRDFData brdfData;
    InitializeBRDFData(surfaceData.albedo, surfaceData.metallic, surfaceData.specular, surfaceData.smoothness, surfaceData.alpha, brdfData);

    half3 color = GlobalIllumination(brdfData, inputData.bakedGI, surfaceData.occlusion, inputData.positionWS, inputData.normalWS, inputData.viewDirectionWS);
    gBuffer = BRDFDataToGbuffer(brdfData, inputData, surfaceData.smoothness, surfaceData.emission + color, surfaceData.occlusion);
}

#endif
#endif
