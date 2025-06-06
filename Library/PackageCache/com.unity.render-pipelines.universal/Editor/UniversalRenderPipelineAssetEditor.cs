using UnityEditorInternal;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using Styles = UnityEditor.Rendering.Universal.UniversalRenderPipelineAssetUI.Styles;

namespace UnityEditor.Rendering.Universal
{
    /// <summary>
    /// Editor script for a <c>UniversalRenderPipelineAsset</c> class.
    /// </summary>
    [CustomEditor(typeof(UniversalRenderPipelineAsset)), CanEditMultipleObjects]
    public class UniversalRenderPipelineAssetEditor : Editor
    {
        SerializedProperty m_RendererDataProp;
        SerializedProperty m_DefaultRendererProp;

        internal ReorderableList rendererList => m_RendererDataList;
        ReorderableList m_RendererDataList;

        private SerializedUniversalRenderPipelineAsset m_SerializedURPAsset;

        /// <inheritdoc/>
        public override void OnInspectorGUI()
        {
            m_SerializedURPAsset.Update();
            UniversalRenderPipelineAssetUI.Inspector.Draw(m_SerializedURPAsset, this);
            m_SerializedURPAsset.Apply();
        }

        void OnEnable()
        {
            m_SerializedURPAsset = new SerializedUniversalRenderPipelineAsset(serializedObject);
            CreateRendererReorderableList();
        }

        void CreateRendererReorderableList()
        {
            m_RendererDataProp = serializedObject.FindProperty("m_RendererDataList");
            m_DefaultRendererProp = serializedObject.FindProperty("m_DefaultRendererIndex");
            m_RendererDataList = new ReorderableList(serializedObject, m_RendererDataProp, true, true, true, true)
            {
                drawElementCallback = OnDrawElement,
                drawHeaderCallback = (Rect rect) => EditorGUI.LabelField(rect, Styles.rendererHeaderText),
                onCanRemoveCallback = reorderableList => reorderableList.count > 1,
                onRemoveCallback = OnRemoveElement,
                onReorderCallbackWithDetails = (reorderableList, index, newIndex) => UpdateDefaultRendererValue(index, newIndex) // Need to update the default renderer index
            };
        }

        void OnRemoveElement(ReorderableList reorderableList)
        {
            bool shouldUpdateIndex = false;
            // Checking so that the user is not deleting  the default renderer
            if (reorderableList.index != m_DefaultRendererProp.intValue)
            {
                // Need to add the undo to the removal of our assets here, for it to work properly.
                Undo.RecordObject(target, $"Deleting renderer at index {reorderableList.index}");

                shouldUpdateIndex = true;
                m_RendererDataProp.DeleteArrayElementAtIndex(reorderableList.index);
            }
            else
            {
                EditorUtility.DisplayDialog(Styles.rendererListDefaultMessage.text, Styles.rendererListDefaultMessage.tooltip, "Close");
            }

            if (shouldUpdateIndex)
            {
                UpdateDefaultRendererValue(reorderableList.index);
            }

            EditorUtility.SetDirty(target);
        }

        void OnDrawElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            rect.y += 2;
            var indexRect = new Rect(rect.x, rect.y, 14, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(indexRect, index.ToString());

            using (var changeCheck = new EditorGUI.ChangeCheckScope())
            {
                var objectFieldWidth = rect.width - 110;
                var objRect = new Rect(rect.x + indexRect.width, rect.y, objectFieldWidth, EditorGUIUtility.singleLineHeight);
                EditorGUI.ObjectField(objRect, m_RendererDataProp.GetArrayElementAtIndex(index), GUIContent.none);
                if (changeCheck.changed)
                    EditorUtility.SetDirty(target);
            }

            var isDefaultRenderer = index == m_DefaultRendererProp.intValue;
            using (new EditorGUI.DisabledScope(isDefaultRenderer))
            {
                var defaultButtonX = rect.width - 51;
                var defaultButtonRect = new Rect(defaultButtonX, rect.y, 86, EditorGUIUtility.singleLineHeight);
                if (GUI.Button(defaultButtonRect, !GUI.enabled ? Styles.rendererDefaultText : Styles.rendererSetDefaultText))
                {
                    m_DefaultRendererProp.intValue = index;
                    EditorUtility.SetDirty(target);
                }
            }

            // If object selector chose an object, assign it to the correct ScriptableRendererData slot.
            if (Event.current.commandName == "ObjectSelectorUpdated" && EditorGUIUtility.GetObjectPickerControlID() == index)
            {
                m_RendererDataProp.GetArrayElementAtIndex(index).objectReferenceValue = EditorGUIUtility.GetObjectPickerObject();
            }
        }

        void UpdateDefaultRendererValue(int index)
        {
            // If the index that is being removed is lower than the default renderer value,
            // the default prop value needs to be one lower.
            if (index < m_DefaultRendererProp.intValue)
            {
                m_DefaultRendererProp.intValue--;
            }
        }

        void UpdateDefaultRendererValue(int prevIndex, int newIndex)
        {
            // If we are moving the index that is the same as the default renderer we need to update that
            if (prevIndex == m_DefaultRendererProp.intValue)
            {
                m_DefaultRendererProp.intValue = newIndex;
            }
            // If newIndex is the same as default
            // then we need to know if newIndex is above or below the default index
            else if (newIndex == m_DefaultRendererProp.intValue)
            {
                m_DefaultRendererProp.intValue += prevIndex > newIndex ? 1 : -1;
            }
            // If the old index is lower than default renderer and
            // the new index is higher then we need to move the default renderer index one lower
            else if (prevIndex < m_DefaultRendererProp.intValue && newIndex > m_DefaultRendererProp.intValue)
            {
                m_DefaultRendererProp.intValue--;
            }
            else if (newIndex < m_DefaultRendererProp.intValue && prevIndex > m_DefaultRendererProp.intValue)
            {
                m_DefaultRendererProp.intValue++;
            }
        }
    }
}
