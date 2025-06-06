using UnityEngine;

namespace UnityEditor.U2D.Sprites
{
    internal partial class SpriteFrameModule : SpriteFrameModuleBase
    {
        private static class SpriteFrameModuleStyles
        {
            public static readonly GUIContent sliceButtonLabel = EditorGUIUtility.TrTextContent("Slice");
            public static readonly GUIContent trimButtonLabel = EditorGUIUtility.TrTextContent("Trim", "Trims selected rectangle (T)");
        }

        // overrides for SpriteFrameModuleBase
        public override void DoMainGUI()
        {
            // Beautification. Run ValidateSpriteRects only when window is presented.
            if(!m_SpriteRectValidated)
            {
                EditorApplication.delayCall += ValidateSpriteRects;
            }

            // Do nothing when extension is activated.
            if (m_CurrentMode != null)
            {
                m_CurrentMode.DoMainGUI();
                return;
            }
            base.DoMainGUI();
            DrawSpriteRectGizmos();
            DrawPotentialSpriteRectGizmos();

            if (!spriteEditor.editingDisabled)
            {
                HandleGizmoMode();

                if (containsMultipleSprites)
                    HandleRectCornerScalingHandles();

                HandleBorderCornerScalingHandles();
                HandleBorderSidePointScalingSliders();

                if (containsMultipleSprites)
                    HandleRectSideScalingHandles();

                HandleBorderSideScalingHandles();
                HandlePivotHandle();

                if (containsMultipleSprites)
                    HandleDragging();

                spriteEditor.HandleSpriteSelection();

                if (containsMultipleSprites)
                {
                    HandleCreate();
                    HandleDelete();
                    HandleDuplicate();
                }
                spriteEditor.spriteRects = m_RectsCache.GetSpriteRects();
            }
        }

        private void DrawPotentialSpriteRectGizmos()
        {
            if (m_PotentialRects != null && m_PotentialRects.Count > 0)
                DrawRectGizmos(m_PotentialRects, Color.red);
        }

        public override void DoToolbarGUI(Rect toolbarRect)
        {
            using (new EditorGUI.DisabledScope(!containsMultipleSprites || spriteEditor.editingDisabled || m_TextureDataProvider.GetReadableTexture2D() == null || m_CurrentMode != null))
            {
                GUIStyle skin = EditorStyles.toolbarPopup;

                Rect drawArea = toolbarRect;

                drawArea.width = skin.CalcSize(SpriteFrameModuleStyles.sliceButtonLabel).x;
                SpriteUtilityWindow.DrawToolBarWidget(ref drawArea, ref toolbarRect, (adjustedDrawArea) =>
                {
                    if (GUI.Button(adjustedDrawArea, SpriteFrameModuleStyles.sliceButtonLabel, skin))
                    {
                        if (SpriteEditorMenu.ShowAtPosition(adjustedDrawArea, this, this))
                            GUIUtility.ExitGUI();
                    }
                });

                using (new EditorGUI.DisabledScope(!hasSelected))
                {
                    drawArea.x += drawArea.width;
                    drawArea.width = skin.CalcSize(SpriteFrameModuleStyles.trimButtonLabel).x;
                    SpriteUtilityWindow.DrawToolBarWidget(ref drawArea, ref toolbarRect, (adjustedDrawArea) =>
                    {
                        if (GUI.Button(adjustedDrawArea, SpriteFrameModuleStyles.trimButtonLabel, EditorStyles.toolbarButton))
                        {
                            TrimAlpha();
                            Repaint();
                        }
                    });
                }
            }
        }

        private void HandleRectCornerScalingHandles()
        {
            if (!hasSelected)
                return;

            GUIStyle dragDot = styles.dragdot;
            GUIStyle dragDotActive = styles.dragdotactive;
            var color = Color.white;

            Rect rect = new Rect(selectedSpriteRect_Rect);

            float left = rect.xMin;
            float right = rect.xMax;
            float top = rect.yMax;
            float bottom = rect.yMin;

            EditorGUI.BeginChangeCheck();

            HandleBorderPointSlider(ref left, ref top, MouseCursor.ResizeUpLeft, false, dragDot, dragDotActive, color);
            HandleBorderPointSlider(ref right, ref top, MouseCursor.ResizeUpRight, false, dragDot, dragDotActive, color);
            HandleBorderPointSlider(ref left, ref bottom, MouseCursor.ResizeUpRight, false, dragDot, dragDotActive, color);
            HandleBorderPointSlider(ref right, ref bottom, MouseCursor.ResizeUpLeft, false, dragDot, dragDotActive, color);

            if (EditorGUI.EndChangeCheck())
            {
                rect.xMin = left;
                rect.xMax = right;
                rect.yMax = top;
                rect.yMin = bottom;
                ScaleSpriteRect(rect);
                PopulateSpriteFrameInspectorField();
            }
        }

