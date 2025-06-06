using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.Universal
{
    internal static class Light2DLookupTexture
    {
        internal static readonly string k_LightLookupProperty = "_LightLookup";
        internal static readonly string k_FalloffLookupProperty = "_FalloffLookup";
        internal static readonly int k_LightLookupID = Shader.PropertyToID(k_LightLookupProperty);
        internal static readonly int k_FalloffLookupID = Shader.PropertyToID(k_FalloffLookupProperty);

        private static Texture2D s_PointLightLookupTexture;
        private static Texture2D s_FalloffLookupTexture;
        private static RTHandle m_LightLookupRTHandle = null;
        private static RTHandle m_FalloffRTHandle = null;

        public static RTHandle GetLightLookupTexture_Rendergraph()
        {
            if (s_PointLightLookupTexture == null || m_LightLookupRTHandle == null)
            {
                var lightLookupTexture = GetLightLookupTexture();

                m_LightLookupRTHandle?.Release();
                m_LightLookupRTHandle = RTHandles.Alloc(lightLookupTexture);
            }

            return m_LightLookupRTHandle;
        }

        public static RTHandle GetFallOffLookupTexture_Rendergraph()
        {
            if (s_FalloffLookupTexture == null || m_FalloffRTHandle == null)
            {
                var fallOffLookupTexture = GetFalloffLookupTexture();

                m_FalloffRTHandle?.Release();
                m_FalloffRTHandle = RTHandles.Alloc(fallOffLookupTexture);
            }

            return m_FalloffRTHandle;
        }

        public static void Release()
        {
            m_FalloffRTHandle?.Release();
            m_LightLookupRTHandle?.Release();
            m_FalloffRTHandle = null;
            m_LightLookupRTHandle = null;
        }

        public static Texture GetLightLookupTexture()
        {
            if (s_PointLightLookupTexture == null)
                s_PointLightLookupTexture = CreatePointLightLookupTexture();
            return s_PointLightLookupTexture;
        }

        public static Texture GetFalloffLookupTexture()
        {
            if (s_FalloffLookupTexture == null)
                s_FalloffLookupTexture = CreateFalloffLookupTexture();
            return s_FalloffLookupTexture;

        }

        private static Texture2D CreatePointLightLookupTexture()
        {
            const int WIDTH = 256;
            const int HEIGHT = 256;

            var textureFormat = GraphicsFormat.R8G8B8A8_UNorm;
            if (SystemInfo.IsFormatSupported(GraphicsFormat.R16G16B16A16_SFloat, GraphicsFormatUsage.SetPixels))
                textureFormat = GraphicsFormat.R16G16B16A16_SFloat;
            else if (SystemInfo.IsFormatSupported(GraphicsFormat.R32G32B32A32_SFloat, GraphicsFormatUsage.SetPixels))
                textureFormat = GraphicsFormat.R32G32B32A32_SFloat;

            var texture = new Texture2D(WIDTH, HEIGHT, textureFormat, TextureCreationFlags.None);
            texture.name = k_LightLookupProperty;
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            var center = new Vector2(WIDTH / 2.0f, HEIGHT / 2.0f);

            for (var y = 0; y < HEIGHT; y++)
            {
                for (var x = 0; x < WIDTH; x++)
                {
                    var pos = new Vector2(x, y);
                    var distance = Vector2.Distance(pos, center);
                    var relPos = pos - center;
                    var direction = center - pos;
                    direction.Normalize();

                    // red   = 1-0 distance
                    // green  = 1-0 angle
                    // blue = direction.x
                    // alpha = direction.y

                    float red;
                    if (x == WIDTH - 1 || y == HEIGHT - 1)
                        red = 0;
                    else
                        red = Mathf.Clamp(1 - (2.0f * distance / WIDTH), 0.0f, 1.0f);

                    var cosAngle = Vector2.Dot(Vector2.down, relPos.normalized);
                    var angle = Mathf.Acos(cosAngle) / Mathf.PI; // 0-1

                    var green = Mathf.Clamp(1 - angle, 0.0f, 1.0f);
                    var blue = direction.x;
                    var alpha = direction.y;

                    var color = new Color(red, green, blue, alpha);

                    texture.SetPixel(x, y, color);
                }
            }
            texture.Apply();
            return texture;
        }

        private static Texture2D CreateFalloffLookupTexture()
        {
            const int WIDTH = 2048;
            const int HEIGHT = 192;

            const GraphicsFormat textureFormat = GraphicsFormat.R8G8B8A8_SRGB;
            var texture = new Texture2D(WIDTH, HEIGHT - 64, textureFormat, TextureCreationFlags.None);
            texture.name = k_FalloffLookupProperty;
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            for (var y = 0; y < HEIGHT; y++)
            {
                var baseValue = (float)(y + 32) / (HEIGHT + 64);
                var lineValue = -baseValue + 1;
                var exponent = Mathf.Log(lineValue) / Mathf.Log(baseValue);

                for (var x = 0; x < WIDTH; x++)
                {
                    var t = (float)x / WIDTH;
                    var red = Mathf.Pow(t, exponent);
                    var color = new Color(red, 0, 0, 1);
                    if (y >= 32 && y < 160)
                        texture.SetPixel(x, y - 32, color);
                }
            }
            texture.Apply();
            return texture;
        }

        //#if UNITY_EDITOR
        //        [MenuItem("Light2D Debugging/Write Light Texture")]
        //        static public void WriteLightTexture()
        //        {
        //            var path = EditorUtility.SaveFilePanel("Save texture as PNG", "", "LightLookupTexture.exr", "png");

        //            CreatePointLightLookupTexture();

        //            byte[] imgData = s_PointLightLookupTexture.EncodeToEXR(Texture2D.EXRFlags.CompressRLE);
        //            if (imgData != null)
        //                File.WriteAllBytes(path, imgData);
        //        }

        //        [MenuItem("Light2D Debugging/Write Falloff Texture")]
        //        static public void WriteCurveTexture()
        //        {
        //            var path = EditorUtility.SaveFilePanel("Save texture as PNG", "", "FalloffLookupTexture.png", "png");

        //            CreateFalloffLookupTexture();

        //            byte[] imgData = s_FalloffLookupTexture.EncodeToPNG();
        //            if (imgData != null)
        //                File.WriteAllBytes(path, imgData);
        //        }
        //#endif
    }
}
