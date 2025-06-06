using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    abstract class VFXOperator : VFXSlotContainerModel<VFXModel, VFXModel>
    {
        protected VFXOperator()
        {
            m_UICollapsed = false;
        }

        private static void GetSlotPredicateRecursive(List<VFXSlot> result, IEnumerable<VFXSlot> slots, Func<VFXSlot, bool> fnTest)
        {
            foreach (var s in slots)
            {
                if (fnTest(s))
                {
                    result.Add(s);
                }
                else
                {
                    GetSlotPredicateRecursive(result, s.children, fnTest);
                }
            }
        }

        protected override void OnInvalidate(VFXModel model, InvalidationCause cause)
        {
            var outputSlotSpaceable = outputSlots.Where(o => o.spaceable);
            bool needUpdateInputSpaceable = false;
            foreach (var output in outputSlotSpaceable)
            {
                var currentSpaceForSlot = GetOutputSpaceFromSlot(output);
                if (currentSpaceForSlot != output.space)
                {
                    output.space = currentSpaceForSlot;
                    needUpdateInputSpaceable = true;
                }
            }

            //If one of output slot has changed its space, expression tree for inputs,
            //and more generally, current operation expression graph is invalid.
            //=> Trigger invalidation on input is enough to recompute the graph from this operator
            if (needUpdateInputSpaceable)
            {
                var inputSlotSpaceable = inputSlots.Where(o => o.spaceable);
                foreach (var input in inputSlotSpaceable)
                {
                    input.Invalidate(InvalidationCause.kSpaceChanged);
                }
            }

            bool isInputSlot = model is VFXSlot && ((VFXSlot)model).direction == VFXSlot.Direction.kInput;

            if (cause == InvalidationCause.kParamChanged ||
                cause == InvalidationCause.kExpressionValueInvalidated)
            {
                if (isInputSlot)
                {
                    foreach (var slot in outputSlots)
                        slot.Invalidate(InvalidationCause.kExpressionValueInvalidated);
                }
            }

            base.OnInvalidate(model, cause);

            if (cause == InvalidationCause.kSettingChanged ||
    (isInputSlot && (cause == InvalidationCause.kExpressionInvalidated || cause == InvalidationCause.kConnectionChanged)))
            {
                MarkOutputExpressionsAsOutOfDate();
                foreach (var slot in outputSlots) // invalidate expressions on output slots
                    slot.InvalidateExpressionTree();
            }
        }

        public override VFXSpace GetOutputSpaceFromSlot(VFXSlot outputSlot)
        {
            //Most common case : space is the maximal output space from input slot
            return inputSlots.Any()
                ? inputSlots.Select(o => o.space).Max()
                : VFXSpace.None;
        }

        protected override sealed void UpdateOutputExpressions()
        {
            var outputSlotWithExpression = new List<VFXSlot>();
            var inputSlotWithExpression = new List<VFXSlot>();
            GetSlotPredicateRecursive(outputSlotWithExpression, outputSlots, s => s.DefaultExpr != null);
            GetSlotPredicateRecursive(inputSlotWithExpression, inputSlots, s => s.GetExpression() != null);

            IEnumerable<VFXExpression> inputExpressions = inputSlotWithExpression.Select(o => o.GetExpression());
            inputExpressions = ApplyPatchInputExpression(inputExpressions);

            var outputExpressions = BuildExpression(inputExpressions.ToArray());
            if (outputExpressions.Length != outputSlotWithExpression.Count)
                throw new Exception(string.Format("Numbers of output expressions ({0}) does not match number of output (with expression)s slots ({1})", outputExpressions.Length, outputSlotWithExpression.Count));

            for (int i = 0; i < outputSlotWithExpression.Count; ++i)
            {
                var slot = outputSlotWithExpression[i];
                slot.SetExpression(outputExpressions[i]);
            }
        }

        protected virtual IEnumerable<VFXExpression> ApplyPatchInputExpression(IEnumerable<VFXExpression> inputExpression)
        {
            return inputExpression;
        }

        protected abstract VFXExpression[] BuildExpression(VFXExpression[] inputExpression);
    }
}
