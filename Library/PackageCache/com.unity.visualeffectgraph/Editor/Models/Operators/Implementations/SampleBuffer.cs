using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX.Operator
{
    [VFXHelpURL("Operator-SampleBuffer")]
    [VFXInfo(name = "Sample Graphics Buffer", category = "Sampling")]
    class SampleBuffer : VFXOperatorDynamicType
    {
        public override IEnumerable<int> staticSlotIndex
        {
            get
            {
                yield return 0;
                yield return 1;
            }
        }

        public override IEnumerable<Type> validTypes
        {
            get
            {
                return VFXLibrary.GetGraphicsBufferType();
            }
        }

        protected override Type defaultValueType => typeof(float);

        public override string name { get { return "Sample Graphics Buffer"; } }

        [VFXSetting, SerializeField, Tooltip("Specifies how Unity handles the sample when the custom index is out the out of bounds of GraphicsBuffer")]
        private VFXOperatorUtility.SequentialAddressingMode mode = VFXOperatorUtility.SequentialAddressingMode.Clamp;

        public class InputProperties
        {
            [Tooltip("Sets the Graphics Buffer to sample from.")]
            public GraphicsBuffer buffer = null;
            [Tooltip("Sets the index of element to sample.")]
            public uint index;
        }

        protected override IEnumerable<VFXPropertyWithValue> outputProperties
        {
            get
            {
                if (m_Type != null)
                    yield return new VFXPropertyWithValue(new VFXProperty(m_Type, "s"));
            }
        }

        public string ComputeSlotPath(VFXSlot slot)
        {
            if (slot.IsMasterSlot())
                return string.Empty;

            var parent = slot.GetParent();
            var name = slot.name;
            while (!parent.IsMasterSlot())
            {
                name = string.Format("{0}.{1}", parent.name, name);
                parent = parent.GetParent();
            }
            return name;
        }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            if (GetNbOutputSlots() == 0)
                return new VFXExpression[] { };

            var outputSlot = GetOutputSlot(0);
            var slots = outputSlot.GetVFXValueTypeSlots();

            var buffer = inputExpression[0];
            var index = inputExpression[1];
            var count = new VFXExpressionBufferCount(buffer);

            index = VFXOperatorUtility.ApplyAddressingMode(index, count, mode);
            var stride = new VFXExpressionBufferStride(buffer);
            var expressions = new List<VFXExpression>();

            var type = (Type)m_Type;
            var usage = new BufferUsage(BufferUsage.Container.StructuredBuffer, type.Name, type);

            var bufferWithUsage = new VFXExpressionBufferWithType(usage, buffer);
            foreach (var slot in slots)
            {
                var path = ComputeSlotPath(slot);
                var current = new VFXExpressionSampleBuffer(slot.valueType, path, bufferWithUsage, index, stride, count);
                expressions.Add(current);
            }
            return expressions.ToArray();
        }
    }
}
