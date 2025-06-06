using UnityEngine;

namespace UnityEditor.VFX.Operator
{
    [VFXHelpURL("Operator-Modulo")]
    [VFXInfo(category = "Math/Arithmetic", synonyms = new[] { "%" })]
    class Modulo : VFXOperatorNumericUniform
    {
        public class InputProperties
        {
            [Tooltip("The numerator operand.")]
            public float a = 1.0f;
            [Tooltip("The denominator operand.")]
            public float b = 1.0f;
        }

        protected override sealed string operatorName { get { return "Modulo"; } }

        protected override VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            return new[] { VFXOperatorUtility.Modulo(inputExpression[0], inputExpression[1]) };
        }
    }
}
