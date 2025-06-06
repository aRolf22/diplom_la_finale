using System;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    static class MatrixExtension
    {
        public static bool IsMatrix3x3Identity(this Matrix4x4 matrix)
        {
            return
                Mathf.Approximately(matrix.m00, 1f) && Mathf.Approximately(matrix.m10, 0f) && Mathf.Approximately(matrix.m20, 0f) &&
                Mathf.Approximately(matrix.m01, 0f) && Mathf.Approximately(matrix.m11, 1f) && Mathf.Approximately(matrix.m21, 0f) &&
                Mathf.Approximately(matrix.m02, 0f) && Mathf.Approximately(matrix.m12, 0f) && Mathf.Approximately(matrix.m22, 1f);
        }
    }

    class VFXExpressionTRSToMatrix : VFXExpression
    {
        public VFXExpressionTRSToMatrix() : this(new VFXExpression[] { VFXValue<Vector3>.Default, VFXValue<Vector3>.Default, VFXValue<Vector3>.Default }
        )
        {
        }

        public VFXExpressionTRSToMatrix(params VFXExpression[] parents) : base(VFXExpression.Flags.None, parents)
        {
        }

        public override VFXExpressionOperation operation
        {
            get
            {
                return VFXExpressionOperation.TRSToMatrix;
            }
        }

        sealed protected override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            var posReduce = constParents[0];
            var rotReduce = constParents[1];
            var scaleReduce = constParents[2];

            var pos = posReduce.Get<Vector3>();
            var rot = rotReduce.Get<Vector3>();
            var scale = scaleReduce.Get<Vector3>();
            var quat = Quaternion.Euler(rot);

            Matrix4x4 matrix = new Matrix4x4();
            matrix.SetTRS(pos, quat, scale);

            return VFXValue.Constant(matrix);
        }

        public override string GetCodeString(string[] parents)
        {
            return string.Format("GetTRSMatrix({0}, {1}, {2})", parents[0], parents[1], parents[2]);
        }
    }

    class VFXExpressionInverseMatrix : VFXExpression
    {
        public VFXExpressionInverseMatrix() : this(VFXValue<Matrix4x4>.Default)
        { }

        public VFXExpressionInverseMatrix(VFXExpression parent) : base(VFXExpression.Flags.InvalidOnGPU, parent)
        { }

        sealed public override VFXExpressionOperation operation
        {
            get
            {
                return VFXExpressionOperation.InverseMatrix;
            }
        }

        sealed protected override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            var matrix = constParents[0].Get<Matrix4x4>();
            return VFXValue.Constant(matrix.inverse);
        }
    }

    class VFXExpressionTransposeMatrix : VFXExpression
    {
        public VFXExpressionTransposeMatrix() : this(VFXValue<Matrix4x4>.Default)
        { }

        public VFXExpressionTransposeMatrix(VFXExpression parent) : base(Flags.None, parent)
        { }

        public sealed override VFXExpressionOperation operation
        {
            get
            {
                return VFXExpressionOperation.TransposeMatrix;
            }
        }

        sealed protected override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            var matrix = constParents[0].Get<Matrix4x4>();
            return VFXValue.Constant(matrix.transpose);
        }

        public override string GetCodeString(string[] parents)
        {
            return string.Format("transpose({0})", parents[0]);
        }
    }

    class VFXExpressionInverseTRSMatrix : VFXExpression
    {
        public VFXExpressionInverseTRSMatrix() : this(VFXValue<Matrix4x4>.Default)
        { }

        public VFXExpressionInverseTRSMatrix(VFXExpression parent) : base(VFXExpression.Flags.None, parent)
        { }

        sealed public override VFXExpressionOperation operation
        {
            get
            {
                return VFXExpressionOperation.InverseTRSMatrix;
            }
        }

        // Invert 3D transformation matrix (not perspective). Adapted from graphics gems 2.
        // Inverts upper left by calculating its determinant and multiplying it to the symmetric
        // adjust matrix of each element. Finally deals with the translation by transforming the
        // original translation using by the calculated inverse.
        //https://github.com/erich666/GraphicsGems/blob/master/gemsii/inverse.c
        static bool inputvertMatrix4x4_General3D(Matrix4x4 input, ref Matrix4x4 output)
        {
            return Matrix4x4.Inverse3DAffine(input, ref output);
        }

        sealed protected override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            var matrix = constParents[0].Get<Matrix4x4>();

            var result = Matrix4x4.identity;
            inputvertMatrix4x4_General3D(matrix, ref result);
            return VFXValue.Constant(result);
        }

        public override string GetCodeString(string[] parents)
        {
            return string.Format("VFXInverseTRSMatrix({0})", parents[0]);
        }
    }

    class VFXExpressionExtractPositionFromMatrix : VFXExpression
    {
        public VFXExpressionExtractPositionFromMatrix() : this(VFXValue<Matrix4x4>.Default)
        {
        }

        public VFXExpressionExtractPositionFromMatrix(VFXExpression parent) : base(VFXExpression.Flags.None, new VFXExpression[] { parent })
        {
        }

        public override VFXExpressionOperation operation
        {
            get
            {
                return VFXExpressionOperation.ExtractPositionFromMatrix;
            }
        }

        protected sealed override VFXExpression Reduce(VFXExpression[] reducedParents)
        {
            var parent = reducedParents[0];
            if (parent is VFXExpressionTRSToMatrix)
                return parent.parents[0];

            return base.Reduce(reducedParents);
        }

        sealed protected override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            var matrixReduce = constParents[0];
            var matrix = matrixReduce.Get<Matrix4x4>();

            return VFXValue.Constant<Vector3>(matrix.GetColumn(3));
        }

        public override string GetCodeString(string[] parents)
        {
            return string.Format("{0}._14_24_34", parents[0]);
        }
    }

    class VFXExpressionExtractAnglesFromMatrix : VFXExpression
    {
        public VFXExpressionExtractAnglesFromMatrix() : this(VFXValue<Matrix4x4>.Default)
        {
        }

        public VFXExpressionExtractAnglesFromMatrix(VFXExpression parent) : base(VFXExpression.Flags.InvalidOnGPU, new VFXExpression[] { parent })
        {
        }

        public override VFXExpressionOperation operation
        {
            get
            {
                return VFXExpressionOperation.ExtractAnglesFromMatrix;
            }
        }

        protected sealed override VFXExpression Reduce(VFXExpression[] reducedParents)
        {
            var parent = reducedParents[0];
            if (parent is VFXExpressionTRSToMatrix)
                return parent.parents[1];

            return base.Reduce(reducedParents);
        }

        sealed protected override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            var matrixReduce = constParents[0];
            var matrix = matrixReduce.Get<Matrix4x4>();
            matrix.SetRow(3, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));
            return VFXValue.Constant(matrix.rotation.eulerAngles);
        }
    }

    class VFXExpressionExtractScaleFromMatrix : VFXExpression
    {
        public VFXExpressionExtractScaleFromMatrix() : this(VFXValue<Matrix4x4>.Default)
        {
        }

        public VFXExpressionExtractScaleFromMatrix(VFXExpression parent) : base(VFXExpression.Flags.None, new VFXExpression[] { parent })
        {
        }

        public override VFXExpressionOperation operation
        {
            get
            {
                return VFXExpressionOperation.ExtractScaleFromMatrix;
            }
        }

        protected sealed override VFXExpression Reduce(VFXExpression[] reducedParents)
        {
            var parent = reducedParents[0];
            if (parent is VFXExpressionTRSToMatrix)
                return parent.parents[2];

            return base.Reduce(reducedParents);
        }

        sealed protected override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            var matrixReduce = constParents[0];
            var matrix = matrixReduce.Get<Matrix4x4>();
            matrix.SetRow(3, new Vector4(0.0f, 0.0f, 0.0f, 1.0f));
            return VFXValue.Constant(matrix.lossyScale);
        }

        public override string GetCodeString(string[] parents)
        {
            return string.Format("float3(length({0}._11_21_31),length({0}._12_22_32),length({0}._13_23_33))", parents[0]);
        }
    }

    class VFXExpressionTransformMatrix : VFXExpression
    {
        public VFXExpressionTransformMatrix() : this(VFXValue<Matrix4x4>.Default, VFXValue<Matrix4x4>.Default)
        {
        }

        public VFXExpressionTransformMatrix(VFXExpression left, VFXExpression right) : base(VFXExpression.Flags.None, new VFXExpression[] { left, right })
        {
        }

        public override VFXExpressionOperation operation
        {
            get
            {
                return VFXExpressionOperation.TransformMatrix;
            }
        }

        protected sealed override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            var left = constParents[0].Get<Matrix4x4>();
            var right = constParents[1].Get<Matrix4x4>();
            return VFXValue.Constant(left * right);
        }

        public override string GetCodeString(string[] parents)
        {
            return string.Format("mul({0}, {1})", parents[0], parents[1]);
        }
    }

    class VFXExpressionTransformPosition : VFXExpression
    {
        public VFXExpressionTransformPosition() : this(VFXValue<Matrix4x4>.Default, VFXValue<Vector3>.Default)
        {
        }

        public VFXExpressionTransformPosition(VFXExpression matrix, VFXExpression position) : base(VFXExpression.Flags.None, new VFXExpression[] { matrix, position })
        {
        }

        public override VFXExpressionOperation operation => VFXExpressionOperation.TransformPos;

        protected override VFXExpression Reduce(VFXExpression[] reducedParents)
        {
            if (reducedParents[0].Is(VFXExpression.Flags.Constant) && reducedParents[0] is VFXValue<Matrix4x4> && reducedParents[0].Get<Matrix4x4>() is { isIdentity: true })
            {
                return reducedParents[1];
            }

            return base.Reduce(reducedParents);
        }

        protected sealed override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            var matrixReduce = constParents[0];
            var positionReduce = constParents[1];

            var matrix = matrixReduce.Get<Matrix4x4>();
            var position = positionReduce.Get<Vector3>();

            return VFXValue.Constant(matrix.MultiplyPoint(position));
        }

        public override string GetCodeString(string[] parents)
        {
            return string.Format("mul({0}, float4({1}, 1.0)).xyz", parents[0], parents[1]);
        }
    }

    class VFXExpressionTransformVector : VFXExpression
    {
        public VFXExpressionTransformVector() : this(VFXValue<Matrix4x4>.Default, VFXValue<Vector3>.Default)
        {
        }

        public VFXExpressionTransformVector(VFXExpression matrix, VFXExpression vector) : base(Flags.None, new VFXExpression[] { matrix, vector })
        {
        }

        public override VFXExpressionOperation operation => VFXExpressionOperation.TransformVec;

        protected override VFXExpression Reduce(VFXExpression[] reducedParents)
        {
            if (reducedParents[0].Is(Flags.Constant) && reducedParents[0].Get<Matrix4x4>() is var matrix4x4 && matrix4x4.IsMatrix3x3Identity())
            {
                return reducedParents[1];
            }

            return base.Reduce(reducedParents);
        }

        protected sealed override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            var matrixReduce = constParents[0];
            var positionReduce = constParents[1];

            var matrix = matrixReduce.Get<Matrix4x4>();
            var vector = positionReduce.Get<Vector3>();

            return VFXValue.Constant(matrix.MultiplyVector(vector));
        }

        public override string GetCodeString(string[] parents)
        {
            return string.Format("mul((float3x3){0}, {1})", parents[0], parents[1]);
        }
    }

    class VFXExpressionTransformVector4 : VFXExpression
    {
        public VFXExpressionTransformVector4()
            : this(VFXValue<Matrix4x4>.Default, VFXValue<Vector4>.Default)
        {
        }

        public VFXExpressionTransformVector4(VFXExpression matrix, VFXExpression vec)
            : base(VFXExpression.Flags.None, new VFXExpression[] { matrix, vec })
        {
        }

        public override VFXExpressionOperation operation => VFXExpressionOperation.TransformVector4;

        protected override VFXExpression Reduce(VFXExpression[] reducedParents)
        {
            if (reducedParents[0].Is(Flags.Constant) && reducedParents[0].Get<Matrix4x4>() is { isIdentity: true })
            {
                return reducedParents[1];
            }

            return base.Reduce(reducedParents);
        }

        protected sealed override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            var matrixReduce = constParents[0];
            var vReduce = constParents[1];

            var matrix = matrixReduce.Get<Matrix4x4>();
            var vec = vReduce.Get<Vector4>();

            // No multiply Vector4 in Unity API :(
            Vector4 dst = new Vector4();
            dst.x = Vector4.Dot(matrix.GetRow(0), vec);
            dst.y = Vector4.Dot(matrix.GetRow(1), vec);
            dst.z = Vector4.Dot(matrix.GetRow(2), vec);
            dst.w = Vector4.Dot(matrix.GetRow(3), vec);

            return VFXValue.Constant(dst);
        }

        public override string GetCodeString(string[] parents)
        {
            return string.Format("mul({0}, {1})", parents[0], parents[1]);
        }
    }

    class VFXExpressionTransformDirection : VFXExpression
    {
        public VFXExpressionTransformDirection() : this(VFXValue<Matrix4x4>.Default, VFXValue<Vector3>.Default)
        {
        }

        public VFXExpressionTransformDirection(VFXExpression matrix, VFXExpression vector) : base(VFXExpression.Flags.None, new VFXExpression[] { matrix, vector })
        {
        }

        public override VFXExpressionOperation operation => VFXExpressionOperation.TransformDir;

        protected override VFXExpression Reduce(VFXExpression[] reducedParents)
        {
            if (reducedParents[0].Is(Flags.Constant) && reducedParents[0].Get<Matrix4x4>() is var matrix4x4 && matrix4x4.IsMatrix3x3Identity())
            {
                return reducedParents[1];
            }

            return base.Reduce(reducedParents);
        }

        protected sealed override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            var matrixReduce = constParents[0];
            var positionReduce = constParents[1];

            var matrix = matrixReduce.Get<Matrix4x4>();
            var vector = positionReduce.Get<Vector3>();

            return VFXValue.Constant(matrix.MultiplyVector(vector).normalized);
        }

        public override string GetCodeString(string[] parents)
        {
            return string.Format("normalize(mul((float3x3){0}, {1}))", parents[0], parents[1]);
        }
    }

    class VFXExpressionRowToMatrix : VFXExpression
    {
        public VFXExpressionRowToMatrix() : this(new VFXExpression[] { new VFXValue<Vector4>(new Vector4(1, 0, 0, 0)), new VFXValue<Vector4>(new Vector4(0, 1, 0, 0)), new VFXValue<Vector4>(new Vector4(0, 0, 1, 0)), new VFXValue<Vector4>(new Vector4(0, 0, 0, 1)) }
        )
        {
        }

        public VFXExpressionRowToMatrix(params VFXExpression[] parents) : base(VFXExpression.Flags.None, parents)
        {
        }

        public override VFXExpressionOperation operation
        {
            get
            {
                return VFXExpressionOperation.RowToMatrix;
            }
        }

        sealed protected override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            var xReduce = constParents[0];
            var yReduce = constParents[1];
            var zReduce = constParents[2];
            var wReduce = constParents[3];

            var x = xReduce.Get<Vector4>();
            var y = yReduce.Get<Vector4>();
            var z = zReduce.Get<Vector4>();
            var w = wReduce.Get<Vector4>();

            Matrix4x4 matrix = new Matrix4x4();
            matrix.SetRow(0, x);
            matrix.SetRow(1, y);
            matrix.SetRow(2, z);
            matrix.SetRow(3, w);

            return VFXValue.Constant(matrix);
        }

        public override string GetCodeString(string[] parents)
        {
            return string.Format("VFXCreateMatrixFromRows({0}, {1}, {2}, {3});", parents[0], parents[1], parents[2], parents[3]);
        }
    }

    class VFXExpressionColumnToMatrix : VFXExpression
    {
        public VFXExpressionColumnToMatrix() : this(new VFXExpression[] { new VFXValue<Vector4>(new Vector4(1, 0, 0, 0)), new VFXValue<Vector4>(new Vector4(0, 1, 0, 0)), new VFXValue<Vector4>(new Vector4(0, 0, 1, 0)), new VFXValue<Vector4>(new Vector4(0, 0, 0, 1)) }
        )
        {
        }

        public VFXExpressionColumnToMatrix(params VFXExpression[] parents) : base(VFXExpression.Flags.None, parents)
        {
        }

        public override VFXExpressionOperation operation
        {
            get
            {
                return VFXExpressionOperation.ColumnToMatrix;
            }
        }

        sealed protected override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            var xReduce = constParents[0];
            var yReduce = constParents[1];
            var zReduce = constParents[2];
            var wReduce = constParents[3];

            var x = xReduce.Get<Vector4>();
            var y = yReduce.Get<Vector4>();
            var z = zReduce.Get<Vector4>();
            var w = wReduce.Get<Vector4>();

            Matrix4x4 matrix = new Matrix4x4();
            matrix.SetColumn(0, x);
            matrix.SetColumn(1, y);
            matrix.SetColumn(2, z);
            matrix.SetColumn(3, w);

            return VFXValue.Constant(matrix);
        }

        public override string GetCodeString(string[] parents)
        {
            return string.Format("VFXCreateMatrixFromColumns({0}, {1}, {2}, {3});", parents[0], parents[1], parents[2], parents[3]);
        }
    }

    class VFXExpressionAxisToMatrix : VFXExpression
    {
        public VFXExpressionAxisToMatrix() : this(new VFXExpression[] { new VFXValue<Vector3>(Vector3.right), new VFXValue<Vector3>(Vector3.up), new VFXValue<Vector3>(Vector3.forward), VFXValue<Vector3>.Default }
        )
        {
        }

        public VFXExpressionAxisToMatrix(params VFXExpression[] parents) : base(VFXExpression.Flags.None, parents)
        {
        }

        public override VFXExpressionOperation operation
        {
            get
            {
                return VFXExpressionOperation.AxisToMatrix;
            }
        }

        sealed protected override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            var xReduce = constParents[0];
            var yReduce = constParents[1];
            var zReduce = constParents[2];
            var wReduce = constParents[3];

            var x = xReduce.Get<Vector3>();
            var y = yReduce.Get<Vector3>();
            var z = zReduce.Get<Vector3>();
            var w = wReduce.Get<Vector3>();

            Matrix4x4 matrix = new Matrix4x4();
            matrix.SetColumn(0, new Vector4(x.x, x.y, x.z, 0.0f));
            matrix.SetColumn(1, new Vector4(y.x, y.y, y.z, 0.0f));
            matrix.SetColumn(2, new Vector4(z.x, z.y, z.z, 0.0f));
            matrix.SetColumn(3, new Vector4(w.x, w.y, w.z, 1.0f));

            return VFXValue.Constant(matrix);
        }

        public override string GetCodeString(string[] parents)
        {
            return string.Format("VFXCreateMatrixFromColumns(float4({0}, 0.0), float4({1}, 0.0), float4({2}, 0.0), float4({3}, 1.0));", parents[0], parents[1], parents[2], parents[3]);
        }
    }

    class VFXExpressionMatrixToRow : VFXExpression
    {
        public VFXExpressionMatrixToRow() : this(new VFXExpression[] { VFXValue<Matrix4x4>.Default, VFXValue.Constant<int>(0) }
        )
        {
        }

        public VFXExpressionMatrixToRow(params VFXExpression[] parents) : base(VFXExpression.Flags.None, parents)
        {
        }

        public override VFXExpressionOperation operation
        {
            get
            {
                return VFXExpressionOperation.MatrixToRow;
            }
        }

        protected sealed override VFXExpression Reduce(VFXExpression[] reducedParents)
        {
            var parent = reducedParents[0];
            if (parent is VFXExpressionRowToMatrix && reducedParents[1].Is(Flags.Constant))
                return parent.parents[reducedParents[1].Get<int>()];

            return base.Reduce(reducedParents);
        }

        sealed protected override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            var matReduce = constParents[0];
            var axisReduce = constParents[1];

            var mat = matReduce.Get<Matrix4x4>();
            var axis = axisReduce.Get<int>();

            return VFXValue.Constant(mat.GetRow(axis));
        }

        public override string GetCodeString(string[] parents)
        {
            return string.Format("VFXGetRowFromMatrix({0}, {1})", parents[0], parents[1]);
        }
    }

    class VFXExpressionMatrixToColumn : VFXExpression
    {
        public VFXExpressionMatrixToColumn() : this(new VFXExpression[] { VFXValue<Matrix4x4>.Default, VFXValue.Constant<int>(0) }
        )
        {
        }

        public VFXExpressionMatrixToColumn(params VFXExpression[] parents) : base(VFXExpression.Flags.None, parents)
        {
        }

        public override VFXExpressionOperation operation
        {
            get
            {
                return VFXExpressionOperation.MatrixToColumn;
            }
        }

        protected sealed override VFXExpression Reduce(VFXExpression[] reducedParents)
        {
            var parent = reducedParents[0];
            if (parent is VFXExpressionColumnToMatrix && reducedParents[1].Is(Flags.Constant))
                return parent.parents[reducedParents[1].Get<int>()];

            return base.Reduce(reducedParents);
        }

        sealed protected override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            var matReduce = constParents[0];
            var axisReduce = constParents[1];

            var mat = matReduce.Get<Matrix4x4>();
            var axis = axisReduce.Get<int>();

            return VFXValue.Constant(mat.GetColumn(axis));
        }

        public override string GetCodeString(string[] parents)
        {
            return string.Format("VFXGetColumnFromMatrix({0}, {1})", parents[0], parents[1]);
        }
    }

    class VFXExpressionMatrixToAxis : VFXExpression
    {
        public VFXExpressionMatrixToAxis() : this(new VFXExpression[] { VFXValue<Matrix4x4>.Default, VFXValue.Constant<int>(0) }
        )
        {
        }

        public VFXExpressionMatrixToAxis(params VFXExpression[] parents) : base(VFXExpression.Flags.None, parents)
        {
        }

        public override VFXExpressionOperation operation
        {
            get
            {
                return VFXExpressionOperation.MatrixToAxis;
            }
        }

        protected sealed override VFXExpression Reduce(VFXExpression[] reducedParents)
        {
            var parent = reducedParents[0];
            if (parent is VFXExpressionAxisToMatrix && reducedParents[1].Is(Flags.Constant))
                return parent.parents[reducedParents[1].Get<int>()];

            return base.Reduce(reducedParents);
        }

        sealed protected override VFXExpression Evaluate(VFXExpression[] constParents)
        {
            var matReduce = constParents[0];
            var axisReduce = constParents[1];

            var mat = matReduce.Get<Matrix4x4>();
            var axis = axisReduce.Get<int>();

            Vector4 c = mat.GetColumn(axis);
            return VFXValue.Constant(new Vector3(c.x, c.y, c.z));
        }

        public override string GetCodeString(string[] parents)
        {
            return string.Format("VFXGetColumnFromMatrix({0}, {1}).xyz", parents[0], parents[1]);
        }
    }
}
