namespace UnityEditor.U2D.Aseprite
{
    internal static class TexturePlatformSettingsModal
    {
        static readonly TextureImporterFormat[] k_FormatsWithCompressionSettings =
        {
            TextureImporterFormat.PVRTC_RGB2,
            TextureImporterFormat.PVRTC_RGB4,
            TextureImporterFormat.PVRTC_RGBA2,
            TextureImporterFormat.PVRTC_RGBA4,
            TextureImporterFormat.ETC_RGB4,
            TextureImporterFormat.ETC2_RGBA8,
            TextureImporterFormat.ETC_RGB4,
            TextureImporterFormat.ETC2_RGB4,
            TextureImporterFormat.ETC2_RGB4_PUNCHTHROUGH_ALPHA,
            TextureImporterFormat.ETC2_RGBA8,
            TextureImporterFormat.ASTC_4x4,
            TextureImporterFormat.ASTC_5x5,
            TextureImporterFormat.ASTC_6x6,
            TextureImporterFormat.ASTC_8x8,
            TextureImporterFormat.ASTC_10x10,
            TextureImporterFormat.ASTC_12x12,
        };

        public struct BuildPlatformData
        {
            public string buildTargetName;
            public TextureImporterFormat defaultTextureFormat;
            public BuildTarget[] buildTarget;
        }

        // Add new platforms here
        public static readonly BuildPlatformData[] validBuildPlatform = new BuildPlatformData[]
        {
            new BuildPlatformData()
            {
                buildTargetName = "Default",
                defaultTextureFormat = TextureImporterFormat.Automatic,
                buildTarget = new[]
                {
                    BuildTarget.NoTarget
                }
            },

            new BuildPlatformData()
            {
                buildTargetName = "PC, Mac & Linux Standalone",
                defaultTextureFormat = TextureImporterFormat.RGBA32,
                buildTarget = new[]
                {
                    BuildTarget.StandaloneWindows,
                    BuildTarget.StandaloneWindows64,
                    BuildTarget.StandaloneLinux64,
                    BuildTarget.StandaloneOSX,
                }
            },
            new BuildPlatformData()
            {
                buildTargetName = "iOS",
                defaultTextureFormat = TextureImporterFormat.RGBA32,
                buildTarget = new[] { BuildTarget.iOS }
            },
            new BuildPlatformData()
            {
                buildTargetName = "tvOS",
                defaultTextureFormat = TextureImporterFormat.RGBA32,
                buildTarget = new[] { BuildTarget.tvOS }
            },
            new BuildPlatformData()
            {
                buildTargetName = "Android",
                defaultTextureFormat = TextureImporterFormat.RGBA32,
                buildTarget = new[] { BuildTarget.Android }
            },
            new BuildPlatformData()
            {
                buildTargetName = "Universal Windows Platform",
                defaultTextureFormat = TextureImporterFormat.RGBA32,
                buildTarget = new[] { BuildTarget.WSAPlayer }
            },
        };

        static readonly int[] k_TextureFormatsValueApplePVR =
        {
            (int)TextureImporterFormat.PVRTC_RGB2,
            (int)TextureImporterFormat.PVRTC_RGBA2,
            (int)TextureImporterFormat.PVRTC_RGB4,
            (int)TextureImporterFormat.PVRTC_RGBA4,

            (int)TextureImporterFormat.ASTC_4x4,
            (int)TextureImporterFormat.ASTC_5x5,
            (int)TextureImporterFormat.ASTC_6x6,
            (int)TextureImporterFormat.ASTC_8x8,
            (int)TextureImporterFormat.ASTC_10x10,
            (int)TextureImporterFormat.ASTC_12x12,

            (int)TextureImporterFormat.RGB16,
            (int)TextureImporterFormat.RGB24,
            (int)TextureImporterFormat.Alpha8,
            (int)TextureImporterFormat.RGBA16,
            (int)TextureImporterFormat.RGBA32
        };

