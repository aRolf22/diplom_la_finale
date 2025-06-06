namespace UnityEditor.VFX.Operator
{
    [VFXHelpURL("Operator-Multiply")]
    [VFXInfo(category = "Math/Arithmetic")]
    class Multiply : VFXOperatorNumericCascadedUnified
    {
        protected override sealed string operatorName { get { return "Multiply"; } }

        protected override sealed double defaultValueDouble { get { return 1.0; } }

        protected override sealed VFXExpression ComposeExpression(VFXExpression a, VFXExpression b)
        {
            return a * b;
        }
    }
}
