using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.VFX;
using UnityEngine.Profiling;

using Object = UnityEngine.Object;
using UnityEditor.Graphs;
using System.Collections.ObjectModel;

namespace UnityEditor.VFX
{
    enum VFXDeviceTarget
    {
        CPU,
        GPU,
    }

    class VFXExpressionGraph
    {
        private struct ExpressionData
        {
            public int depth;
            public int index;
        }

        public VFXExpressionGraph()
        { }

        private void AddExpressionDataRecursively(Dictionary<VFXExpression, ExpressionData> dst, VFXExpression exp, int depth = 0)
        {
            ExpressionData data;
            if (!dst.TryGetValue(exp, out data) || data.depth < depth)
            {
                data.index = -1; // Will be overridden later on
                data.depth = depth;
                dst[exp] = data;
                foreach (var parent in exp.parents)
                    AddExpressionDataRecursively(dst, parent, depth + 1);
            }
        }

        private void CompileExpressionContext(IEnumerable<VFXContext> contexts,
            VFXExpressionContextOption options,
            VFXDeviceTarget target)
        {
            var expressionContext = new VFXExpression.Context(options, m_GlobalEventAttributes);

            var contextsToExpressions = target == VFXDeviceTarget.GPU ? m_ContextsToGPUExpressions : m_ContextsToCPUExpressions;
            var expressionsToReduced = target == VFXDeviceTarget.GPU ? m_GPUExpressionsToReduced : m_CPUExpressionsToReduced;

            foreach (var context in contexts)
            {
                var mapper = context.GetExpressionMapper(target);
                if (mapper != null)
                {
                    foreach (var exp in mapper.expressions)
                        expressionContext.RegisterExpression(exp, context);
                    contextsToExpressions.Add(context, mapper);
                }
            }

            expressionContext.Compile();

            foreach (var exp in expressionContext.RegisteredExpressions)
            {
                var reduced = expressionContext.GetReduced(exp);
                if (expressionsToReduced.TryGetValue(exp, out var previousReducedExpression))
                {
                    if (reduced != previousReducedExpression)
                        throw new InvalidOperationException("Unexpected diverging expression reduction");
                    continue;
                }
                expressionsToReduced.Add(exp, reduced);
            }

            var allReduced = expressionContext.BuildAllReduced();

            m_Expressions.UnionWith(allReduced);

            foreach (var exp in expressionsToReduced.Values)
                AddExpressionDataRecursively(m_ExpressionsData, exp);

            if (options.HasFlag(VFXExpressionContextOption.CollectPerContextData))
            {
                foreach (var bufferTypeUsage in expressionContext.GraphicsBufferTypeUsagePerContext)
                {
                    m_BufferTypeUsagePerContext.TryAdd(bufferTypeUsage.Key, bufferTypeUsage.Value);
                }

                foreach (var hlslCodeHolder in expressionContext.hlslCodeHoldersPerContext)
                {
                    m_CustomHLSLExpressionsPerContext.Add(hlslCodeHolder.Key, hlslCodeHolder.Value);
                }
            }
        }

        public void CompileExpressions(VFXGraph graph, VFXExpressionContextOption options, bool filterOutInvalidContexts = false)
        {
            var models = new HashSet<ScriptableObject>();
            graph.CollectDependencies(models, false);
            var contexts = models.OfType<VFXContext>();
            if (filterOutInvalidContexts)
                contexts = contexts.Where(c => c.CanBeCompiled());

            CompileExpressions(contexts, options);
        }

        private static void ComputeEventAttributeDescs(List<VFXLayoutElementDesc> globalEventAttributes, IEnumerable<VFXContext> contexts)
        {
            globalEventAttributes.Clear();

            //SpawnCount should always be added first : spawnCount is an implicit parameter from eventAttribute
            globalEventAttributes.Add(new VFXLayoutElementDesc()
            {
                name = VFXAttribute.SpawnCount.name,
                type = VFXAttribute.SpawnCount.type
            });

            IEnumerable<VFXLayoutElementDesc> globalAttribute = Enumerable.Empty<VFXLayoutElementDesc>();
            foreach (var context in contexts.Where(o => o.contextType == VFXContextType.Spawner || o.contextType == VFXContextType.Event))
            {
                var attributesToStoreFromOutputContext = context.outputContexts.Select(o => o.GetData()).Where(o => o != null)
                    .SelectMany(o => o.GetAttributes().Where(a => (a.mode & VFXAttributeMode.ReadSource) != 0));
                var attributesReadInSpawnContext = context.GetData().GetAttributes().Where(a => (a.mode & VFXAttributeMode.Read) != 0);
                var attributesInGlobal = attributesToStoreFromOutputContext.Concat(attributesReadInSpawnContext).GroupBy(o => o.attrib.name);

                foreach (var attribute in attributesInGlobal.Select(o => o.First()))
                {
                    if (!globalEventAttributes.Any(o => o.name == attribute.attrib.name))
                    {
                        globalEventAttributes.Add(new VFXLayoutElementDesc()
                        {
                            name = attribute.attrib.name,
                            type = attribute.attrib.type
                        });
                    }
                }
            }

            var structureLayoutTotalSize = (uint)globalEventAttributes.Sum(e => (long)VFXExpression.TypeToSize(e.type));
            var currentLayoutSize = 0u;
            var listWithOffset = new List<VFXLayoutElementDesc>();
            globalEventAttributes.ForEach(e =>
            {
                e.offset.element = currentLayoutSize;
                e.offset.structure = structureLayoutTotalSize;
                currentLayoutSize += (uint)VFXExpression.TypeToSize(e.type);
                listWithOffset.Add(e);
            });

            globalEventAttributes.Clear();
            globalEventAttributes.AddRange(listWithOffset);
        }

