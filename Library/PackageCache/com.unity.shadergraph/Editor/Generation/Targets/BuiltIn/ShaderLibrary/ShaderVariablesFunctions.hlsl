#ifndef UNITY_SHADER_VARIABLES_FUNCTIONS_INCLUDED
#define UNITY_SHADER_VARIABLES_FUNCTIONS_INCLUDED

#include "Packages/com.unity.shadergraph/Editor/Generation/Targets/BuiltIn/ShaderLibrary/ShaderVariablesFunctions.deprecated.hlsl"

VertexPositionInputs GetVertexPositionInputs(float3 positionOS)
{
    VertexPositionInputs input;
    input.positionWS = TransformObjectToWorld(positionOS);
    input.positionVS = TransformWorldToView(input.positionWS);
    input.positionCS = TransformWorldToHClip(input.positionWS);

    float4 ndc = input.positionCS * 0.5f;
    input.positionNDC.xy = float2(ndc.x, ndc.y * _ProjectionParams.x) + ndc.w;
    input.positionNDC.zw = input.positionCS.zw;

    return input;
}

VertexNormalInputs GetVertexNormalInputs(float3 normalOS)
{
    VertexNormalInputs tbn;
    tbn.tangentWS = real3(1.0, 0.0, 0.0);
    tbn.bitangentWS = real3(0.0, 1.0, 0.0);
    tbn.normalWS = TransformObjectToWorldNormal(normalOS);
    return tbn;
}

VertexNormalInputs GetVertexNormalInputs(float3 normalOS, float4 tangentOS)
{
    VertexNormalInputs tbn;

    // mikkts space compliant. only normalize when extracting normal at frag.
    real sign = tangentOS.w * GetOddNegativeScale();
    tbn.normalWS = TransformObjectToWorldNormal(normalOS);
    tbn.tangentWS = TransformObjectToWorldDir(tangentOS.xyz);
    tbn.bitangentWS = cross(tbn.normalWS, tbn.tangentWS) * sign;
    return tbn;
}

float4 GetScaledScreenParams()
{
    // NOTE: Builtin does not use _ScaledScreenParams, so we simply return _ScreenParams instead
    return _ScreenParams;
}

// Returns 'true' if the current view performs a perspective projection.
bool IsPerspectiveProjection()
{
    return (unity_OrthoParams.w == 0);
}

float3 GetCameraPositionWS()
{
    // Currently we do not support Camera Relative Rendering so
    // we simply return the _WorldSpaceCameraPos until then
    return _WorldSpaceCameraPos;

    // We will replace the code above with this one once
    // we start supporting Camera Relative Rendering
    //#if (SHADEROPTIONS_CAMERA_RELATIVE_RENDERING != 0)
    //    return float3(0, 0, 0);
    //#else
    //    return _WorldSpaceCameraPos;
    //#endif
}

// Could be e.g. the position of a primary camera or a shadow-casting light.
float3 GetCurrentViewPosition()
{
    // Currently we do not support Camera Relative Rendering so
    // we simply return the _WorldSpaceCameraPos until then
    return GetCameraPositionWS();

    // We will replace the code above with this one once
    // we start supporting Camera Relative Rendering
    //#if defined(SHADERPASS) && (SHADERPASS != SHADERPASS_SHADOWS)
    //    return GetCameraPositionWS();
    //#else
    //    // This is a generic solution.
    //    // However, for the primary camera, using '_WorldSpaceCameraPos' is better for cache locality,
    //    // and in case we enable camera-relative rendering, we can statically set the position is 0.
    //    return UNITY_MATRIX_I_V._14_24_34;
    //#endif
}

// Returns the forward (central) direction of the current view in the world space.
float3 GetViewForwardDir()
{
    float4x4 viewMat = GetWorldToViewMatrix();
    return -viewMat[2].xyz;
}

// Computes the world space view direction (pointing towards the viewer).
float3 GetWorldSpaceViewDir(float3 positionWS)
{
    if (IsPerspectiveProjection())
    {
        // Perspective
        return GetCurrentViewPosition() - positionWS;
    }
    else
    {
        // Orthographic
        return -GetViewForwardDir();
    }
}

float3 GetWorldSpaceNormalizeViewDir(float3 positionWS)
{
    if (IsPerspectiveProjection())
    {
        // Perspective
        float3 V = GetCurrentViewPosition() - positionWS;
        return normalize(V);
    }
    else
    {
        // Orthographic
        return -GetViewForwardDir();
    }
}

// UNITY_MATRIX_V defines a right-handed view space with the Z axis pointing towards the viewer.
// This function reverses the direction of the Z axis (so that it points forward),
// making the view space coordinate system left-handed.
void GetLeftHandedViewSpaceMatrices(out float4x4 viewMatrix, out float4x4 projMatrix)
{
    viewMatrix = UNITY_MATRIX_V;
    viewMatrix._31_32_33_34 = -viewMatrix._31_32_33_34;

    projMatrix = UNITY_MATRIX_P;
    projMatrix._13_23_33_43 = -projMatrix._13_23_33_43;
}

void AlphaDiscard(real alpha, real cutoff, real offset = 0.0h)
{
    #ifdef _ALPHATEST_ON
        clip(alpha - cutoff + offset);
    #endif
}

half OutputAlpha(half outputAlpha, half surfaceType = 0.0)
{
    return surfaceType == 1 ? outputAlpha : 1.0;
}

