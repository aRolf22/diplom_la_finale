using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXHelpURL("Operator-BufferCount")]
    [VFXInfo(name = "Graphics Buffer Count", category = "Sampling")]
    class BufferCount : VFXOperator
    {
        public override string name { get { return "Graphics Buffer Count"; } }

        public class InputProperties
        {
            [Tooltip("Sets the Graphics Buffer to retrieve count.")]
            public GraphicsBuffer buffer = null;
        }

        public class OutputProperties
        {
            [Tooltip("The Graphics Buffer count.")]
            public uint count;
        }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            var count = new VFXExpressionBufferCount(inputExpression[0]);
            return new VFXExpression[] { count };
        }
    }
}
