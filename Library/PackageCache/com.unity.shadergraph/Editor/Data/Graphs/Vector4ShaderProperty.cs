using System;
using UnityEditor.Graphing;
using UnityEngine;

namespace UnityEditor.ShaderGraph.Internal
{
    [Serializable]
    [FormerName("UnityEditor.ShaderGraph.Vector4ShaderProperty")]
    [BlackboardInputInfo(4)]
    public sealed class Vector4ShaderProperty : VectorShaderProperty
    {
        internal Vector4ShaderProperty()
        {
            displayName = "Vector4";
        }

        internal override int vectorDimension => 4;

        public override PropertyType propertyType => PropertyType.Vector4;

        internal override AbstractMaterialNode ToConcreteNode()
        {
            var node = new Vector4Node();
            node.FindInputSlot<Vector1MaterialSlot>(Vector4Node.InputSlotXId).value = value.x;
            node.FindInputSlot<Vector1MaterialSlot>(Vector4Node.InputSlotYId).value = value.y;
            node.FindInputSlot<Vector1MaterialSlot>(Vector4Node.InputSlotZId).value = value.z;
            node.FindInputSlot<Vector1MaterialSlot>(Vector4Node.InputSlotWId).value = value.w;
            return node;
        }

        internal override PreviewProperty GetPreviewMaterialProperty()
        {
            return new PreviewProperty(propertyType)
            {
                name = referenceName,
                vector4Value = value
            };
        }

        internal override ShaderInput Copy()
        {
            return new Vector4ShaderProperty()
            {
                displayName = displayName,
                value = value,
            };
        }

        internal override void ForeachHLSLProperty(Action<HLSLProperty> action)
        {
            HLSLDeclaration decl = GetDefaultHLSLDeclaration();
            action(new HLSLProperty(HLSLType._float4, referenceName, decl, concretePrecision));
        }

        public override int latestVersion => 1;
        public override void OnAfterDeserialize(string json)
        {
            if (sgVersion == 0)
            {
                LegacyShaderPropertyData.UpgradeToHLSLDeclarationOverride(json, this);
                ChangeVersion(1);
            }
        }
    }
}
