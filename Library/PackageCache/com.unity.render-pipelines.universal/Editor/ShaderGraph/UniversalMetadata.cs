using System;
using UnityEngine;
using Unity.Rendering.Universal;

namespace UnityEditor.Rendering.Universal.ShaderGraph
{
    // This is a metadata object attached to ShaderGraph import asset results by the Universal Target
    // it contains any additional information that we might want to know about the Universal shader
    [Serializable]
    sealed class UniversalMetadata : ScriptableObject
    {
        [SerializeField]
        ShaderUtils.ShaderID m_ShaderID;

        [SerializeField]
        bool m_AllowMaterialOverride;

        [SerializeField]
        SurfaceType m_SurfaceType;

        [SerializeField]
        AlphaMode m_AlphaMode;

        [SerializeField]
        bool m_CastShadows;

        [SerializeField]
        bool m_HasVertexModificationInMotionVector;

        [SerializeField]
        bool m_IsVFXCompatible;

        public ShaderUtils.ShaderID shaderID
        {
            get => m_ShaderID;
            set => m_ShaderID = value;
        }

        public bool allowMaterialOverride
        {
            get => m_AllowMaterialOverride;
            set => m_AllowMaterialOverride = value;
        }
        public SurfaceType surfaceType
        {
            get => m_SurfaceType;
            set => m_SurfaceType = value;
        }

        public AlphaMode alphaMode
        {
            get => m_AlphaMode;
            set => m_AlphaMode = value;
        }

        public bool castShadows
        {
            get => m_CastShadows;
            set => m_CastShadows = value;
        }

        public bool hasVertexModificationInMotionVector
        {
            get => m_HasVertexModificationInMotionVector;
            set => m_HasVertexModificationInMotionVector = value;
        }

        public bool isVFXCompatible
        {
            get => m_IsVFXCompatible;
            set => m_IsVFXCompatible = value;
        }
    }
}
