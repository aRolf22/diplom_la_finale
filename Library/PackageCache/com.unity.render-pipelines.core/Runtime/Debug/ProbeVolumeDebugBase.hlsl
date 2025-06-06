#ifndef PROBEVOLUMEDEBUG_BASE_HLSL
#define PROBEVOLUMEDEBUG_BASE_HLSL

// TEMPORARY WORKAROUND
// Unfortunately we don't have a cross pipeline way to pass per frame constant.
// One of the reason is that we don't want to force a certain Constant Buffer layout on users (read: SRP writer) for shared data.
// This means that usually SRP Core functions (see SpaceTransforms.hlsl for example) use common names that are NOT declared in the Core package but rather in each pipelines.
// The consequence is that when writing core shaders that need those variables, we currently have to copy their declaration and set them manually from core C# code to the relevant shaders.

// Here is current the subset of variables and functions required by APV debug.
// Copying them here means that these shaders don't support either XR or Camera Relative rendering (at least until those concept become fully cross pipeline)
CBUFFER_START(ShaderVariablesProbeVolumeDebug)
float4x4 unity_MatrixVP;        // Sent by builtin
float4x4 unity_MatrixInvV;      // Sent by builtin
float4x4 unity_ObjectToWorld;   // Sent by builtin
float4x4 unity_MatrixV;         // Sent by builtin
float4 _ScreenSize;
float3 _WorldSpaceCameraPos;    // Sent by builtin
CBUFFER_END

TEXTURE2D(_ExposureTexture);

#define UNITY_MATRIX_VP unity_MatrixVP
#define UNITY_MATRIX_V unity_MatrixV
#define UNITY_MATRIX_I_V unity_MatrixInvV
#define UNITY_MATRIX_M unity_ObjectToWorld

float3 GetCurrentViewPosition()
{
    return UNITY_MATRIX_I_V._14_24_34;
}

float GetCurrentExposureMultiplier()
{
    return LOAD_TEXTURE2D(_ExposureTexture, int2(0, 0)).x;
}

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureXR.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/Lighting/ProbeVolume/DecodeSH.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/Lighting/ProbeVolume/ProbeVolume.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/Lighting/ProbeVolume/ProbeReferenceVolume.Debug.cs.hlsl"


uniform int _ShadingMode;
uniform float _ExposureCompensation;
uniform float _ProbeSize;
uniform float4 _Color;
uniform int _SubdivLevel;
uniform float _CullDistance;
uniform int _MaxAllowedSubdiv;
uniform int _MinAllowedSubdiv;
uniform float _ValidityThreshold;
uniform uint _RenderingLayerMask;
uniform float _OffsetSize;

// Sampling Position Debug
uniform bool _DebugProbeVolumeSampling = false;
StructuredBuffer<float4> _positionNormalBuffer;
uniform float4 _DebugArrowColor; // arrow color for position and normal debug
uniform float4 _DebugLocator01Color; // locator color for final sampling position debug
uniform float4 _DebugLocator02Color; // locator color for normal and view bias sampling position debug
uniform float4 _DebugEmptyProbeData; // probe color for missing data
uniform bool _ForceDebugNormalViewBias; // additional locator to debug Normal Bias and View Bias without AntiLeak Reduction Mode
uniform bool _DebugSamplingNoise = false;
uniform sampler2D _NumbersTex;

UNITY_INSTANCING_BUFFER_START(Props)
    UNITY_DEFINE_INSTANCED_PROP(float, _Validity)
    UNITY_DEFINE_INSTANCED_PROP(float, _RenderingLayer)
    UNITY_DEFINE_INSTANCED_PROP(float, _DilationThreshold)
    UNITY_DEFINE_INSTANCED_PROP(float, _TouchupedByVolume)
    UNITY_DEFINE_INSTANCED_PROP(float4, _IndexInAtlas)
    UNITY_DEFINE_INSTANCED_PROP(float4, _Offset)
    UNITY_DEFINE_INSTANCED_PROP(float, _RelativeSize)
UNITY_INSTANCING_BUFFER_END(Props)

