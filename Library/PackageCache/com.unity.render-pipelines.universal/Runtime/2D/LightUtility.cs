using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine.U2D;

namespace UnityEngine.Rendering.Universal
{
    // Per Light parameters to batch.
    struct PerLight2D
    {
        internal float4x4 InvMatrix;
        internal float4 Color;
        internal float4 Position;
        internal float FalloffIntensity;
        internal float FalloffDistance;
        internal float OuterAngle;
        internal float InnerAngle;
        internal float InnerRadiusMult;
        internal float VolumeOpacity;
        internal float ShadowIntensity;
        internal int LightType;
    };

    internal static class LightUtility
    {
        public static bool CheckForChange(Light2D.LightType a, ref Light2D.LightType b)
        {
            var changed = a != b;
            b = a;
            return changed;
        }

        public static bool CheckForChange(Component a, ref Component b)
        {
            var changed = a != b;
            b = a;
            return changed;
        }

        public static bool CheckForChange(int a, ref int b)
        {
            var changed = a != b;
            b = a;
            return changed;
        }

        public static bool CheckForChange(float a, ref float b)
        {
            var changed = a != b;
            b = a;
            return changed;
        }

        public static bool CheckForChange(bool a, ref bool b)
        {
            var changed = a != b;
            b = a;
            return changed;
        }

        private enum PivotType
        {
            PivotBase,
            PivotCurve,
            PivotIntersect,
            PivotSkip,
            PivotClip
        };

        [Serializable]
        internal struct LightMeshVertex
        {
            public Vector3 position;
            public Color color;
            public Vector2 uv;

            public static readonly VertexAttributeDescriptor[] VertexLayout = new[]
            {
                new VertexAttributeDescriptor(VertexAttribute.Position, VertexAttributeFormat.Float32, 3),
                new VertexAttributeDescriptor(VertexAttribute.Color, VertexAttributeFormat.Float32, 4),
                new VertexAttributeDescriptor(VertexAttribute.TexCoord0, VertexAttributeFormat.Float32, 2),
            };
        }

        static bool TestPivot(List<IntPoint> path, int activePoint, long lastPoint)
        {
            for (int i = activePoint; i < path.Count; ++i)
            {
                if (path[i].N > lastPoint)
                    return true;
            }

            return (path[activePoint].N == -1);
        }

        // Degenerate Pivots at the End Points.
        static List<IntPoint> DegeneratePivots(List<IntPoint> path, List<IntPoint> inPath, ref int interiorStart)
        {
            List<IntPoint> degenerate = new List<IntPoint>();
            var minN = path[0].N;
            var maxN = path[0].N;
            for (int i = 1; i < path.Count; ++i)
            {
                if (path[i].N != -1)
                {
                    minN = Math.Min(minN, path[i].N);
                    maxN = Math.Max(maxN, path[i].N);
                }
            }

            for (long i = 0; i < minN; ++i)
            {
                IntPoint ins = path[(int)minN];
                ins.N = i;
                degenerate.Add(ins);
            }
            degenerate.AddRange(path.GetRange(0, path.Count));
            interiorStart = degenerate.Count;
            for (long i = maxN + 1; i < inPath.Count; ++i)
            {
                IntPoint ins = inPath[(int)i];
                ins.N = i;
                degenerate.Add(ins);
            }
            return degenerate;
        }

        // Ensure that we get a valid path from 0.
        static List<IntPoint> SortPivots(List<IntPoint> outPath, List<IntPoint> inPath)
        {
            List<IntPoint> sorted = new List<IntPoint>();
            var min = outPath[0].N;
            var max = outPath[0].N;
            var minIndex = 0;
            bool newMin = true;
            for (int i = 1; i < outPath.Count; ++i)
            {
                if (max > outPath[i].N && newMin && outPath[i].N != -1)
                {
                    min = max = outPath[i].N;
                    minIndex = i;
                    newMin = false;
                }
                else if (outPath[i].N >= max)
                {
                    max = outPath[i].N;
                    newMin = true;
                }
            }
            sorted.AddRange(outPath.GetRange(minIndex, (outPath.Count - minIndex)));
            sorted.AddRange(outPath.GetRange(0, minIndex));
            return sorted;
        }

