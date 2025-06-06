using UnityEngine;

namespace Unity.VisualScripting
{
    public class ListInspector : Inspector
    {
        public ListInspector(Metadata metadata) : base(metadata)
        {
            adaptor = new MetadataListAdaptor(metadata, this);
        }

        protected MetadataListAdaptor adaptor { get; set; }

        protected override float GetHeight(float width, GUIContent label)
        {
            return adaptor.GetHeight(width, label);
        }

        protected override void OnGUI(Rect position, GUIContent label)
        {
            adaptor.Field(position, label);
        }

        public override float GetAdaptiveWidth()
        {
            return adaptor.GetAdaptiveWidth();
        }
    }

    internal class ProjectAssemblyOptionsListInspector : Inspector
    {
        public ProjectAssemblyOptionsListInspector(Metadata metadata) : base(metadata)
        {
            adaptor = new ProjectAssemblyOptionsListAdaptor(metadata, this);
        }

        ProjectAssemblyOptionsListAdaptor adaptor { get; set; }

        protected override float GetHeight(float width, GUIContent label)
        {
            return adaptor.GetHeight(width, label);
        }

        protected override void OnGUI(Rect position, GUIContent label)
        {
            adaptor.Field(position, label);
        }

        public override float GetAdaptiveWidth()
        {
            return adaptor.GetAdaptiveWidth();
        }
    }
}
