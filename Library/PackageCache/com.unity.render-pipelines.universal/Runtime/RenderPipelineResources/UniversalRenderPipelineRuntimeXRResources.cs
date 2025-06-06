using System;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Class containing shader resources needed in URP for XR.
    /// </summary>
    /// <seealso cref="Shader"/>
    [Serializable]
    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    [Categorization.CategoryInfo(Name = "R: Runtime XR", Order = 1000), HideInInspector]
    public class UniversalRenderPipelineRuntimeXRResources : IRenderPipelineResources
    {
        /// <summary>
        /// Version of the XR Resources
        /// </summary>
        public int version => 0;

        bool IRenderPipelineGraphicsSettings.isAvailableInPlayerBuild => true;

        [SerializeField]
        [ResourcePath("Shaders/XR/XROcclusionMesh.shader")]
        private Shader m_xrOcclusionMeshPS;

        /// <summary>
        /// XR Occlusion mesh shader.
        /// </summary>
        public Shader xrOcclusionMeshPS
        {
            get => m_xrOcclusionMeshPS;
            set => this.SetValueAndNotify(ref m_xrOcclusionMeshPS, value, nameof(m_xrOcclusionMeshPS));
        }

        [SerializeField]
        [ResourcePath("Shaders/XR/XRMirrorView.shader")]
        private Shader m_xrMirrorViewPS;

        /// <summary>
        /// XR Mirror View shader.
        /// </summary>
        public Shader xrMirrorViewPS
        {
            get => m_xrMirrorViewPS;
            set => this.SetValueAndNotify(ref m_xrMirrorViewPS, value, nameof(m_xrMirrorViewPS));
        }

        [SerializeField]
        [ResourcePath("Shaders/XR/XRMotionVector.shader")]
        private Shader m_xrMotionVector;

        /// <summary>
        /// XR MotionVector shader.
        /// </summary>
        public Shader xrMotionVector
        {
            get => m_xrMotionVector;
            set => this.SetValueAndNotify(ref m_xrMotionVector, value, nameof(m_xrMotionVector));
        }

        internal bool valid
        {
            get
            {
                if (xrOcclusionMeshPS == null)
                    return false;

                if (xrMirrorViewPS == null)
                    return false;

                if (m_xrMotionVector == null)
                    return false;

                return true;
            }
        }
    }
}