        // Ensure that all points eliminated due to overlaps and intersections are accounted for Tessellation.
        static List<IntPoint> FixPivots(List<IntPoint> outPath, List<IntPoint> inPath, ref int interiorStart)
        {
            var path = SortPivots(outPath, inPath);
            long pivotPoint = path[0].N;

            // Connect Points for Overlaps.
            for (int i = 1; i < path.Count; ++i)
            {
                var j = (i == path.Count - 1) ? 0 : (i + 1);
                var prev = path[i - 1];
                var curr = path[i];
                var next = path[j];

                if (prev.N > curr.N)
                {
                    var incr = TestPivot(path, i, pivotPoint);
                    if (incr)
                    {
                        if (prev.N == next.N)
                            curr.N = prev.N;
                        else
                            curr.N = (pivotPoint + 1) < inPath.Count ? (pivotPoint + 1) : 0;
                        curr.D = 3;
                        path[i] = curr;
                    }
                }
                pivotPoint = path[i].N;
            }

            // Insert Skipped Points.
            for (int i = 1; i < path.Count - 1;)
            {
                var prev = path[i - 1];
                var curr = path[i];
                var next = path[i + 1];

                if (curr.N - prev.N > 1)
                {
                    if (curr.N == next.N)
                    {
                        IntPoint ins = curr;
                        ins.N = (ins.N - 1);
                        path[i] = ins;
                    }
                    else
                    {
                        IntPoint ins = curr;
                        ins.N = (ins.N - 1);
                        path.Insert(i, ins);
                    }
                }
                else
                {
                    i++;
                }
            }

            path = DegeneratePivots(path, inPath, ref interiorStart);
            return path;
        }

        // Rough shape only used in Inspector for quick preview.
        internal static List<Vector2> GetOutlinePath(Vector3[] shapePath, float offsetDistance)
        {
            const float kClipperScale = 10000.0f;
            List<IntPoint> path = new List<IntPoint>();
            List<Vector2> output = new List<Vector2>();
            for (var i = 0; i < shapePath.Length; ++i)
            {
                var newPoint = new Vector2(shapePath[i].x, shapePath[i].y) * kClipperScale;
                path.Add(new IntPoint((System.Int64)(newPoint.x), (System.Int64)(newPoint.y)));
            }
            List<List<IntPoint>> solution = new List<List<IntPoint>>();
            ClipperOffset clipOffset = new ClipperOffset(24.0f);
            clipOffset.AddPath(path, JoinTypes.jtRound, EndTypes.etClosedPolygon);
            clipOffset.Execute(ref solution, kClipperScale * offsetDistance, path.Count);
            if (solution.Count > 0)
            {
                int interiorStart = 0;
                var outPath = solution[0];
                outPath = FixPivots(outPath, path, ref interiorStart);

                for (int i = 0; i < outPath.Count; ++i)
                    output.Add(new Vector2(outPath[i].X / kClipperScale, outPath[i].Y / kClipperScale));
            }
            return output;
        }

        static void TransferToMesh(NativeArray<LightMeshVertex> vertices, int vertexCount, NativeArray<ushort> indices,
            int indexCount, Light2D light)
        {
            var mesh = light.lightMesh;
            mesh.SetVertexBufferParams(vertexCount, LightMeshVertex.VertexLayout);
            mesh.SetVertexBufferData(vertices, 0, 0, vertexCount);
            mesh.SetIndices(indices, 0, indexCount, MeshTopology.Triangles, 0, true);

            light.vertices = new LightMeshVertex[vertexCount];
            NativeArray<LightMeshVertex>.Copy(vertices, light.vertices, vertexCount);
            light.indices = new ushort[indexCount];
            NativeArray<ushort>.Copy(indices, light.indices, indexCount);
        }