        public void CompileExpressions(IEnumerable<VFXContext> contexts, VFXExpressionContextOption options)
        {
            Profiler.BeginSample("VFXEditor.CompileExpressionGraph");

            try
            {
                ComputeEventAttributeDescs(m_GlobalEventAttributes, contexts);

                m_Expressions.Clear();
                m_FlattenedExpressions.Clear();
                m_CommonExpressionCount = 0u;
                m_ExpressionsData.Clear();

                m_ContextsToGPUExpressions.Clear();
                m_ContextsToCPUExpressions.Clear();
                m_GPUExpressionsToReduced.Clear();
                m_CPUExpressionsToReduced.Clear();

                var spawnerContexts = contexts.Where(o => o.contextType == VFXContextType.Spawner);
                var otherContexts = contexts.Where(o => o.contextType != VFXContextType.Spawner);
                CompileExpressionContext(spawnerContexts, options | VFXExpressionContextOption.PatchReadToEventAttribute, VFXDeviceTarget.CPU);
                CompileExpressionContext(otherContexts, options, VFXDeviceTarget.CPU);
                CompileExpressionContext(contexts, options | VFXExpressionContextOption.GPUDataTransformation | VFXExpressionContextOption.CollectPerContextData, VFXDeviceTarget.GPU);

                var sortedList = m_ExpressionsData.Where(kvp =>
                {
                    var exp = kvp.Key;
                    return !exp.IsAny(VFXExpression.Flags.NotCompilableOnCPU); // remove per element expression from flattened data // TODO Remove uniform constants too
                });

                var expressionPerSpawn = sortedList.Where(o => o.Key.Is(VFXExpression.Flags.PerSpawn));
                var expressionNotPerSpawn = sortedList.Where(o => !o.Key.Is(VFXExpression.Flags.PerSpawn));

                //m_FlattenedExpressions is a concatenation of [sorted all expression !PerSpawn] & [sorted all expression Per Spawn]
                //It's more convenient for two reasons :
                // - Reduces process chunk for ComputePreProcessExpressionForSpawn
                // - Allows to determine the maximum index of expression while processing main expression evaluation
                sortedList = expressionNotPerSpawn.OrderByDescending(o => o.Value.depth);
                sortedList = sortedList.Concat(expressionPerSpawn.OrderByDescending(o => o.Value.depth));

                m_FlattenedExpressions = sortedList.Select(o => o.Key).ToList();
                m_CommonExpressionCount = (uint)expressionNotPerSpawn.Count();
                // update index in expression data
                for (int i = 0; i < m_FlattenedExpressions.Count; ++i)
                {
                    var data = m_ExpressionsData[m_FlattenedExpressions[i]];
                    data.index = i;
                    m_ExpressionsData[m_FlattenedExpressions[i]] = data;
                }
                if (VFXViewPreference.advancedLogs)
                    Debug.Log(string.Format("RECOMPILE EXPRESSION GRAPH - NB EXPRESSIONS: {0} - NB CPU END EXPRESSIONS: {1} - NB GPU END EXPRESSIONS: {2}", m_Expressions.Count, m_CPUExpressionsToReduced.Count, m_GPUExpressionsToReduced.Count));
            }
            finally
            {
                Profiler.EndSample();
            }
        }

        public int GetFlattenedIndex(VFXExpression exp)
        {
            if (m_ExpressionsData.ContainsKey(exp))
                return m_ExpressionsData[exp].index;
            return -1;
        }

        public VFXExpression GetReduced(VFXExpression exp, VFXDeviceTarget target)
        {
            VFXExpression reduced;
            var expressionToReduced = target == VFXDeviceTarget.GPU ? m_GPUExpressionsToReduced : m_CPUExpressionsToReduced;
            expressionToReduced.TryGetValue(exp, out reduced);
            return reduced;
        }

