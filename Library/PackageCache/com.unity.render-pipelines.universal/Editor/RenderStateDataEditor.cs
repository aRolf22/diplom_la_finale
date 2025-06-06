using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal
{
    [CustomPropertyDrawer(typeof(StencilStateData), true)]
    internal class StencilStateDataDrawer : PropertyDrawer
    {
        class Styles
        {
            public static readonly GUIContent overrideStencil =
                EditorGUIUtility.TrTextContent("Stencil", "Process and override the Stencil buffer values.");

            public static readonly GUIContent stencilValue = EditorGUIUtility.TrTextContent("Value",
                "For each pixel, the Compare function compares this value with the value in the Stencil buffer. The function writes this value to the buffer if the Pass property is set to Replace.");

            public static readonly GUIContent stencilFunction = EditorGUIUtility.TrTextContent("Compare Function",
                "For each pixel, Unity uses this function to compare the value in the Value property with the value in the Stencil buffer.");

            public static readonly GUIContent stencilPass =
                EditorGUIUtility.TrTextContent("Pass", "What happens to the stencil value when passing.");

            public static readonly GUIContent stencilFail =
                EditorGUIUtility.TrTextContent("Fail", "What happens to the stencil value when failing.");

            public static readonly GUIContent stencilZFail =
                EditorGUIUtility.TrTextContent("Z Fail", "What happens to the stencil value when failing Z testing.");
        }

        //Stencil rendering
        private const int stencilBits = 4;
        private const int minStencilValue = 0;
        private const int maxStencilValue = (1 << stencilBits) - 1;

        //Stencil props
        private SerializedProperty m_OverrideStencil;
        private SerializedProperty m_StencilIndex;
        private SerializedProperty m_StencilFunction;
        private SerializedProperty m_StencilPass;
        private SerializedProperty m_StencilFail;
        private SerializedProperty m_StencilZFail;
        private List<SerializedObject> m_properties = new List<SerializedObject>();
        void Init(SerializedProperty property)
        {
            //Stencil
            m_OverrideStencil = property.FindPropertyRelative("overrideStencilState");
            m_StencilIndex = property.FindPropertyRelative("stencilReference");
            m_StencilFunction = property.FindPropertyRelative("stencilCompareFunction");
            m_StencilPass = property.FindPropertyRelative("passOperation");
            m_StencilFail = property.FindPropertyRelative("failOperation");
            m_StencilZFail = property.FindPropertyRelative("zFailOperation");

            m_properties.Add(property.serializedObject);
        }

        public override void OnGUI(Rect rect, SerializedProperty property, GUIContent label)
        {
            if (!m_properties.Contains(property.serializedObject))
                Init(property);

            rect.height = EditorGUIUtility.singleLineHeight;

            EditorGUI.PropertyField(rect, m_OverrideStencil, Styles.overrideStencil);
            if (m_OverrideStencil.boolValue)
            {
                EditorGUI.indentLevel++;
                rect.y += EditorUtils.Styles.defaultLineSpace;
                //Stencil value
                EditorGUI.BeginChangeCheck();
                var stencilVal = m_StencilIndex.intValue;
                stencilVal = EditorGUI.IntSlider(rect, Styles.stencilValue, stencilVal, minStencilValue, maxStencilValue);
                if (EditorGUI.EndChangeCheck())
                    m_StencilIndex.intValue = stencilVal;
                rect.y += EditorUtils.Styles.defaultLineSpace;
                //Stencil compare options
                EditorGUI.PropertyField(rect, m_StencilFunction, Styles.stencilFunction);
                rect.y += EditorUtils.Styles.defaultLineSpace;
                EditorGUI.indentLevel++;
                EditorGUI.PropertyField(rect, m_StencilPass, Styles.stencilPass);
                rect.y += EditorUtils.Styles.defaultLineSpace;
                EditorGUI.PropertyField(rect, m_StencilFail, Styles.stencilFail);
                rect.y += EditorUtils.Styles.defaultLineSpace;
                EditorGUI.indentLevel--;
                //Stencil compare options
                EditorGUI.PropertyField(rect, m_StencilZFail, Styles.stencilZFail);
                EditorGUI.indentLevel--;
            }
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            if (m_properties.Contains(property.serializedObject))
            {
                if (m_OverrideStencil != null && m_OverrideStencil.boolValue)
                    return EditorUtils.Styles.defaultLineSpace * 6;
            }
            return EditorUtils.Styles.defaultLineSpace * 1;
        }
    }
}