        public static Bounds GenerateShapeMesh(Light2D light, Vector3[] shapePath, float falloffDistance, float batchColor)
        {
            const float kClipperScale = 10000.0f;

            var restoreState = Random.state;
            Random.InitState(123456); // for deterministic output

            // todo Revisit this while we do Batching.
            var meshInteriorColor = new Color(0, 0, batchColor, 1.0f);
            var meshExteriorColor = new Color(0, 0, batchColor, 0.0f);

            // Create shape geometry based on edges
            int inEdgeCount = shapePath.Length;
            NativeArray<int2> tessInEdges = new NativeArray<int2>(inEdgeCount, Allocator.Temp);
            NativeArray<float2> tessInVertices = new NativeArray<float2>(inEdgeCount, Allocator.Temp);

            for (int i = 0; i < inEdgeCount; ++i)
            {
                int edgeEnd = i + 1;
                if (edgeEnd == inEdgeCount)
                    edgeEnd = 0;

                int2 edge = new int2(i, edgeEnd);
                tessInEdges[i] = edge;

                int index = edge.x;
                tessInVertices[index] = new float2(shapePath[index].x, shapePath[index].y);
            }

            // Do tessellation
            NativeArray<int> tessOutIndices = new NativeArray<int>(tessInEdges.Length * 8, Allocator.Temp);
            NativeArray<float2> tessOutVertices = new NativeArray<float2>(tessInEdges.Length * 8, Allocator.Temp);
            NativeArray<int2> tessOutEdges = new NativeArray<int2>(tessInEdges.Length * 8, Allocator.Temp);
            int tessOutVertexCount = 0;
            int tessOutIndexCount = 0;
            int tessOutEdgeCount = 0;

            UTess.ModuleHandle.Tessellate(Allocator.Temp, tessInVertices, tessInEdges, ref tessOutVertices, ref tessOutVertexCount, ref tessOutIndices, ref tessOutIndexCount, ref tessOutEdges, ref tessOutEdgeCount);

            // Create falloff geometry with random noise to account for collinear points
            var inputPointCount = shapePath.Length;
            List<IntPoint> path = new List<IntPoint>();
            for (var i = 0; i < inputPointCount; ++i)
            {
                var nx = (System.Int64)((double)shapePath[i].x * (double)kClipperScale);
                var ny = (System.Int64)((double)shapePath[i].y * (double)kClipperScale);
                var addPoint = new IntPoint(nx + Random.Range(-10, 10), ny + Random.Range(-10, 10));
                addPoint.N = i; addPoint.D = -1;
                path.Add(addPoint);
            }
            var lastPointIndex = inputPointCount - 1;
            var interiorStartPoint = 0;

            // Generate Bevels.
            List<List<IntPoint>> solution = new List<List<IntPoint>>();
            ClipperOffset clipOffset = new ClipperOffset(24.0f);
            clipOffset.AddPath(path, JoinTypes.jtRound, EndTypes.etClosedPolygon);
            clipOffset.Execute(ref solution, kClipperScale * falloffDistance, path.Count);

            if (solution.Count > 0)
            {
                // Fix path for Pivots.
                var outPath = solution[0];
                var minPath = (long)inputPointCount;
                for (int i = 0; i < outPath.Count; ++i)
                    minPath = (outPath[i].N != -1) ? Math.Min(minPath, outPath[i].N) : minPath;
                var containsStart = minPath == 0;
                outPath = FixPivots(outPath, path, ref interiorStartPoint);

                // Size accounts for light mesh + falloff geometry(outer + inner)
                int totalOutVertices = tessOutVertexCount + outPath.Count + inputPointCount;
                int totalOutIndices = tessOutIndexCount + (outPath.Count * 6) + 6;
                var outVertices = new NativeArray<LightMeshVertex>(totalOutVertices, Allocator.Temp);
                var outIndices = new NativeArray<ushort>(totalOutIndices, Allocator.Temp);

                for (int i = 0; i < tessOutIndexCount; ++i)
                    outIndices[i] = (ushort)tessOutIndices[i];

                for (int i = 0; i < tessOutVertexCount; ++i)
                {
                    outVertices[i] = new LightMeshVertex()
                    {
                        position = new float3(tessOutVertices[i].x, tessOutVertices[i].y, 0),
                        color = meshInteriorColor
                    };
                }

                var vcount = tessOutVertexCount;
                var icount = tessOutIndexCount;
                var innerIndices = new ushort[inputPointCount];

                // Inner Vertices. (These may or may not be part of the created path. Beware!!)
                for (int i = 0; i < inputPointCount; ++i)
                {
                    outVertices[vcount++] = new LightMeshVertex()
                    {
                        position = new float3(shapePath[i].x, shapePath[i].y, 0),
                        color = meshInteriorColor
                    };
                    innerIndices[i] = (ushort)(vcount - 1);
                }

                var saveIndex = (ushort)vcount;
                var pathStart = saveIndex;
                var prevIndex = outPath[0].N == -1 ? 0 : outPath[0].N;

                // Outer Vertices
                for (int i = 0; i < outPath.Count; ++i)
                {
                    var curr = outPath[i];
                    var currPoint = new float2(curr.X / kClipperScale, curr.Y / kClipperScale);
                    var currIndex = curr.N == -1 ? 0 : curr.N;

                    outVertices[vcount++] = new LightMeshVertex()
                    {
                        position = new float3(currPoint.x, currPoint.y, 0),
                        color = (interiorStartPoint > i) ? meshExteriorColor : meshInteriorColor
                    };

                    if (prevIndex != currIndex)
                    {
                        outIndices[icount++] = innerIndices[prevIndex];
                        outIndices[icount++] = innerIndices[currIndex];
                        outIndices[icount++] = (ushort)(vcount - 1);
                    }

                    outIndices[icount++] = innerIndices[prevIndex];
                    outIndices[icount++] = saveIndex;
                    outIndices[icount++] = saveIndex = (ushort)(vcount - 1);
                    prevIndex = currIndex;
                }

                // Close the Loop.
                {
                    outIndices[icount++] = pathStart;
                    outIndices[icount++] = innerIndices[minPath];
                    outIndices[icount++] = containsStart ? innerIndices[lastPointIndex] : saveIndex;

                    outIndices[icount++] = containsStart ? pathStart : saveIndex;
                    outIndices[icount++] = containsStart ? saveIndex : innerIndices[minPath];

                    // Last Triangle. todo: Remove Clipper Usage and use SpriteShape Geometry Generator for falloff potentially.
                    if (containsStart)
                    {
                        var kTolerance = 0.001f;
                        var connectingPoint = innerIndices[lastPointIndex];
                        // End point detection is tricky and depends on convexity of shape. Simple test is to just check the vertices and detect.
                        var testA = MathF.Abs(outVertices[connectingPoint].position.x - outVertices[outIndices[icount - 1]].position.x) > kTolerance || MathF.Abs(outVertices[connectingPoint].position.y - outVertices[outIndices[icount - 1]].position.y) > kTolerance;
                        var testB = MathF.Abs(outVertices[connectingPoint].position.x - outVertices[outIndices[icount - 2]].position.x) > kTolerance || MathF.Abs(outVertices[connectingPoint].position.y - outVertices[outIndices[icount - 2]].position.y) > kTolerance;
                        if (!testA || !testB)
                            connectingPoint = (ushort)(interiorStartPoint + inputPointCount + tessOutVertexCount - 1);
                        outIndices[icount++] = connectingPoint;
                    }
                    else
                        outIndices[icount++] = innerIndices[minPath - 1];
                }

                TransferToMesh(outVertices, vcount, outIndices, icount, light);
            }

            Random.state = restoreState;
            return light.lightMesh.GetSubMesh(0).bounds;
        }