// A word on normalization of normals:
// For better quality normals should be normalized before and after
// interpolation.
// 1) In vertex, skinning or blend shapes might vary significantly the lenght of normal.
// 2) In fragment, because even outputting unit-length normals interpolation can make it non-unit.
// 3) In fragment when using normal map, because mikktspace sets up non orthonormal basis.
// However we will try to balance performance vs quality here as also let users configure that as
// shader quality tiers.
// Low Quality Tier: Don't normalize per-vertex.
// Medium Quality Tier: Always normalize per-vertex.
// High Quality Tier: Always normalize per-vertex.
//
// Always normalize per-pixel.
// Too many bug like lighting quality issues otherwise.
real3 NormalizeNormalPerVertex(real3 normalWS)
{
    #if defined(SHADER_QUALITY_LOW) && defined(_NORMALMAP)
        return normalWS;
    #else
        return normalize(normalWS);
    #endif
}

real3 NormalizeNormalPerPixel(real3 normalWS)
{
    return normalize(normalWS);
}



real ComputeFogFactor(float z)
{
    float clipZ_01 = UNITY_Z_0_FAR_FROM_CLIPSPACE(z);

    #if defined(FOG_LINEAR)
        // factor = (end-z)/(end-start) = z * (-1/(end-start)) + (end/(end-start))
        float fogFactor = saturate(clipZ_01 * unity_FogParams.z + unity_FogParams.w);
        return real(fogFactor);
    #elif defined(FOG_EXP) || defined(FOG_EXP2)
        // factor = exp(-(density*z)^2)
        // -density * z computed at vertex
        return real(unity_FogParams.x * clipZ_01);
    #else
        return 0.0h;
    #endif
}

real ComputeFogIntensity(real fogFactor)
{
    real fogIntensity = 0.0h;
    #if defined(FOG_LINEAR) || defined(FOG_EXP) || defined(FOG_EXP2)
        #if defined(FOG_EXP)
            // factor = exp(-density*z)
            // fogFactor = density*z compute at vertex
            fogIntensity = saturate(exp2(-fogFactor));
        #elif defined(FOG_EXP2)
            // factor = exp(-(density*z)^2)
            // fogFactor = density*z compute at vertex
            fogIntensity = saturate(exp2(-fogFactor * fogFactor));
        #elif defined(FOG_LINEAR)
            fogIntensity = fogFactor;
        #endif
    #endif
    return fogIntensity;
}

half3 MixFogColor(real3 fragColor, real3 fogColor, real fogFactor)
{
    #if defined(FOG_LINEAR) || defined(FOG_EXP) || defined(FOG_EXP2)
        real fogIntensity = ComputeFogIntensity(fogFactor);
        fragColor = lerp(fogColor, fragColor, fogIntensity);
    #endif
    return fragColor;
}

half3 MixFog(real3 fragColor, real fogFactor)
{
    return MixFogColor(fragColor, unity_FogColor.rgb, fogFactor);
}

// Linear depth buffer value between [0, 1] or [1, 0] to eye depth value between [near, far]
real LinearDepthToEyeDepth(real rawDepth)
{
    #if UNITY_REVERSED_Z
    return _ProjectionParams.z - (_ProjectionParams.z - _ProjectionParams.y) * rawDepth;
    #else
    return _ProjectionParams.y + (_ProjectionParams.z - _ProjectionParams.y) * rawDepth;
    #endif
}

void TransformScreenUV(inout float2 uv, float screenHeight)
{
    #if UNITY_UV_STARTS_AT_TOP
    uv.y = screenHeight - (uv.y * _ScaleBiasRt.x + _ScaleBiasRt.y * screenHeight);
    #endif
}

void TransformScreenUV(inout float2 uv)
{
    #if UNITY_UV_STARTS_AT_TOP
    TransformScreenUV(uv, GetScaledScreenParams().y);
    #endif
}

void TransformNormalizedScreenUV(inout float2 uv)
{
    #if UNITY_UV_STARTS_AT_TOP
    TransformScreenUV(uv, 1.0);
    #endif
}

float2 GetNormalizedScreenSpaceUV(float2 positionCS)
{
    float2 normalizedScreenSpaceUV = positionCS.xy * rcp(GetScaledScreenParams().xy);
    TransformNormalizedScreenUV(normalizedScreenSpaceUV);
    return normalizedScreenSpaceUV;
}

float2 GetNormalizedScreenSpaceUV(float4 positionCS)
{
    return GetNormalizedScreenSpaceUV(positionCS.xy);
}

#if defined(UNITY_SINGLE_PASS_STEREO)
    float2 TransformStereoScreenSpaceTex(float2 uv, float w)
    {
        // TODO: RVS support can be added here, if BuiltIn decides to support it
        float4 scaleOffset = unity_StereoScaleOffset[unity_StereoEyeIndex];
        return uv.xy * scaleOffset.xy + scaleOffset.zw * w;
    }

    float2 UnityStereoTransformScreenSpaceTex(float2 uv)
    {
        return TransformStereoScreenSpaceTex(saturate(uv), 1.0);
    }
#else
    #define UnityStereoTransformScreenSpaceTex(uv) uv
#endif // defined(UNITY_SINGLE_PASS_STEREO)

#endif // UNITY_SHADER_VARIABLES_FUNCTIONS_INCLUDED
