// ENABLE_VR is not defined on Game Core but the assembly is available with limited features when the XR module is enabled.
#if UNITY_INPUT_SYSTEM_ENABLE_XR && (ENABLE_VR || UNITY_GAMECORE) || PACKAGE_DOCS_GENERATION
namespace UnityEngine.InputSystem.XR.Haptics
{
    public struct BufferedRumble
    {
        public HapticCapabilities capabilities { get; private set; }
        InputDevice device { get; set; }

        public BufferedRumble(InputDevice device)
        {
            if (device == null)
                throw new System.ArgumentNullException(nameof(device));

            this.device = device;

            var command = GetHapticCapabilitiesCommand.Create();
            device.ExecuteCommand(ref command);
            capabilities = command.capabilities;
        }

        public void EnqueueRumble(byte[] samples)
        {
            var command = SendBufferedHapticCommand.Create(samples);
            device.ExecuteCommand(ref command);
        }
    }
}
#endif