        public static Bounds GenerateParametricMesh(Light2D light, float radius, float falloffDistance, float angle, int sides, float batchColor)
        {
            var angleOffset = Mathf.PI / 2.0f + Mathf.Deg2Rad * angle;
            if (sides < 3)
            {
                radius = 0.70710678118654752440084436210485f * radius;
                sides = 4;
            }

            if (sides == 4)
            {
                angleOffset = Mathf.PI / 4.0f + Mathf.Deg2Rad * angle;
            }

            var vertexCount = 1 + 2 * sides;
            var indexCount = 3 * 3 * sides;
            var vertices = new NativeArray<LightMeshVertex>(vertexCount, Allocator.Temp);
            var triangles = new NativeArray<ushort>(indexCount, Allocator.Temp);
            var centerIndex = (ushort)(2 * sides);
            var mesh = light.lightMesh;

            // Only Alpha value in Color channel is ever used. May remove it or keep it for batching params in the future.
            var color = new Color(0, 0, batchColor, 1.0f);
            vertices[centerIndex] = new LightMeshVertex
            {
                position = float3.zero,
                color = color
            };

            var radiansPerSide = 2 * Mathf.PI / sides;
            var min = new float3(float.MaxValue, float.MaxValue, 0);
            var max = new float3(float.MinValue, float.MinValue, 0);

            for (var i = 0; i < sides; i++)
            {
                var endAngle = (i + 1) * radiansPerSide;
                var extrudeDir = new float3(math.cos(endAngle + angleOffset), math.sin(endAngle + angleOffset), 0);
                var endPoint = radius * extrudeDir;

                var vertexIndex = (2 * i + 2) % (2 * sides);
                vertices[vertexIndex] = new LightMeshVertex
                {
                    position = endPoint,
                    color = new Color(extrudeDir.x, extrudeDir.y, batchColor, 0)
                };
                vertices[vertexIndex + 1] = new LightMeshVertex
                {
                    position = endPoint,
                    color = color
                };

                // Triangle 1 (Tip)
                var triangleIndex = 9 * i;
                triangles[triangleIndex] = (ushort)(vertexIndex + 1);
                triangles[triangleIndex + 1] = (ushort)(2 * i + 1);
                triangles[triangleIndex + 2] = centerIndex;

                // Triangle 2 (Upper Top Left)
                triangles[triangleIndex + 3] = (ushort)(vertexIndex);
                triangles[triangleIndex + 4] = (ushort)(2 * i);
                triangles[triangleIndex + 5] = (ushort)(2 * i + 1);

                // Triangle 2 (Bottom Top Left)
                triangles[triangleIndex + 6] = (ushort)(vertexIndex + 1);
                triangles[triangleIndex + 7] = (ushort)(vertexIndex);
                triangles[triangleIndex + 8] = (ushort)(2 * i + 1);

                min = math.min(min, endPoint + extrudeDir * falloffDistance);
                max = math.max(max, endPoint + extrudeDir * falloffDistance);
            }

            mesh.SetVertexBufferParams(vertexCount, LightMeshVertex.VertexLayout);
            mesh.SetVertexBufferData(vertices, 0, 0, vertexCount);
            mesh.SetIndices(triangles, MeshTopology.Triangles, 0, false);

            light.vertices = new LightMeshVertex[vertexCount];
            NativeArray<LightMeshVertex>.Copy(vertices, light.vertices, vertexCount);
            light.indices = new ushort[indexCount];
            NativeArray<ushort>.Copy(triangles, light.indices, indexCount);

            return new Bounds
            {
                min = min,
                max = max
            };
        }

