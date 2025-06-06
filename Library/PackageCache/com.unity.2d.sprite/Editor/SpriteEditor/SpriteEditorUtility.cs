using System;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEvent = UnityEngine.Event;

namespace UnityEditor.U2D.Sprites
{
    static internal class SpriteEditorUtility
    {
        public static Vector2 GetPivotValue(SpriteAlignment alignment, Vector2 customOffset)
        {
            switch (alignment)
            {
                case SpriteAlignment.BottomLeft:
                    return new Vector2(0f, 0f);
                case SpriteAlignment.BottomCenter:
                    return new Vector2(0.5f, 0f);
                case SpriteAlignment.BottomRight:
                    return new Vector2(1f, 0f);

                case SpriteAlignment.LeftCenter:
                    return new Vector2(0f, 0.5f);
                case SpriteAlignment.Center:
                    return new Vector2(0.5f, 0.5f);
                case SpriteAlignment.RightCenter:
                    return new Vector2(1f, 0.5f);

                case SpriteAlignment.TopLeft:
                    return new Vector2(0f, 1f);
                case SpriteAlignment.TopCenter:
                    return new Vector2(0.5f, 1f);
                case SpriteAlignment.TopRight:
                    return new Vector2(1f, 1f);

                case SpriteAlignment.Custom:
                    return customOffset;
            }
            return Vector2.zero;
        }

        public static Rect RoundedRect(Rect rect)
        {
            return new Rect(
                Mathf.RoundToInt(rect.xMin),
                Mathf.RoundToInt(rect.yMin),
                Mathf.RoundToInt(rect.width),
                Mathf.RoundToInt(rect.height)
            );
        }

        public static Rect RoundToInt(Rect r)
        {
            r.xMin = Mathf.RoundToInt(r.xMin);
            r.yMin = Mathf.RoundToInt(r.yMin);
            r.xMax = Mathf.RoundToInt(r.xMax);
            r.yMax = Mathf.RoundToInt(r.yMax);

            return r;
        }

        public static Rect ClampedRect(Rect rect, Rect clamp, bool maintainSize)
        {
            Rect r = new Rect(rect);

            if (maintainSize)
            {
                Vector2 center = rect.center;
                if (center.x + Mathf.Abs(rect.width) * .5f > clamp.xMax)
                    center.x = clamp.xMax - rect.width * .5f;
                if (center.x - Mathf.Abs(rect.width) * .5f < clamp.xMin)
                    center.x = clamp.xMin + rect.width * .5f;
                if (center.y + Mathf.Abs(rect.height) * .5f > clamp.yMax)
                    center.y = clamp.yMax - rect.height * .5f;
                if (center.y - Mathf.Abs(rect.height) * .5f < clamp.yMin)
                    center.y = clamp.yMin + rect.height * .5f;
                r.center = center;
            }
            else
            {
                if (r.width > 0f)
                {
                    r.xMin = Mathf.Max(rect.xMin, clamp.xMin);
                    r.xMax = Mathf.Min(rect.xMax, clamp.xMax);
                }
                else
                {
                    r.xMin = Mathf.Min(rect.xMin, clamp.xMax);
                    r.xMax = Mathf.Max(rect.xMax, clamp.xMin);
                }
                if (r.height > 0f)
                {
                    r.yMin = Mathf.Max(rect.yMin, clamp.yMin);
                    r.yMax = Mathf.Min(rect.yMax, clamp.yMax);
                }
                else
                {
                    r.yMin = Mathf.Min(rect.yMin, clamp.yMax);
                    r.yMax = Mathf.Max(rect.yMax, clamp.yMin);
                }
            }

            r.width = Mathf.Abs(r.width);
            r.height = Mathf.Abs(r.height);

            return r;
        }

        public static void DrawBox(Rect position)
        {
            var points0 = new Vector3(position.xMin, position.yMin, 0f);
            var points1 = new Vector3(position.xMax, position.yMin, 0f);
            var points2 = new Vector3(position.xMax, position.yMax, 0f);
            var points3 = new Vector3(position.xMin, position.yMax, 0f);

            DrawLine(points0, points1);
            DrawLine(points1, points2);
            DrawLine(points2, points3);
            DrawLine(points3, points0);
        }

        public static void DrawLine(Vector3 p1, Vector3 p2)
        {
            GL.Vertex(p1);
            GL.Vertex(p2);
        }

        public static void BeginLines(Color color)
        {
            Assert.IsTrue(UnityEvent.current.type == EventType.Repaint);
            HandleUtility.ApplyWireMaterial();
            GL.PushMatrix();
            GL.MultMatrix(Handles.matrix);
            GL.Begin(GL.LINES);
            GL.Color(color);
        }

        public static void EndLines()
        {
            Assert.IsTrue(UnityEvent.current.type == EventType.Repaint);
            GL.End();
            GL.PopMatrix();
        }

        public static void FourIntFields(Vector2 rectSize, GUIContent label, GUIContent labelX, GUIContent labelY, GUIContent labelZ, GUIContent labelW, ref int x, ref int y, ref int z, ref int w)
        {
            Rect rect = GUILayoutUtility.GetRect(rectSize.x, rectSize.y);

            Rect labelRect = rect;
            labelRect.width = EditorGUIUtility.labelWidth;
            labelRect.height = EditorGUI.kSingleLineHeight;

            GUI.Label(labelRect, label);

            Rect fieldRect = rect;
            fieldRect.width -= EditorGUIUtility.labelWidth;
            fieldRect.height = EditorGUI.kSingleLineHeight;
            fieldRect.x += EditorGUIUtility.labelWidth;
            fieldRect.width /= 2;
            fieldRect.width -= EditorGUI.kSpacingSubLabel;

            float oldLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = EditorGUI.kMiniLabelW;

            GUI.SetNextControlName("FourIntFields_x");
            x = EditorGUI.IntField(fieldRect, labelX, x);
            fieldRect.x += fieldRect.width + EditorGUI.kSpacing;
            GUI.SetNextControlName("FourIntFields_y");
            y = EditorGUI.IntField(fieldRect, labelY, y);
            fieldRect.y += EditorGUI.kSingleLineHeight + EditorGUI.kVerticalSpacingMultiField;
            fieldRect.x -= fieldRect.width + EditorGUI.kSpacing;
            GUI.SetNextControlName("FourIntFields_z");
            z = EditorGUI.IntField(fieldRect, labelZ, z);
            fieldRect.x += fieldRect.width + EditorGUI.kSpacing;
            GUI.SetNextControlName("FourIntFields_w");
            w = EditorGUI.IntField(fieldRect, labelW, w);

            EditorGUIUtility.labelWidth = oldLabelWidth;
        }

        public static void DetermineGridCellSizeWithCellCount(int width, int height, Vector2 offset, Vector2 padding, Vector2 cellCount, out Vector2 cellSize)
        {
            cellSize.x = (width - offset.x -  (padding.x * Math.Max(0, cellCount.x - 1))) / cellCount.x;
            cellSize.y = (height - offset.y - (padding.y * Math.Max(0, cellCount.y - 1))) / cellCount.y;

            cellSize.x = Mathf.Floor(cellSize.x);
            cellSize.y = Mathf.Floor(cellSize.y);

            cellSize.x = Mathf.Clamp(cellSize.x, 1, width);
            cellSize.y = Mathf.Clamp(cellSize.y, 1, height);
        }
    }
}