        static readonly int[] k_TextureFormatsValueAndroid =
        {
            (int)TextureImporterFormat.DXT1,
            (int)TextureImporterFormat.DXT5,
#if ENABLE_CRUNCH_TEXTURE_COMPRESSION
            (int)TextureImporterFormat.DXT1Crunched,
            (int)TextureImporterFormat.DXT5Crunched,
#endif

            (int)TextureImporterFormat.ETC_RGB4,

            (int)TextureImporterFormat.ETC2_RGB4,
            (int)TextureImporterFormat.ETC2_RGB4_PUNCHTHROUGH_ALPHA,
            (int)TextureImporterFormat.ETC2_RGBA8,


            (int)TextureImporterFormat.PVRTC_RGB2,
            (int)TextureImporterFormat.PVRTC_RGBA2,
            (int)TextureImporterFormat.PVRTC_RGB4,
            (int)TextureImporterFormat.PVRTC_RGBA4,

            (int)TextureImporterFormat.ETC_RGB4,
            (int)TextureImporterFormat.ETC2_RGBA8,

            (int)TextureImporterFormat.ASTC_4x4,
            (int)TextureImporterFormat.ASTC_5x5,
            (int)TextureImporterFormat.ASTC_6x6,
            (int)TextureImporterFormat.ASTC_8x8,
            (int)TextureImporterFormat.ASTC_10x10,
            (int)TextureImporterFormat.ASTC_12x12,

            (int)TextureImporterFormat.RGB16,
            (int)TextureImporterFormat.RGB24,
            (int)TextureImporterFormat.Alpha8,
            (int)TextureImporterFormat.RGBA16,
            (int)TextureImporterFormat.RGBA32
        };

        static readonly int[] k_TextureFormatsValueSTV =
        {
            (int)TextureImporterFormat.ETC_RGB4,

            (int)TextureImporterFormat.RGB16,
            (int)TextureImporterFormat.RGB24,
            (int)TextureImporterFormat.Alpha8,
            (int)TextureImporterFormat.RGBA16,
            (int)TextureImporterFormat.RGBA32,
        };

        static readonly int[] k_TextureFormatsValueWebGL =
        {
            (int)TextureImporterFormat.DXT1,
            (int)TextureImporterFormat.DXT5,
#if ENABLE_CRUNCH_TEXTURE_COMPRESSION
            (int)TextureImporterFormat.DXT1Crunched,
            (int)TextureImporterFormat.DXT5Crunched,
#endif
            (int)TextureImporterFormat.RGB16,
            (int)TextureImporterFormat.RGB24,
            (int)TextureImporterFormat.Alpha8,
            (int)TextureImporterFormat.ARGB16,
            (int)TextureImporterFormat.RGBA32
        };

        public static readonly int[] kNormalFormatsValueDefault =
        {
            (int)TextureImporterFormat.BC5,
            (int)TextureImporterFormat.BC7,
            (int)TextureImporterFormat.DXT5,
#if ENABLE_CRUNCH_TEXTURE_COMPRESSION
            (int)TextureImporterFormat.DXT5Crunched,
#endif
            (int)TextureImporterFormat.ARGB16,
            (int)TextureImporterFormat.RGBA32,
        };

        static readonly int[] k_TextureFormatsValueDefault =
        {
            (int)TextureImporterFormat.DXT1,
            (int)TextureImporterFormat.DXT5,
#if ENABLE_CRUNCH_TEXTURE_COMPRESSION
            (int)TextureImporterFormat.DXT1Crunched,
            (int)TextureImporterFormat.DXT5Crunched,
#endif
            (int)TextureImporterFormat.RGB16,
            (int)TextureImporterFormat.RGB24,
            (int)TextureImporterFormat.Alpha8,
            (int)TextureImporterFormat.ARGB16,
            (int)TextureImporterFormat.RGBA32,
            (int)TextureImporterFormat.RGBAHalf,
            (int)TextureImporterFormat.BC4,
            (int)TextureImporterFormat.BC5,
            (int)TextureImporterFormat.BC6H,
            (int)TextureImporterFormat.BC7,
        };

        static readonly int[] k_TextureFormatsValueSingleChannel =
        {
            (int)TextureImporterFormat.Alpha8,
            (int)TextureImporterFormat.BC4,
        };

        static string[] s_TextureFormatStringsWebGL;
        static string[] s_TextureFormatStringsApplePVR;
        static string[] s_TextureFormatStringsAndroid;
        static string[] s_TextureFormatStringsSTV;
        static string[] s_TextureFormatStringsSingleChannel;
        static string[] s_TextureFormatStringsDefault;

