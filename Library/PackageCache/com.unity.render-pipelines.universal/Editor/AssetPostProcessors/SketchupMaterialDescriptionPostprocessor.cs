using System.IO;
using UnityEngine;
using UnityEditor.AssetImporters;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    class SketchupMaterialDescriptionPreprocessor : AssetPostprocessor
    {
        static readonly uint k_Version = 2;
        static readonly int k_Order = -980;

        public override uint GetVersion()
        {
            return k_Version;
        }

        public override int GetPostprocessOrder()
        {
            return k_Order;
        }

        public void OnPreprocessMaterialDescription(MaterialDescription description, Material material, AnimationClip[] clips)
        {
            var pipelineAsset = GraphicsSettings.currentRenderPipeline;
            if (!pipelineAsset || pipelineAsset.GetType() != typeof(UniversalRenderPipelineAsset))
                return;

            var lowerCasePath = Path.GetExtension(assetPath).ToLower();
            if (lowerCasePath != ".skp")
                return;

            string path = AssetDatabase.GUIDToAssetPath(ShaderUtils.GetShaderGUID(ShaderPathID.Lit));
            var shader = AssetDatabase.LoadAssetAtPath<Shader>(path);
            if (shader == null)
                return;
            material.shader = shader;

            float floatProperty;
            Vector4 vectorProperty;
            TexturePropertyDescription textureProperty;

            if (description.TryGetProperty("DiffuseMap", out textureProperty) && textureProperty.texture != null)
            {
                SetMaterialTextureProperty("_BaseMap", material, textureProperty);
                SetMaterialTextureProperty("_MainTex", material, textureProperty);
                var color = new Color(1.0f, 1.0f, 1.0f, 1.0f);
                material.SetColor("_BaseColor", color);
                material.SetColor("_Color", color);
            }
            else if (description.TryGetProperty("DiffuseColor", out vectorProperty))
            {
                Color diffuseColor = vectorProperty;
                diffuseColor = PlayerSettings.colorSpace == ColorSpace.Linear ? diffuseColor.gamma : diffuseColor;
                material.SetColor("_BaseColor", diffuseColor);
                material.SetColor("_Color", diffuseColor);
            }

            if (description.TryGetProperty("IsTransparent", out floatProperty) && floatProperty == 1.0f)
            {
                material.SetFloat("_Mode", 3.0f); // From C# enum BlendMode
                material.SetOverrideTag("RenderType", "Transparent");
                material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.One);
                material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                material.SetFloat("_ZWrite", 0.0f);
                material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
                material.SetFloat("_Surface", 1.0f);
                material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }
            else
            {
                material.SetFloat("_Mode", 0.0f); // From C# enum BlendMode
                material.SetOverrideTag("RenderType", "");
                material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.One);
                material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.Zero);
                material.SetFloat("_ZWrite", 1.0f);
                material.DisableKeyword("_ALPHATEST_ON");
                material.DisableKeyword("_ALPHABLEND_ON");
                material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
                material.renderQueue = -1;
                material.SetFloat("_Surface", 0.0f);
                material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
            }
        }

        static void SetMaterialTextureProperty(string propertyName, Material material, TexturePropertyDescription textureProperty)
        {
            material.SetTexture(propertyName, textureProperty.texture);
            material.SetTextureOffset(propertyName, textureProperty.offset);
            material.SetTextureScale(propertyName, textureProperty.scale);
        }
    }
}