        public static Bounds GenerateSpriteMesh(Light2D light, Sprite sprite, float batchColor)
        {
            var mesh = light.lightMesh;

            if (sprite == null)
            {
                mesh.Clear();
                return new Bounds(Vector3.zero, Vector3.zero);
            }

            // this needs to be called before getting UV at the line below.
            // Venky fixed it, enroute to trunk
            var uvs = sprite.uv;

            var srcVertices = sprite.GetVertexAttribute<Vector3>(VertexAttribute.Position);
            var srcUVs = sprite.GetVertexAttribute<Vector2>(VertexAttribute.TexCoord0);
            var srcIndices = sprite.GetIndices();

            var center = 0.5f * (sprite.bounds.min + sprite.bounds.max);
            var vertices = new NativeArray<LightMeshVertex>(srcIndices.Length, Allocator.Temp);
            var color = new Color(0, 0, batchColor, 1);

            for (var i = 0; i < srcVertices.Length; i++)
            {
                vertices[i] = new LightMeshVertex
                {
                    position = new Vector3(srcVertices[i].x, srcVertices[i].y, 0),
                    color = color,
                    uv = srcUVs[i]
                };
            }
            mesh.SetVertexBufferParams(vertices.Length, LightMeshVertex.VertexLayout);
            mesh.SetVertexBufferData(vertices, 0, 0, vertices.Length);
            mesh.SetIndices(srcIndices, MeshTopology.Triangles, 0, true);

            light.vertices = new LightMeshVertex[vertices.Length];
            NativeArray<LightMeshVertex>.Copy(vertices, light.vertices, vertices.Length);
            light.indices = new ushort[srcIndices.Length];
            NativeArray<ushort>.Copy(srcIndices, light.indices, srcIndices.Length);

            return mesh.GetSubMesh(0).bounds;
        }

        public static int GetShapePathHash(Vector3[] path)
        {
            unchecked
            {
                int hashCode = (int)2166136261;

                if (path != null)
                {
                    foreach (var point in path)
                        hashCode = hashCode * 16777619 ^ point.GetHashCode();
                }
                else
                {
                    hashCode = 0;
                }

                return hashCode;
            }
        }
    }
}