        private void HandleRectSideScalingHandles()
        {
            if (!hasSelected)
                return;

            Rect rect = new Rect(selectedSpriteRect_Rect);

            float left = rect.xMin;
            float right = rect.xMax;
            float top = rect.yMax;
            float bottom = rect.yMin;

            Vector2 screenRectTopLeft = Handles.matrix.MultiplyPoint(new Vector3(rect.xMin, rect.yMin));
            Vector2 screenRectBottomRight = Handles.matrix.MultiplyPoint(new Vector3(rect.xMax, rect.yMax));

            float screenRectWidth = Mathf.Abs(screenRectBottomRight.x - screenRectTopLeft.x);
            float screenRectHeight = Mathf.Abs(screenRectBottomRight.y - screenRectTopLeft.y);

            EditorGUI.BeginChangeCheck();

            left = HandleBorderScaleSlider(left, rect.yMax, screenRectWidth, screenRectHeight, true);
            right = HandleBorderScaleSlider(right, rect.yMax, screenRectWidth, screenRectHeight, true);

            top = HandleBorderScaleSlider(rect.xMin, top, screenRectWidth, screenRectHeight, false);
            bottom = HandleBorderScaleSlider(rect.xMin, bottom, screenRectWidth, screenRectHeight, false);

            if (EditorGUI.EndChangeCheck())
            {
                rect.xMin = left;
                rect.xMax = right;
                rect.yMax = top;
                rect.yMin = bottom;

                ScaleSpriteRect(rect);
                PopulateSpriteFrameInspectorField();
            }
        }

        private void HandleDragging()
        {
            if (hasSelected && !MouseOnTopOfInspector())
            {
                Rect textureBounds = new Rect(0, 0, textureActualWidth, textureActualHeight);
                EditorGUI.BeginChangeCheck();

                Rect oldRect = selectedSpriteRect_Rect;
                Rect newRect = SpriteEditorUtility.ClampedRect(SpriteEditorUtility.RoundedRect(SpriteEditorHandles.SliderRect(oldRect)), textureBounds, true);

                if (EditorGUI.EndChangeCheck())
                {
                    selectedSpriteRect_Rect = newRect;
                    UpdatePositionField(null);
                }
            }
        }

        private void HandleCreate()
        {
            if (!MouseOnTopOfInspector() && !eventSystem.current.alt)
            {
                // Create new rects via dragging in empty space
                EditorGUI.BeginChangeCheck();
                Rect newRect = SpriteEditorHandles.RectCreator(textureActualWidth, textureActualHeight, styles.createRect);
                if (EditorGUI.EndChangeCheck() && newRect.width > 0f && newRect.height > 0f)
                {
                    CreateSprite(newRect);
                    GUIUtility.keyboardControl = 0;
                }
            }
        }

        private void HandleDuplicate()
        {
            IEvent evt = eventSystem.current;
            if ((evt.type == EventType.ValidateCommand || evt.type == EventType.ExecuteCommand)
                && evt.commandName == EventCommandNames.Duplicate)
            {
                if (evt.type == EventType.ExecuteCommand)
                    DuplicateSprite();

                evt.Use();
            }
        }

        private void HandleDelete()
        {
            IEvent evt = eventSystem.current;

            if ((evt.type == EventType.ValidateCommand || evt.type == EventType.ExecuteCommand)
                && (evt.commandName == EventCommandNames.SoftDelete || evt.commandName == EventCommandNames.Delete))
            {
                if (evt.type == EventType.ExecuteCommand && hasSelected)
                    DeleteSprite();

                evt.Use();
            }
        }

        public override void DoPostGUI()
        {
            if (m_CurrentMode != null)
                m_CurrentMode.DoPostGUI();
            else
                base.DoPostGUI();
        }
    }
}
