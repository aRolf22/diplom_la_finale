namespace UnityEditor.VFX.Operator
{
    [VFXHelpURL("Operator-Negate")]
    [VFXInfo(name = "Negate (-x)", category = "Math/Arithmetic", synonyms = new []{ "opposite" })]
    class Negate : VFXOperatorNumericUniform
    {
        public class InputProperties
        {
            public float x = 0.0f;
        }

        protected override sealed string operatorName { get { return "Negate (-x)"; } }

        protected override sealed VFXExpression[] BuildExpression(VFXExpression[] inputExpression)
        {
            var input = inputExpression[0];
            var minusOne = VFXOperatorUtility.MinusOneExpression[input.valueType];
            return new[] { minusOne * input };
        }

        protected sealed override ValidTypeRule typeFilter
        {
            get
            {
                return ValidTypeRule.allowEverythingExceptInteger;
            }
        }
    }
}
