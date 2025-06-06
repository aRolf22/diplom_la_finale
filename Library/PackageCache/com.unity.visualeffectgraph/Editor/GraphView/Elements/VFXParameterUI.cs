using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Profiling;

namespace UnityEditor.VFX.UI
{
    class VFXOutputParameterDataAnchor : VFXOutputDataAnchor
    {
        public static new VFXOutputParameterDataAnchor Create(VFXDataAnchorController controller, VFXNodeUI node)
        {
            var anchor = new VFXOutputParameterDataAnchor(controller.orientation, controller.direction, controller.portType, node);

            anchor.m_EdgeConnector = new VFXEdgeConnector(anchor);
            anchor.controller = controller;
            anchor.AddManipulator(anchor.m_EdgeConnector);
            return anchor;
        }

        protected VFXOutputParameterDataAnchor(Orientation anchorOrientation, Direction anchorDirection, Type type, VFXNodeUI node) : base(anchorOrientation, anchorDirection, type, node)
        {
        }

        public override bool ContainsPoint(Vector2 localPoint)
        {
            return base.ContainsPoint(localPoint) && !m_ConnectorText.ContainsPoint(this.ChangeCoordinatesTo(m_ConnectorText, localPoint));
        }
    }

    class VFXInputParameterDataAnchor : VFXDataAnchor
    {
        public static new VFXInputParameterDataAnchor Create(VFXDataAnchorController controller, VFXNodeUI node)
        {
            var anchor = new VFXInputParameterDataAnchor(controller.orientation, controller.direction, controller.portType, node);

            anchor.m_EdgeConnector = new EdgeConnector<VFXDataEdge>(anchor);
            anchor.controller = controller;
            anchor.AddManipulator(anchor.m_EdgeConnector);
            return anchor;
        }

        protected VFXInputParameterDataAnchor(Orientation anchorOrientation, Direction anchorDirection, Type type, VFXNodeUI node) : base(anchorOrientation, anchorDirection, type, node)
        {
        }

        public override bool ContainsPoint(Vector2 localPoint)
        {
            return base.ContainsPoint(localPoint) && !m_ConnectorText.ContainsPoint(this.ChangeCoordinatesTo(m_ConnectorText, localPoint));
        }
    }


    static class UXMLHelper
    {
        const string folderName = VisualEffectAssetEditorUtility.editorResourcesFolder;

        public static string GetUXMLPath(string name)
        {
            string path = null;
            if (s_Cache.TryGetValue(name, out path))
            {
                return path;
            }

            return GetUXMLPathRecursive("Assets", name);
        }

        static Dictionary<string, string> s_Cache = new Dictionary<string, string>();

        static string GetUXMLPathRecursive(string path, string name)
        {
            Profiler.BeginSample("UXMLHelper.GetUXMLPathRecursive");
            string localFileName = path + "/" + folderName + "/" + name;
            if (System.IO.File.Exists(localFileName))
            {
                Profiler.EndSample();
                s_Cache[name] = localFileName;
                return localFileName;
            }

            foreach (var dir in System.IO.Directory.GetDirectories(path))
            {
                if (dir.Length <= folderName.Length || !dir.EndsWith(folderName) || !"/\\".Contains(dir[dir.Length - folderName.Length - 1]))
                {
                    string result = GetUXMLPathRecursive(dir, name);
                    if (result != null)
                    {
                        Profiler.EndSample();
                        return result;
                    }
                }
            }

            Profiler.EndSample();
            return null;
        }
    }


    class VFXParameterUI : VFXNodeUI
    {
        public VFXParameterUI() : base("uxml/VFXParameter")
        {
            RemoveFromClassList("VFXNodeUI");
            styleSheets.Add(VFXView.LoadStyleSheet("VFXParameter"));
            styleSheets.Add(EditorGUIUtility.Load("StyleSheets/GraphView/Node.uss") as StyleSheet);

            RegisterCallback<MouseEnterEvent>(OnMouseHover);
            RegisterCallback<MouseLeaveEvent>(OnMouseHover);

            m_ExposedIcon = this.Q<Image>("exposed-icon");
            this.AddManipulator(new SuperCollapser());

            m_Label = this.Q<Label>("title-label");
        }

        Label m_Label;

        public new VFXParameterNodeController controller => base.controller as VFXParameterNodeController;

        protected override VFXDataAnchor InstantiateDataAnchor(VFXDataAnchorController controller, VFXNodeUI node)
        {
            if (controller.direction == Direction.Input)
                return VFXInputParameterDataAnchor.Create(controller, node);
            else
                return VFXOutputParameterDataAnchor.Create(controller, node);
        }

        Image m_ExposedIcon;

        protected override void UpdateTitleUI()
        {
            m_Label.text = controller.title;
        }

        protected override void SelfChange()
        {
            base.SelfChange();

            if (m_ExposedIcon != null)
                m_ExposedIcon.visible = controller.parentController.exposed;

            if (controller.parentController.exposed)
            {
                AddToClassList("exposed");
            }
            else
            {
                RemoveFromClassList("exposed");
            }

            if (controller.parentController.isOutput)
            {
                AddToClassList("output");
            }
            else
            {
                RemoveFromClassList("output");
            }

            m_Label.parent.tooltip = controller.parentController.model.tooltip;
        }

        public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
        {
            if (evt.target == this && controller != null)
            {
                evt.menu.AppendAction("Convert to Inline", OnConvertToInline, e => DropdownMenuAction.Status.Normal);
                evt.menu.AppendSeparator();
            }
        }

        void OnConvertToInline(DropdownMenuAction evt)
        {
            controller.ConvertToInline();
        }

        void OnMouseHover(EventBase evt)
        {
            Profiler.BeginSample("VFXParameterUI.OnMouseOver");
            try
            {
                var view = GetFirstAncestorOfType<VFXView>();
                var blackboard = view?.blackboard;
                var row = blackboard?.GetRowFromController(controller.parentController);
                if (row == null)
                    return;

                if (evt.eventTypeId == MouseEnterEvent.TypeId())
                    row.AddToClassList("hovered");
                else
                    row.RemoveFromClassList("hovered");
            }
            finally
            {
                Profiler.EndSample();
            }
        }
    }
}
