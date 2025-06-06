using UnityEngine.EventSystems;

namespace UnityEngine.UI
{
    internal static class MultipleDisplayUtilities
    {
        /// <summary>
        /// Converts the current drag position into a relative position for the display.
        /// </summary>
        /// <param name="eventData"></param>
        /// <param name="position"></param>
        /// <returns>Returns true except when the drag operation is not on the same display as it originated</returns>
        public static bool GetRelativeMousePositionForDrag(PointerEventData eventData, ref Vector2 position)
        {
            #if UNITY_EDITOR
            position = eventData.position;
            #else
            int pressDisplayIndex = eventData.pointerPressRaycast.displayIndex;
            var relativePosition = RelativeMouseAtScaled(eventData.position, eventData.displayIndex);
            int currentDisplayIndex = (int)relativePosition.z;

            // Discard events on a different display.
            if (currentDisplayIndex != pressDisplayIndex)
                return false;

            // If we are not on the main display then we must use the relative position.
            position = pressDisplayIndex != 0 ? (Vector2)relativePosition : eventData.position;
            #endif
            return true;
        }

        internal static Vector3 GetRelativeMousePositionForRaycast(PointerEventData eventData)
        {
            // The multiple display system is not supported on all platforms, when it is not supported the returned position
            // will be all zeros so when the returned index is 0 we will default to the event data to be safe.
            Vector3 eventPosition = RelativeMouseAtScaled(eventData.position, eventData.displayIndex);
            if (eventPosition == Vector3.zero)
            {
                eventPosition = eventData.position;
#if UNITY_EDITOR
                eventPosition.z = Display.activeEditorGameViewTarget;
#endif
                // We don't really know in which display the event occurred. We will process the event assuming it occurred in our display.
            }

            // We support multiple display on some platforms. When supported:
            //  - InputSystem will set eventData.displayIndex
            //  - Old Input System will set eventPosition.z
            //
            // If the event is on the main display, both displayIndex and eventPosition.z
            // will be 0 so in that case we can leave the eventPosition untouched (see UUM-47650).
#if ENABLE_INPUT_SYSTEM && PACKAGE_INPUTSYSTEM
            if (eventData.displayIndex > 0)
                eventPosition.z = eventData.displayIndex;
#endif

            return eventPosition;
        }

        /// <summary>
        /// A version of Display.RelativeMouseAt that scales the position when the main display has a different rendering resolution to the system resolution.
        /// By default, the mouse position is relative to the main render area, we need to adjust this so it is relative to the system resolution
        /// in order to correctly determine the position on other displays.
        /// </summary>
        /// <returns></returns>
        public static Vector3 RelativeMouseAtScaled(Vector2 position, int displayIndex)
        {
            #if !UNITY_EDITOR && !UNITY_WSA
            // If the main display is now the same resolution as the system then we need to scale the mouse position. (case 1141732)
            if (Display.main.renderingWidth != Display.main.systemWidth || Display.main.renderingHeight != Display.main.systemHeight)
            {
                // The system will add padding when in full-screen and using a non-native aspect ratio. (case UUM-7893)
                // For example Rendering 1920x1080 with a systeem resolution of 3440x1440 would create black bars on each side that are 330 pixels wide.
                // we need to account for this or it will offset our coordinates when we are not on the main display.
                var systemAspectRatio = Display.main.systemWidth / (float)Display.main.systemHeight;

                var sizePlusPadding = new Vector2(Display.main.renderingWidth, Display.main.renderingHeight);
                var padding = Vector2.zero;
                if (Screen.fullScreen)
                {
                    var aspectRatio = Screen.width / (float)Screen.height;
                    if (Display.main.systemHeight * aspectRatio < Display.main.systemWidth)
                    {
                        // Horizontal padding
                        sizePlusPadding.x = Display.main.renderingHeight * systemAspectRatio;
                        padding.x = (sizePlusPadding.x - Display.main.renderingWidth) * 0.5f;
                    }
                    else
                    {
                        // Vertical padding
                        sizePlusPadding.y = Display.main.renderingWidth / systemAspectRatio;
                        padding.y = (sizePlusPadding.y - Display.main.renderingHeight) * 0.5f;
                    }
                }

                var sizePlusPositivePadding = sizePlusPadding - padding;

                // If we are not inside of the main display then we must adjust the mouse position so it is scaled by
                // the main display and adjusted for any padding that may have been added due to different aspect ratios.
                if (position.y < -padding.y || position.y > sizePlusPositivePadding.y ||
                     position.x < -padding.x || position.x > sizePlusPositivePadding.x)
                {
                    var adjustedPosition = position;

                    if (!Screen.fullScreen)
                    {
                        // When in windowed mode, the window will be centered with the 0,0 coordinate at the top left, we need to adjust so it is relative to the screen instead.
                        adjustedPosition.x -= (Display.main.renderingWidth - Display.main.systemWidth) * 0.5f;
                        adjustedPosition.y -= (Display.main.renderingHeight - Display.main.systemHeight) * 0.5f;
                    }
                    else
                    {
                        // Scale the mouse position to account for the black bars when in a non-native aspect ratio.
                        adjustedPosition += padding;
                        adjustedPosition.x *= Display.main.systemWidth / sizePlusPadding.x;
                        adjustedPosition.y *= Display.main.systemHeight / sizePlusPadding.y;
                    }

					// fix for UUM-63551: Use the display index provided to this method.  Display.RelativeMouseAt( ) no longer works starting with 2021 LTS and new input system
					// as the Pointer position is reported in Window coordinates rather than relative to the primary window as Display.RelativeMouseAt( ) expects.
#if ENABLE_INPUT_SYSTEM && PACKAGE_INPUTSYSTEM && (UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX)
                    var relativePos = new Vector3(adjustedPosition.x, adjustedPosition.y, displayIndex);
#else
                    var relativePos = Display.RelativeMouseAt(adjustedPosition);
#endif

                    // If we are not on the main display then return the adjusted position.
                    if (relativePos.z != 0)
                        return relativePos;
                }

                // We are using the main display.
                return new Vector3(position.x, position.y, 0);
            }
#endif

			// fix for UUM-63551: Use the display index provided to this method.  Display.RelativeMouseAt( ) no longer works starting with 2021 LTS and new input system
			// as the Pointer position is reported in Window coordinates rather than relative to the primary window as Display.RelativeMouseAt( ) expects.
#if ENABLE_INPUT_SYSTEM && PACKAGE_INPUTSYSTEM && (UNITY_STANDALONE_WIN || UNITY_STANDALONE_LINUX)
            return new Vector3(position.x, position.y, displayIndex);
#else
            return Display.RelativeMouseAt(position);
#endif
        }
    }
}
