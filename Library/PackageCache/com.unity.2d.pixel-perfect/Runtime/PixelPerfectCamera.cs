namespace UnityEngine.U2D
{
    /// <summary>
    /// The Pixel Perfect Camera component ensures your pixel art remains crisp and clear at different resolutions, and stable in motion.
    /// </summary>
    [DisallowMultipleComponent]
#if ENABLE_URP
    [AddComponentMenu("")]
#else
    [AddComponentMenu("Rendering/Pixel Perfect Camera")]
#endif
    [RequireComponent(typeof(Camera))]
    [HelpURL("https://docs.unity3d.com/Packages/com.unity.2d.pixel-perfect@latest/index.html?subfolder=/manual/index.html%23properties")]
    public class PixelPerfectCamera : MonoBehaviour, IPixelPerfectCamera
    {
        /// <summary>
        /// Match this value to to the Pixels Per Unit values of all Sprites within the Scene.
        /// </summary>
        public int assetsPPU { get { return m_AssetsPPU; } set { m_AssetsPPU = value > 0 ? value : 1; } }

        /// <summary>
        /// The original horizontal resolution your Assets are designed for.
        /// </summary>
        public int refResolutionX { get { return m_RefResolutionX; } set { m_RefResolutionX = value > 0 ? value : 1; } }

        /// <summary>
        /// Original vertical resolution your Assets are designed for.
        /// </summary>
        public int refResolutionY { get { return m_RefResolutionY; } set { m_RefResolutionY = value > 0 ? value : 1; } }

        /// <summary>
        /// Set to true to have the Scene rendered to a temporary texture set as close as possible to the Reference Resolution,
        /// while maintaining the full screen aspect ratio. This temporary texture is then upscaled to fit the full screen.
        /// </summary>
        public bool upscaleRT { get { return m_UpscaleRT; } set { m_UpscaleRT = value; } }

        /// <summary>
        /// Set to true to prevent subpixel movement and make Sprites appear to move in pixel-by-pixel increments.
        /// Only applicable when upscaleRT is false.
        /// </summary>
        public bool pixelSnapping { get { return m_PixelSnapping; } set { m_PixelSnapping = value; } }

        /// <summary>
        /// Set to true to crop the viewport with black bars to match refResolutionX in the horizontal direction.
        /// </summary>
        public bool cropFrameX { get { return m_CropFrameX; } set { m_CropFrameX = value; } }

        /// <summary>
        /// Set to true to crop the viewport with black bars to match refResolutionY in the vertical direction.
        /// </summary>
        public bool cropFrameY { get { return m_CropFrameY; } set { m_CropFrameY = value; } }

        /// <summary>
        /// Set to true to expand the viewport to fit the screen resolution while maintaining the viewport's aspect ratio.
        /// Only applicable when both cropFrameX and cropFrameY are true.
        /// </summary>
        public bool stretchFill { get { return m_StretchFill; } set { m_StretchFill = value; } }

        /// <summary>
        /// Ratio of the rendered Sprites compared to their original size (readonly).
        /// </summary>
        public int pixelRatio
        {
            get
            {
                if (m_CinemachineCompatibilityMode)
                {
                    if (m_UpscaleRT)
                        return m_Internal.zoom * m_Internal.cinemachineVCamZoom;
                    else
                        return m_Internal.cinemachineVCamZoom;
                }
                else
                {
                    return m_Internal.zoom;
                }
            }
        }

        /// <summary>
        /// Round a arbitrary position to an integer pixel position. Works in world space.
        /// </summary>
        /// <param name="position"> The position you want to round.</param>
        /// <returns>
        /// The rounded pixel position.
        /// Depending on the values of upscaleRT and pixelSnapping, it could be a screen pixel position or an art pixel position.
        /// </returns>
        public Vector3 RoundToPixel(Vector3 position)
        {
            float unitsPerPixel = m_Internal.unitsPerPixel;
            if (unitsPerPixel == 0.0f)
                return position;

            Vector3 result;
            result.x = Mathf.Round(position.x / unitsPerPixel) * unitsPerPixel;
            result.y = Mathf.Round(position.y / unitsPerPixel) * unitsPerPixel;
            result.z = Mathf.Round(position.z / unitsPerPixel) * unitsPerPixel;

            return result;
        }

        /// <summary>
        /// Find a pixel-perfect orthographic size as close to targetOrthoSize as possible. Used by Cinemachine to solve compatibility issues with Pixel Perfect Camera.
        /// </summary>
        /// <param name="targetOrthoSize">Orthographic size from the live Cinemachine Virtual Camera.</param>
        /// <returns>The corrected orthographic size.</returns>
        public float CorrectCinemachineOrthoSize(float targetOrthoSize)
        {
            m_CinemachineCompatibilityMode = true;

            if (m_Internal == null)
                return targetOrthoSize;
            else
                return m_Internal.CorrectCinemachineOrthoSize(targetOrthoSize);
        }

        [SerializeField]
        int m_AssetsPPU = 100;

        [SerializeField]
        int m_RefResolutionX = 320;

        [SerializeField]
        int m_RefResolutionY = 180;

        [SerializeField]
        bool m_UpscaleRT = false;

        [SerializeField]
        bool m_PixelSnapping = false;

        [SerializeField]
        bool m_CropFrameX = false;

        [SerializeField]
        bool m_CropFrameY = false;

        [SerializeField]
        bool m_StretchFill = false;

        Camera m_Camera;
        PixelPerfectCameraInternal m_Internal;
        bool m_CinemachineCompatibilityMode;

        // Snap camera position to pixels using Camera.worldToCameraMatrix.
        void PixelSnap()
        {
            Vector3 cameraPosition = m_Camera.transform.position;
            Vector3 roundedCameraPosition = RoundToPixel(cameraPosition);
            Vector3 offset = roundedCameraPosition - cameraPosition;
            offset.z = -offset.z;
            Matrix4x4 offsetMatrix = Matrix4x4.TRS(-offset, Quaternion.identity, new Vector3(1.0f, 1.0f, -1.0f));

            m_Camera.worldToCameraMatrix = offsetMatrix * m_Camera.transform.worldToLocalMatrix;
        }

        void Awake()
        {
            m_Camera = GetComponent<Camera>();
            m_Internal = new PixelPerfectCameraInternal(this);

            m_Internal.originalOrthoSize = m_Camera.orthographicSize;
            m_Internal.hasPostProcessLayer = GetComponent("PostProcessLayer") != null;   // query the component by name to avoid hard dependency

            if (m_Camera.targetTexture != null)
                Debug.LogWarning("Render to texture is not supported by Pixel Perfect Camera.", m_Camera);
        }

        void LateUpdate()
        {
            m_Internal.CalculateCameraProperties(Screen.width, Screen.height);

            // To be effective immediately this frame, forceIntoRenderTexture should be set before any camera rendering callback.
            // An exception of this is when the editor is paused, where we call LateUpdate() manually in OnPreCall().
            // In this special case, you'll see one frame of glitch when toggling renderUpscaling on and off.
            m_Camera.forceIntoRenderTexture = m_Internal.hasPostProcessLayer || m_Internal.useOffscreenRT;
        }

        void OnPreCull()
        {
#if UNITY_EDITOR
            // LateUpdate() is not called while the editor is paused, but OnPreCull() is.
            // So call LateUpdate() manually here.
            if (UnityEditor.EditorApplication.isPaused)
                LateUpdate();
#endif

            PixelSnap();

            if (m_Internal.pixelRect != Rect.zero)
                m_Camera.pixelRect = m_Internal.pixelRect;
            else
                m_Camera.rect = new Rect(0.0f, 0.0f, 1.0f, 1.0f);

            // In Cinemachine compatibility mode the control over orthographic size should
            // be given to the virtual cameras, whose orthographic sizes will be corrected to
            // be pixel-perfect. This way when there's blending between virtual cameras, we
            // can have temporary not-pixel-perfect but smooth transitions.
            if (!m_CinemachineCompatibilityMode)
            {
                m_Camera.orthographicSize = m_Internal.orthoSize;
            }
        }

        void OnPreRender()
        {
            // Clear the screen to black so that we can see black bars.
            // Need to do it before anything is drawn if we're rendering directly to the screen.
            if (m_Internal.cropFrameXOrY)
                GL.Clear(false, true, Color.black);

            PixelPerfectRendering.pixelSnapSpacing = m_Internal.unitsPerPixel;
        }

        void OnPostRender()
        {
            PixelPerfectRendering.pixelSnapSpacing = 0.0f;

            if (!m_Internal.useOffscreenRT)
                return;

            RenderTexture activeRT = m_Camera.activeTexture;
            if (activeRT != null)
                activeRT.filterMode = m_Internal.useStretchFill ? FilterMode.Bilinear : FilterMode.Point;

            m_Camera.pixelRect = m_Internal.CalculatePostRenderPixelRect(m_Camera.aspect, Screen.width, Screen.height);
        }

        void OnEnable()
        {
            m_CinemachineCompatibilityMode = false;

#if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlaying)
                UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeChanged;
#endif
        }

        internal void OnDisable()
        {
            m_Camera.rect = new Rect(0.0f, 0.0f, 1.0f, 1.0f);
            m_Camera.orthographicSize = m_Internal.originalOrthoSize;
            m_Camera.forceIntoRenderTexture = m_Internal.hasPostProcessLayer;
            m_Camera.ResetAspect();
            m_Camera.ResetWorldToCameraMatrix();

#if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlaying)
                UnityEditor.EditorApplication.playModeStateChanged -= OnPlayModeChanged;
#endif
        }

#if DEVELOPMENT_BUILD || UNITY_EDITOR
        // Show on-screen warning about invalid render resolutions.
        void OnGUI()
        {
#if UNITY_EDITOR
            if (!UnityEditor.EditorApplication.isPlaying && !runInEditMode)
                return;
#endif

            Color oldColor = GUI.color;
            GUI.color = Color.red;

            Vector2Int renderResolution = Vector2Int.zero;
            renderResolution.x = m_Internal.useOffscreenRT ? m_Internal.offscreenRTWidth : m_Camera.pixelWidth;
            renderResolution.y = m_Internal.useOffscreenRT ? m_Internal.offscreenRTHeight : m_Camera.pixelHeight;

            if (renderResolution.x % 2 != 0 || renderResolution.y % 2 != 0)
            {
                string warning = string.Format("Rendering at an odd-numbered resolution ({0} * {1}). Pixel Perfect Camera may not work properly in this situation.", renderResolution.x, renderResolution.y);
                GUILayout.Box(warning);
            }

            if (Screen.width < refResolutionX || Screen.height < refResolutionY)
            {
                GUILayout.Box("Screen resolution is smaller than the reference resolution. Image may appear stretched or cropped.");
            }

            GUI.color = oldColor;
        }
#endif

#if UNITY_EDITOR
        void OnPlayModeChanged(UnityEditor.PlayModeStateChange state)
        {
            // Stop running in edit mode when entering play mode.
            if (state == UnityEditor.PlayModeStateChange.ExitingEditMode)
            {
                runInEditMode = false;
                OnDisable();
            }
        }
#endif
    }
}