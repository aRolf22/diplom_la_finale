using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEngine.Rendering.Tests
{
    [HideInInspector]
    [VolumeComponentMenu("Supported On Tests/Not Specified Pipeline Supported On")]
    [SupportedOnRenderPipeline]
    class VolumeComponentNotSpecifiedSupportedOn : VolumeComponent
    {
    }
}