struct appdata
{
    float4 vertex : POSITION;
    float3 normal : NORMAL;
    float4 color  : COLOR0;
    float2 texCoord : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct v2f
{
    float4 vertex : SV_POSITION;
    float3 normal : TEXCOORD1;
    float4 color  : COLOR0;
    float2 texCoord : TEXCOORD0;
    float2 samplingFactor_ValidityWeight : TEXCOORD2; // stores sampling factor (for Probe Sampling Debug view) and validity weight
    float2 centerCoordSS : TEXCOORD3;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

void DoCull(inout v2f o)
{
    ZERO_INITIALIZE(v2f, o);
    o.samplingFactor_ValidityWeight = float2(0.0f, 1.0f);
}

// snappedProbePosition_WS : worldspace position of main probe (a corner of the 8 probes cube)
// samplingPosition_WS : worldspace sampling position after applying 'NormalBias' and 'ViewBias' and 'ValidityAndNormalBased Leak Reduction'
// normalizedOffset : normalized offset between sampling position and snappedProbePosition
void FindSamplingData(float3 posWS, float3 normalWS, uint renderingLayer, out float3 snappedProbePosition_WS, out float3 samplingPositionNoAntiLeak_WS, out float probeDistance, out float3 normalizedOffset, out float validityWeights[8])
{
    float3 cameraPosition_WS = _WorldSpaceCameraPos;
    float3 viewDir_WS = normalize(cameraPosition_WS - posWS);

    if (_DebugSamplingNoise)
    {
        float2 posNDC = ComputeNormalizedDeviceCoordinates(posWS, UNITY_MATRIX_VP);
        float2 posSS = floor(posNDC.xy * _ScreenSize.xy);
        posWS = AddNoiseToSamplingPosition(posWS, posSS, viewDir_WS);
    }

    posWS -= _APVWorldOffset;

    // uvw
    APVResources apvRes = FillAPVResources();
    float3 uvw;
    uint subdiv;
    float3 biasedPosWS;
    bool valid = TryToGetPoolUVWAndSubdiv(apvRes, posWS, normalWS, viewDir_WS, uvw, subdiv, biasedPosWS);

    // Validity mask
    float3 texCoord = uvw * _APVPoolDim - .5f;
    float3 texFrac = frac(texCoord);
    uint validityMask = LoadValidityMask(apvRes, renderingLayer, texCoord);
    for (uint i = 0; i < 8; i++)
    {
        int3 probeCoord = GetSampleOffset(i);
        float validityWeight = ((probeCoord.x == 1) ? texFrac.x : 1.0f - texFrac.x) *
                               ((probeCoord.y == 1) ? texFrac.y : 1.0f - texFrac.y) *
                               ((probeCoord.z == 1) ? texFrac.z : 1.0f - texFrac.z);
        validityWeights[i] = validityWeight * GetValidityWeight(i, validityMask);
    }

    // Sample position
    normalizedOffset = texFrac;
    if (_APVLeakReductionMode == APVLEAKREDUCTIONMODE_PERFORMANCE)
    {
        float3 warped = uvw;
        WarpUVWLeakReduction(apvRes, renderingLayer, warped);
        normalizedOffset += (warped - uvw) * _APVPoolDim;
    }

    // stuff
    probeDistance = ProbeDistance(subdiv);
    snappedProbePosition_WS = GetSnappedProbePosition(biasedPosWS, subdiv) + _APVWorldOffset;
    samplingPositionNoAntiLeak_WS = biasedPosWS + _APVWorldOffset;
}

// Return probe sampling weight
float ComputeSamplingFactor(float3 probePosition_WS, float3 snappedProbePosition_WS, float3 normalizedOffset, float probeDistance)
{
    float samplingFactor = -1.0f;

    if (distance(snappedProbePosition_WS, probePosition_WS) < 0.0001f)
    {
        samplingFactor = (1.0f-normalizedOffset.x) * (1.0f-normalizedOffset.y) * (1.0f-normalizedOffset.z);
    }

    else if (distance(snappedProbePosition_WS, probePosition_WS + float3(-1.0f, 0.0f, 0.0f)*probeDistance) < 0.0001f)
    {
        samplingFactor = (normalizedOffset.x) * (1.0f-normalizedOffset.y) * (1.0f-normalizedOffset.z);
    }

    else if (distance(snappedProbePosition_WS, probePosition_WS + float3(-1.0f, -1.0f, 0.0f)*probeDistance) < 0.0001f)
    {
        samplingFactor = (normalizedOffset.x) * (normalizedOffset.y) * (1.0f-normalizedOffset.z);
    }

    else if (distance(snappedProbePosition_WS, probePosition_WS + float3(0.0f, -1.0f, 0.0f)*probeDistance) < 0.0001f)
    {
        samplingFactor = (1.0f-normalizedOffset.x) * (normalizedOffset.y) * (1.0f-normalizedOffset.z);
    }

    else if (distance(snappedProbePosition_WS, probePosition_WS + float3(-1.0f, 0.0f, -1.0f)*probeDistance) < 0.0001f)
    {
        samplingFactor = (normalizedOffset.x) * (1.0f-normalizedOffset.y) * (normalizedOffset.z);
    }

    else if (distance(snappedProbePosition_WS, probePosition_WS + float3(0.0f, 0.0f, -1.0f)*probeDistance) < 0.0001f)
    {
        samplingFactor = (1.0f-normalizedOffset.x) * (1.0f-normalizedOffset.y) * (normalizedOffset.z);
    }

    else if (distance(snappedProbePosition_WS, probePosition_WS + float3(-1.0f, -1.0f, -1.0f)*probeDistance) < 0.0001f)
    {
        samplingFactor = (normalizedOffset.x) * (normalizedOffset.y) * (normalizedOffset.z);
    }

    else if (distance(snappedProbePosition_WS, probePosition_WS + float3(0.0f, -1.0f, -1.0f)*probeDistance) < 0.0001f)
    {
        samplingFactor = (1.0f-normalizedOffset.x) * (normalizedOffset.y) * (normalizedOffset.z);
    }

    return samplingFactor;
}

// Sample a texture with numbers at the right place depending on a number input
half4 SampleCharacter(int input, float2 texCoord) // Samples _NumbersTex to get given character
{
    // texture is divided in 16 parts and contains following characters : '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '.'  (+ empty space)
    float2 uv = float2((texCoord.x + input) / 16.0f, texCoord.y);
    half4 color = tex2D(_NumbersTex, uv);
    return color;
}

// Writes a floating number with two decimals using a texture with numbers.
// Use to debug probe sampling weights with text
half4 WriteFractNumber(float input, float2 texCoord)
{
    // using 4 characters
    float i = 4.0f;

    // 0.00X
    int n3_value = floor(frac(input * 100.0f) * 10.0f);
    int n2_add = 0;
    if (n3_value >= 5) {n2_add = 1;}

    // 0.0X
    int n2_value = floor(frac(input * 10.0f) * 10.0f);
    n2_value += n2_add;
    int n1_add = 0;
    if (n2_value >= 10) {n2_value -= 10; n1_add = 1;}

    // 0.X
    int n1_value = floor(frac(input) * 10.0f);
    n1_value += n1_add;
    int n0_add = 0;
    if (n1_value >= 10) {n1_value -= 10; n0_add = 1;}

    // X
    int n0_value = floor(input);
    n0_value += n0_add;

    float2 n0_uv = float2(clamp(texCoord.x*i - 0.0f, 0.0f, 1.0f), texCoord.y);
    float2 dot_uv = float2(clamp(texCoord.x*i - 1.0f, 0.0f, 1.0f), texCoord.y);
    float2 n1_uv = float2(clamp(texCoord.x*i - 2.0f, 0.0f, 1.0f), texCoord.y);
    float2 n2_uv = float2(clamp(texCoord.x*i - 3.0f, 0.0f, 1.0f), texCoord.y);

    half4 outVal = 0;
    if (texCoord.x <= 0.25)
        outVal = SampleCharacter(n0_value, n0_uv);
    else if (texCoord.x <= 0.50)
        outVal = SampleCharacter(10, dot_uv);
    else if (texCoord.x <= 0.75)
        outVal = SampleCharacter(n1_value, n1_uv);
    else
        outVal = SampleCharacter(n2_value, n2_uv);

    return outVal;
}



// Finer culling, degenerate the vertices of the debug element if it lies over the max distance.
// Coarser culling has already happened on CPU.
bool ShouldCull(inout v2f o)
{
    float4 position = float4(UNITY_MATRIX_M._m03_m13_m23, 1);
    int brickSize = UNITY_ACCESS_INSTANCED_PROP(Props, _IndexInAtlas).w;

    bool shouldCull = false;
    if (distance(position.xyz + _APVWorldOffset, GetCurrentViewPosition()) > _CullDistance || brickSize > _MaxAllowedSubdiv || brickSize < _MinAllowedSubdiv)
    {
        ZERO_INITIALIZE(v2f, o);
        DoCull(o);
        shouldCull = true;
    }

    return shouldCull;
}

float3 EvalL1(float3 L0, float3 L1_R, float3 L1_G, float3 L1_B, float3 N)
{
    float3 outLighting = 0;
    L1_R = DecodeSH(L0.r, L1_R);
    L1_G = DecodeSH(L0.g, L1_G);
    L1_B = DecodeSH(L0.b, L1_B);
    outLighting += SHEvalLinearL1(N, L1_R, L1_G, L1_B);

    return outLighting;
}

float3 EvalL2(inout float3 L0, float4 L2_R, float4 L2_G, float4 L2_B, float4 L2_C, float3 N)
{
    DecodeSH_L2(L0, L2_R, L2_G, L2_B, L2_C);

    return SHEvalLinearL2(N, L2_R, L2_G, L2_B, L2_C);
}

float3 CalculateDiffuseLighting(v2f i)
{
    APVResources apvRes = FillAPVResources();
    int3 texLoc = UNITY_ACCESS_INSTANCED_PROP(Props, _IndexInAtlas).xyz;
    float3 normal = normalize(i.normal);

    float3 skyShadingDirection = normal;
    if (_ShadingMode == DEBUGPROBESHADINGMODE_PROBE_OCCLUSION)
    {
        float4 shadowmask = apvRes.ProbeOcclusion[texLoc];
        return shadowmask.rgb * 0.5 + (shadowmask.aaa * 0.5);
    }
    else if (_ShadingMode == DEBUGPROBESHADINGMODE_SKY_DIRECTION)
    {
        if (_APVSkyDirectionWeight > 0)
        {
            float value = 1.0f / GetCurrentExposureMultiplier();

            uint index = apvRes.SkyShadingDirectionIndices[texLoc].r * 255;
            if (index != 255)
                skyShadingDirection = apvRes.SkyPrecomputedDirections[index].rgb;
            else
                return float3(value, 0.0f, 0.0f);

            if (dot(normal, skyShadingDirection) > 0.95)
                return float3(0.0f, value, 0.0f);
            return float3(0.0f, 0.0f, 0.0f);
        }
        else
        {
            return _DebugEmptyProbeData.xyz / GetCurrentExposureMultiplier();
        }
    }
    else
    {
        float3 skyOcclusion = _DebugEmptyProbeData.xyz;
        if (_APVSkyOcclusionWeight > 0)
        {
            // L0 L1
            float4 temp = float4(kSHBasis0, kSHBasis1 * normal.x, kSHBasis1 * normal.y, kSHBasis1 * normal.z);
            skyOcclusion = dot(temp, apvRes.SkyOcclusionL0L1[texLoc].rgba);
        }

        if (_ShadingMode == DEBUGPROBESHADINGMODE_SKY_OCCLUSION_SH)
        {
            return skyOcclusion / GetCurrentExposureMultiplier();
        }
        else
        {
            float4 L0_L1Rx = apvRes.L0_L1Rx[texLoc].rgba;
            float3 L0 = L0_L1Rx.xyz;

            if (_ShadingMode == DEBUGPROBESHADINGMODE_SHL0)
                return L0;

            float  L1Rx = L0_L1Rx.w;
            float4 L1G_L1Ry = apvRes.L1G_L1Ry[texLoc].rgba;
            float4 L1B_L1Rz = apvRes.L1B_L1Rz[texLoc].rgba;

            float3 bakeDiffuseLighting = EvalL1(L0, float3(L1Rx, L1G_L1Ry.w, L1B_L1Rz.w), L1G_L1Ry.xyz, L1B_L1Rz.xyz, normal);
            bakeDiffuseLighting += L0;

            if (_ShadingMode == DEBUGPROBESHADINGMODE_SHL0L1)
                return bakeDiffuseLighting;

    #ifdef PROBE_VOLUMES_L2
            float4 L2_R = apvRes.L2_0[texLoc].rgba;
            float4 L2_G = apvRes.L2_1[texLoc].rgba;
            float4 L2_B = apvRes.L2_2[texLoc].rgba;
            float4 L2_C = apvRes.L2_3[texLoc].rgba;

            bakeDiffuseLighting += EvalL2(L0, L2_R, L2_G, L2_B, L2_C, normal);
    #endif
            if (_APVSkyOcclusionWeight > 0)
                bakeDiffuseLighting += skyOcclusion * EvaluateAmbientProbe(skyShadingDirection);

            return bakeDiffuseLighting;
        }
    }
}

CBUFFER_START(TouchupVolumeBounds)
    float4 _TouchupVolumeBounds[16 * 3]; // A BBox is 3 float4
    uint _AdjustmentVolumeCount;
CBUFFER_END

bool IsInSelection(float3 position)
{
    for (uint i = 0; i < _AdjustmentVolumeCount; i++)
    {
        float3 center = _TouchupVolumeBounds[i * 3 + 0].xyz;
        bool isSphere = _TouchupVolumeBounds[i * 3 + 0].w >= FLT_MAX;
        if (isSphere)
        {
            float radius = _TouchupVolumeBounds[i * 3 + 1].x;
            if (dot(position - center, position - center) < radius * radius)
                return true;
        }
        else
        {
            float3 X = _TouchupVolumeBounds[i * 3 + 1].xyz;
            float3 Y = _TouchupVolumeBounds[i * 3 + 2].xyz;
            float3 Z = float3(_TouchupVolumeBounds[i * 3 + 0].w,
                              _TouchupVolumeBounds[i * 3 + 1].w,
                              _TouchupVolumeBounds[i * 3 + 2].w);

            if (abs(dot(position - center, normalize(X))) < length(X) &&
                abs(dot(position - center, normalize(Y))) < length(Y) &&
                abs(dot(position - center, normalize(Z))) < length(Z))
                return true;
        }
    }

    return false;
}

#endif //PROBEVOLUMEDEBUG_BASE_HLSL
