Shader "Hidden/Universal Render Pipeline/BokehDepthOfField"
{
    HLSLINCLUDE
        #pragma multi_compile_local_fragment _ _USE_FAST_SRGB_LINEAR_CONVERSION

        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
        #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/Shaders/PostProcessing/Common.hlsl"

        // Do not change this without changing PostProcessPass.PrepareBokehKernel()
        #define SAMPLE_COUNT            42

        // Toggle this to reduce flickering - note that it will reduce overall bokeh energy and add
        // a small cost to the pre-filtering pass
        #define COC_LUMA_WEIGHTING      0

        TEXTURE2D_X(_DofTexture);
        TEXTURE2D_X(_FullCoCTexture);

        half4 _SourceSize;
        half4 _DownSampleScaleFactor;
        half4 _CoCParams;
        half4 _BokehKernel[SAMPLE_COUNT];
        half4 _BokehConstants;

        #define FocusDist       _CoCParams.x
        #define MaxCoC          _CoCParams.y
        #define MaxRadius       _CoCParams.z
        #define RcpAspect       _CoCParams.w

        half FragCoC(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

            float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord);
            float depth = LOAD_TEXTURE2D_X(_CameraDepthTexture, _SourceSize.xy * uv).x;
            float linearEyeDepth = LinearEyeDepth(depth, _ZBufferParams);

            half coc = (1.0 - FocusDist / linearEyeDepth) * MaxCoC;
            half nearCoC = clamp(coc, -1.0, 0.0);
            half farCoC = saturate(coc);

            return saturate((farCoC + nearCoC + 1.0) * 0.5);
        }

        half4 FragPrefilter(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord);

        #if SHADER_TARGET >= 45 && defined(PLATFORM_SUPPORT_GATHER)

            // Sample source colors
            half4 cr = GATHER_RED_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
            half4 cg = GATHER_GREEN_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
            half4 cb = GATHER_BLUE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);

            half3 c0 = half3(cr.x, cg.x, cb.x);
            half3 c1 = half3(cr.y, cg.y, cb.y);
            half3 c2 = half3(cr.z, cg.z, cb.z);
            half3 c3 = half3(cr.w, cg.w, cb.w);

            // Sample CoCs
            half4 cocs = GATHER_TEXTURE2D_X(_FullCoCTexture, sampler_LinearClamp, uv) * 2.0 - 1.0;
            half coc0 = cocs.x;
            half coc1 = cocs.y;
            half coc2 = cocs.z;
            half coc3 = cocs.w;

        #else

            float3 duv = _SourceSize.zwz * float3(0.5, 0.5, -0.5);
            float2 uv0 = uv - duv.xy;
            float2 uv1 = uv - duv.zy;
            float2 uv2 = uv + duv.zy;
            float2 uv3 = uv + duv.xy;

            // Sample source colors
            half3 c0 = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv0).xyz;
            half3 c1 = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv1).xyz;
            half3 c2 = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv2).xyz;
            half3 c3 = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv3).xyz;

            // Sample CoCs
            half coc0 = SAMPLE_TEXTURE2D_X(_FullCoCTexture, sampler_LinearClamp, uv0).x * 2.0 - 1.0;
            half coc1 = SAMPLE_TEXTURE2D_X(_FullCoCTexture, sampler_LinearClamp, uv1).x * 2.0 - 1.0;
            half coc2 = SAMPLE_TEXTURE2D_X(_FullCoCTexture, sampler_LinearClamp, uv2).x * 2.0 - 1.0;
            half coc3 = SAMPLE_TEXTURE2D_X(_FullCoCTexture, sampler_LinearClamp, uv3).x * 2.0 - 1.0;

        #endif

        #if COC_LUMA_WEIGHTING

            // Apply CoC and luma weights to reduce bleeding and flickering
            half w0 = abs(coc0) / (Max3(c0.x, c0.y, c0.z) + 1.0);
            half w1 = abs(coc1) / (Max3(c1.x, c1.y, c1.z) + 1.0);
            half w2 = abs(coc2) / (Max3(c2.x, c2.y, c2.z) + 1.0);
            half w3 = abs(coc3) / (Max3(c3.x, c3.y, c3.z) + 1.0);

            // Weighted average of the color samples
            half3 avg = c0 * w0 + c1 * w1 + c2 * w2 + c3 * w3;
            avg /= max(w0 + w1 + w2 + w3, 1e-5);

        #else

            half3 avg = (c0 + c1 + c2 + c3) / 4.0;

        #endif

            // Select the largest CoC value
            half cocMin = min(coc0, Min3(coc1, coc2, coc3));
            half cocMax = max(coc0, Max3(coc1, coc2, coc3));
            half coc = (-cocMin > cocMax ? cocMin : cocMax) * MaxRadius;

            // Premultiply CoC
            avg *= smoothstep(0, _SourceSize.w * 2.0, abs(coc));

        #if defined(UNITY_COLORSPACE_GAMMA)
            avg = GetSRGBToLinear(avg);
        #endif

            return half4(avg, coc);
        }

        void Accumulate(half4 samp0, float2 uv, half4 disp, inout half4 farAcc, inout half4 nearAcc)
        {
            half4 samp = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, ClampAndScaleUVForBilinear(uv + disp.wy, _BlitTexture_TexelSize.xy));

            // Compare CoC of the current sample and the center sample and select smaller one
            half farCoC = max(min(samp0.a, samp.a), 0.0);

            // Compare the CoC to the sample distance & add a small margin to smooth out
            half farWeight = saturate((farCoC - disp.z + _BokehConstants.y) / _BokehConstants.y);
            half nearWeight = saturate((-samp.a - disp.z + _BokehConstants.y) / _BokehConstants.y);

            // Cut influence from focused areas because they're darkened by CoC premultiplying. This is only
            // needed for near field
            nearWeight *= step(_BokehConstants.x, -samp.a);

            // Accumulation
            farAcc += half4(samp.rgb, 1.0h) * farWeight;
            nearAcc += half4(samp.rgb, 1.0h) * nearWeight;
        }

        half4 FragBlur(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord);

            half4 samp0 = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);

            half4 farAcc = 0.0;  // Background: far field bokeh
            half4 nearAcc = 0.0; // Foreground: near field bokeh

            // Center sample isn't in the kernel array, accumulate it separately
            Accumulate(samp0, uv, 0.0, farAcc, nearAcc);

            UNITY_LOOP
            for (int si = 0; si < SAMPLE_COUNT; si++)
            {
                Accumulate(samp0, uv, _BokehKernel[si], farAcc, nearAcc);
            }

            // Get the weighted average
            farAcc.rgb /= farAcc.a + (farAcc.a == 0.0);     // Zero-div guard
            nearAcc.rgb /= nearAcc.a + (nearAcc.a == 0.0);

            // Normalize the total of the weights for the near field
            nearAcc.a *= PI / (SAMPLE_COUNT + 1);

            // Alpha premultiplying (total near field accumulation weight)
            half alpha = saturate(nearAcc.a);
            half3 rgb = lerp(farAcc.rgb, nearAcc.rgb, alpha);

            return half4(rgb, alpha);
        }

        half4 FragPostBlur(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord);

            // 9-tap tent filter with 4 bilinear samples
            float4 duv = _SourceSize.zwzw * _DownSampleScaleFactor.zwzw * float4(0.5, 0.5, -0.5, 0);
            half4 acc;
            acc  = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, ClampUVForBilinear(uv - duv.xy, _BlitTexture_TexelSize.xy));
            acc += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, ClampUVForBilinear(uv - duv.zy, _BlitTexture_TexelSize.xy));
            acc += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, ClampUVForBilinear(uv + duv.zy, _BlitTexture_TexelSize.xy));
            acc += SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, ClampUVForBilinear(uv + duv.xy, _BlitTexture_TexelSize.xy));
            return acc * 0.25;
        }

        half4 FragComposite(Varyings input) : SV_Target
        {
            UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
            float2 uv = UnityStereoTransformScreenSpaceTex(input.texcoord);

            float dofDownSample = 2.0f;
            half4 dof = SAMPLE_TEXTURE2D_X(_DofTexture, sampler_LinearClamp, ClampUVForBilinear(uv, _BlitTexture_TexelSize.xy * dofDownSample));
            half coc = SAMPLE_TEXTURE2D_X(_FullCoCTexture, sampler_LinearClamp, ClampUVForBilinear(uv, _BlitTexture_TexelSize.xy)).r;
            coc = (coc - 0.5) * 2.0 * MaxRadius;

            // Convert CoC to far field alpha value
            float ffa = smoothstep(_SourceSize.w * 2.0, _SourceSize.w * 4.0, coc);

            half4 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, ClampUVForBilinear(uv, _BlitTexture_TexelSize.xy));

        #if defined(UNITY_COLORSPACE_GAMMA)
            color = GetSRGBToLinear(color);
        #endif

            half alpha = Max3(dof.r, dof.g, dof.b);
            half4 outColor = lerp(color, half4(dof.rgb, alpha), ffa + dof.a - ffa * dof.a);

        #if _ENABLE_ALPHA_OUTPUT
            // Preserve the original value of the pixels with zero alpha
            outColor.rgb = color.a > 0 ? outColor.rgb : color.rgb;
            // The BokehDoF does not process source content alpha as it uses the alpha channel to pack CoC and near/far field weights.
            // The outColor.a is an approximate mixed alpha between the sharp and the blurred (near+far) dof layers.
            // It is not the actual blurred alpha of the source content.
            // Keep the original alpha mask.
            outColor.a = color.a;
        #endif

        #if defined(UNITY_COLORSPACE_GAMMA)
            outColor = GetLinearToSRGB(outColor);
        #endif
            return outColor;
        }

    ENDHLSL

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        LOD 100
        ZTest Always ZWrite Off Cull Off

        Pass
        {
            Name "Bokeh Depth Of Field CoC"

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment FragCoC
                #pragma target 4.5
            ENDHLSL
        }

        Pass
        {
            Name "Bokeh Depth Of Field Prefilter"

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment FragPrefilter
                #pragma target 4.5
            ENDHLSL
        }

        Pass
        {
            Name "Bokeh Depth Of Field Blur"

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment FragBlur
                #pragma target 4.5
            ENDHLSL
        }

        Pass
        {
            Name "Bokeh Depth Of Field Post Blur"

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment FragPostBlur
                #pragma target 4.5
            ENDHLSL
        }

        Pass
        {
            Name "Bokeh Depth Of Field Composite"

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment FragComposite
                #pragma target 4.5
                #pragma multi_compile_fragment _ _ENABLE_ALPHA_OUTPUT
            ENDHLSL
        }
    }

    // SM3.5 fallbacks - needed because of the use of Gather
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        LOD 100
        ZTest Always ZWrite Off Cull Off

        Pass
        {
            Name "Bokeh Depth Of Field CoC"

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment FragCoC
                #pragma target 3.5
            ENDHLSL
        }

        Pass
        {
            Name "Bokeh Depth Of Field Prefilter"

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment FragPrefilter
                #pragma target 3.5
            ENDHLSL
        }

        Pass
        {
            Name "Bokeh Depth Of Field Blur"

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment FragBlur
                #pragma target 3.5
            ENDHLSL
        }

        Pass
        {
            Name "Bokeh Depth Of Field Post Blur"

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment FragPostBlur
                #pragma target 3.5
            ENDHLSL
        }

        Pass
        {
            Name "Bokeh Depth Of Field Composite"

            HLSLPROGRAM
                #pragma vertex Vert
                #pragma fragment FragComposite
                #pragma target 3.5
                #pragma multi_compile_fragment _ _ENABLE_ALPHA_OUTPUT
            ENDHLSL
        }
    }
}
