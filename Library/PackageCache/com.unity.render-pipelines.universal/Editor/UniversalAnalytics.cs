using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Analytics;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace UnityEditor.Rendering.Universal
{
    class UniversalAnalytics : IPostprocessBuildWithReport
    {
        const int k_MaxEventsPerHour = 1000;
        const int k_MaxNumberOfElements = 1000;
        const string k_VendorKey = "unity.universal";
        const string k_EventName = "uUniversalRenderPipelineUsage";


        [AnalyticInfo(eventName: k_EventName, vendorKey: k_VendorKey, maxEventsPerHour: k_MaxEventsPerHour, maxNumberOfElements: k_MaxNumberOfElements)]
        class Analytic : IAnalytic
        {
            public bool TryGatherData(out IAnalytic.IData data, out Exception error)
            {
                data = null;
                UniversalRenderPipelineAsset rendererAsset = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;

                if (rendererAsset != null)
                {
                    ScriptableRendererData[] rendererDataList = rendererAsset.m_RendererDataList;

                    string mainLightMode = rendererAsset.mainLightRenderingMode.ToString();
                    string additionalLightMode = rendererAsset.additionalLightsRenderingMode.ToString();

                    HashSet<string> rendererDatas = new HashSet<string>();
                    HashSet<string> renderFeatures = new HashSet<string>();
                    int rendererDataAmount = 0;
                    int rendererFeaturesAmount = 0;

                    foreach (ScriptableRendererData rendererData in rendererDataList)
                    {
                        if (rendererData != null)
                        {
                            rendererDataAmount++;
                            rendererDatas.Add(rendererData.GetType().ToString());
                            foreach (ScriptableRendererFeature rendererFeature in rendererData.rendererFeatures)
                            {
                                if (rendererFeature != null)
                                {
                                    rendererFeaturesAmount++;
                                    renderFeatures.Add(rendererFeature.GetType().ToString());
                                }
                            }
                        }
                    }

                    data = new AnalyticsData()
                    {
                        renderer_data = rendererDatas.ToArray(),
                        renderer_data_amount = rendererDataAmount,
                        renderer_features = renderFeatures.ToArray(),
                        renderer_features_amount = rendererFeaturesAmount,
                        main_light_rendering_mode = mainLightMode,
                        additional_light_rendering_mode = additionalLightMode,
                    };

                }
                error = null;
                return true;
            }


            [Serializable]
            struct AnalyticsData : IAnalytic.IData
            {
                public string[] renderer_data;
                public int renderer_data_amount;
                public string[] renderer_features;
                public int renderer_features_amount;
                public string main_light_rendering_mode;
                public string additional_light_rendering_mode;
            }
        }

        static void SendUniversalEvent()
        {
            //The event shouldn't be able to report if this is disabled but if we know we're not going to report
            //Lets early out and not waste time gathering all the data
            if (!EditorAnalytics.enabled)
                return;

            Analytic analytic = new Analytic();
            EditorAnalytics.SendAnalytic(analytic);
        }
        
        public int callbackOrder { get; }
        public void OnPostprocessBuild(BuildReport report)
        {
            SendUniversalEvent();
        }
    }
}