        public VFXExpressionMapper BuildCPUMapper(VFXContext context)
        {
            return BuildMapper(context, m_ContextsToCPUExpressions, VFXDeviceTarget.CPU);
        }

        public VFXExpressionMapper BuildGPUMapper(VFXContext context)
        {
            return BuildMapper(context, m_ContextsToGPUExpressions, VFXDeviceTarget.GPU);
        }

        public List<string> GetAllNames(VFXExpression exp)
        {
            List<string> names = new List<string>();
            foreach (var mapper in m_ContextsToCPUExpressions.Values.Concat(m_ContextsToGPUExpressions.Values))
            {
                names.AddRange(mapper.GetData(exp).Select(o => o.fullName));
            }
            return names;
        }

        private VFXExpressionMapper BuildMapper(VFXContext context, Dictionary<VFXContext, VFXExpressionMapper> dictionnary, VFXDeviceTarget target)
        {
            VFXExpression.Flags check = target == VFXDeviceTarget.GPU ? VFXExpression.Flags.InvalidOnGPU | VFXExpression.Flags.PerElement : VFXExpression.Flags.InvalidOnCPU;

            VFXExpressionMapper outMapper = new VFXExpressionMapper();
            VFXExpressionMapper inMapper;

            dictionnary.TryGetValue(context, out inMapper);

            if (inMapper != null)
            {
                foreach (var exp in inMapper.expressions)
                {
                    var reduced = GetReduced(exp, target);
                    if (reduced.Is(check))
                    {
                        var message = $"The expression \"{reduced.GetType().Name}\" is not valid as it have the flag: {check}";
                        context.GetGraph().RegisterCompileError("CompileReduceExpressionFail", message, context);
                        throw new InvalidOperationException(message);
                    }

                    var mappedDataList = inMapper.GetData(exp);
                    foreach (var mappedData in mappedDataList)
                        outMapper.AddExpression(reduced, mappedData);
                }
            }

            return outMapper;
        }

        public HashSet<VFXExpression> Expressions => m_Expressions;

        public List<VFXExpression> FlattenedExpressions => m_FlattenedExpressions;

        public uint CommonExpressionCount => m_CommonExpressionCount;

        public Dictionary<VFXExpression, VFXExpression> GPUExpressionsToReduced => m_GPUExpressionsToReduced;

        public Dictionary<VFXExpression, VFXExpression> CPUExpressionsToReduced => m_CPUExpressionsToReduced;

        public IEnumerable<VFXLayoutElementDesc> GlobalEventAttributes => m_GlobalEventAttributes;

        public ReadOnlyDictionary<VFXExpression, BufferUsage> GetBufferTypeUsage(VFXContext context)
        {
            if (m_BufferTypeUsagePerContext.TryGetValue(context, out var bufferTypeUsage))
            {
                return new ReadOnlyDictionary<VFXExpression, BufferUsage>(bufferTypeUsage);
            }

            return new ReadOnlyDictionary<VFXExpression, BufferUsage>(new Dictionary<VFXExpression, BufferUsage>());
        }

        public IHLSLCodeHolder[] GetCustomHLSLExpressions(VFXContext context)
        {
            if (m_CustomHLSLExpressionsPerContext.TryGetValue(context, out var hlslCodeHolders))
            {
                return hlslCodeHolders.ToArray();
            }
            return Array.Empty<IHLSLCodeHolder>();
        }

        private Dictionary<VFXContext, List<IHLSLCodeHolder>> m_CustomHLSLExpressionsPerContext = new();
        private Dictionary<VFXContext, Dictionary<VFXExpression, BufferUsage>> m_BufferTypeUsagePerContext = new();
        private HashSet<VFXExpression> m_Expressions = new HashSet<VFXExpression>();
        private Dictionary<VFXExpression, VFXExpression> m_CPUExpressionsToReduced = new Dictionary<VFXExpression, VFXExpression>();
        private Dictionary<VFXExpression, VFXExpression> m_GPUExpressionsToReduced = new Dictionary<VFXExpression, VFXExpression>();
        private List<VFXExpression> m_FlattenedExpressions = new List<VFXExpression>();
        private uint m_CommonExpressionCount;
        private Dictionary<VFXExpression, ExpressionData> m_ExpressionsData = new Dictionary<VFXExpression, ExpressionData>();
        private Dictionary<VFXContext, VFXExpressionMapper> m_ContextsToCPUExpressions = new Dictionary<VFXContext, VFXExpressionMapper>();
        private Dictionary<VFXContext, VFXExpressionMapper> m_ContextsToGPUExpressions = new Dictionary<VFXContext, VFXExpressionMapper>();
        private List<VFXLayoutElementDesc> m_GlobalEventAttributes = new List<VFXLayoutElementDesc>();
    }
}