        static TexturePlatformSettingsModal()
        {
            s_TextureFormatStringsApplePVR = BuildTextureStrings(k_TextureFormatsValueApplePVR);
            s_TextureFormatStringsAndroid = BuildTextureStrings(k_TextureFormatsValueAndroid);
            s_TextureFormatStringsSTV = BuildTextureStrings(k_TextureFormatsValueSTV);
            s_TextureFormatStringsWebGL = BuildTextureStrings(k_TextureFormatsValueWebGL);
            s_TextureFormatStringsDefault = BuildTextureStrings(k_TextureFormatsValueDefault);
            s_TextureFormatStringsSingleChannel = BuildTextureStrings(k_TextureFormatsValueSingleChannel);
        }

        static string[] BuildTextureStrings(int[] texFormatValues)
        {
            var retval = new string[texFormatValues.Length];
            for (var i = 0; i < texFormatValues.Length; i++)
            {
                var val = texFormatValues[i];
                retval[i] = GetTextureFormatString((TextureImporterFormat)val);
            }
            return retval;
        }

        static string GetTextureFormatString(TextureImporterFormat format)
        {
            switch (format)
            {
                case TextureImporterFormat.DXT1:
                    return "RGB Crunched DTX1";
                case TextureImporterFormat.DXT5:
                    return "RGB Crunched DTX5";
                case TextureImporterFormat.RGB16:
                    return "RGB 16 bit";
                case TextureImporterFormat.RGB24:
                    return "RGB 24 bit";
                case TextureImporterFormat.Alpha8:
                    return "Alpha 8";
                case TextureImporterFormat.ARGB16:
                    return "ARGB 16 bit";
                case TextureImporterFormat.RGBA32:
                    return "RGBA 32 bit";
                case TextureImporterFormat.ARGB32:
                    return "ARGB 32 bit";
                case TextureImporterFormat.RGBA16:
                    return "RGBA 16 bit";
                case TextureImporterFormat.RGBAHalf:
                    return "RGBA Half";

                case TextureImporterFormat.BC4:
                    return "R Compressed BC4";
                case TextureImporterFormat.BC5:
                    return "RG Compressed BC5";
                case TextureImporterFormat.BC6H:
                    return "RGB HDR Compressed BC6H";
                case TextureImporterFormat.BC7:
                    return "RGB(A) Compressed BC7";
                case TextureImporterFormat.PVRTC_RGB2:
                    return "RGB Compressed PVRTC 2 bits";
                case TextureImporterFormat.PVRTC_RGBA2:
                    return "RGBA Compressed PVRTC 2 bits";
                case TextureImporterFormat.PVRTC_RGB4:
                    return "RGB Compressed PVRTC 4 bits";
                case TextureImporterFormat.PVRTC_RGBA4:
                    return "RGBA Compressed PVRTC 4 bits";
                case TextureImporterFormat.ETC_RGB4:
                    return "RGB Compressed ETC 4 bits";

                case TextureImporterFormat.EAC_R:
                    return "11-bit R Compressed EAC 4 bit";
                case TextureImporterFormat.EAC_R_SIGNED:
                    return "11-bit signed R Compressed EAC 4 bit";
                case TextureImporterFormat.EAC_RG:
                    return "11-bit RG Compressed EAC 8 bit";
                case TextureImporterFormat.EAC_RG_SIGNED:
                    return "11-bit signed RG Compressed EAC 8 bit";

                case TextureImporterFormat.ETC2_RGB4:
                    return "RGB Compressed ETC2 4 bits";
                case TextureImporterFormat.ETC2_RGB4_PUNCHTHROUGH_ALPHA:
                    return "RGB + 1-bit Alpha Compressed ETC2 4 bits";
                case TextureImporterFormat.ETC2_RGBA8:
                    return "RGBA Compressed ETC2 8 bits";

                case TextureImporterFormat.ASTC_4x4:
                    return "RGB(A) Compressed ASTC 4 x 4 block";
                case TextureImporterFormat.ASTC_5x5:
                    return "RGB(A) Compressed ASTC 5 x 5 block";
                case TextureImporterFormat.ASTC_6x6:
                    return "RGB(A) Compressed ASTC 6 x 6 block";
                case TextureImporterFormat.ASTC_8x8:
                    return "RGB(A) Compressed ASTC 8 x 8 block";
                case TextureImporterFormat.ASTC_10x10:
                    return "RGB(A) Compressed ASTC 10 x 10 block";
                case TextureImporterFormat.ASTC_12x12:
                    return "RGB(A) Compressed ASTC 12 x 12 block";
            }
            return "Unsupported";
        }
    }
}
