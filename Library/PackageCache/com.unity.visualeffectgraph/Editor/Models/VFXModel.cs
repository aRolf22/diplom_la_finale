using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using UnityEngine;
using UnityEditor.VFX.UI;
using UnityEngine.VFX;
using UnityEngine.Profiling;

namespace UnityEditor.VFX
{
    class VFXObject : ScriptableObject
    {
        //Explicitly disable the Reset option on all VFXObject
        //Internal Reset() behavior leads to a dandling state in graph object
        [MenuItem("CONTEXT/VFXObject/Reset", false)]
        public static void DummyReset()
        {
        }

        [MenuItem("CONTEXT/VFXObject/Reset", true)]
        static bool ValidateDummyReset()
        {
            return false;
        }

        public Action<VFXObject, bool> onModified;
        void OnValidate()
        {
            if (!VFXGraph.restoringGraph)
                Modified(false);
        }

        public void Modified(bool uiChange)
        {
            if (onModified != null)
                onModified(this, uiChange);
        }
    }

    [Serializable]
    abstract class VFXModel : VFXObject
    {
        public enum InvalidationCause
        {
            kStructureChanged,              // Model structure (hierarchy) has changed
            kParamChanged,                  // Some parameter values have changed
            kSettingChanged,                // A setting value has changed
            kSpaceChanged,                  // Space has been changed
            kConnectionChanged,             // Connection have changed
            kExpressionInvalidated,         // No direct change to the model but a change in connection was propagated from the parents
            kExpressionValueInvalidated,    // No direct change but a kParamChanged upstream was propagated meaning the expression in itself has not changed but it's evaluated value may have
            kExpressionGraphChanged,        // Expression graph must be recomputed
            kUIChanged,                     // UI stuff has changed
            kUIChangedTransient,            // UI stuff has been changed be does not require serialization
            kMaterialChanged,               // Some asset material properties has changed
            kEnableChanged,                 // Node has been enabled/disabled
            kInitValueChanged,              // A value has changed in a spawn/init context and may require a reinit
        }

        public new virtual string name => string.Empty;

        public delegate void InvalidateEvent(VFXModel model, InvalidationCause cause);

        public event InvalidateEvent onInvalidateDelegate;

        protected VFXModel()
        {
            m_UICollapsed = true;
        }

        public virtual void OnEnable()
        {
            if (m_Children == null)
                m_Children = new List<VFXModel>();
            else
            {
                int nbRemoved = m_Children.RemoveAll(c => c == null);// Remove bad references if any
                if (nbRemoved > 0)
                    Debug.LogWarning(String.Format("Remove {0} child(ren) that couldnt be deserialized from {1} of type {2}", nbRemoved, name, GetType()));
            }
        }

        public virtual void Sanitize(int version) { }

        public virtual void CheckGraphBeforeImport() { }

        public virtual void OnUnknownChange() { }

        public virtual void OnSRPChanged() { }

        public virtual void GetSourceDependentAssets(HashSet<string> dependencies)
        {
            foreach (var child in children)
                child.GetSourceDependentAssets(dependencies);
        }

        public virtual void GetImportDependentAssets(HashSet<int> dependencies)
        {
            foreach (var child in children)
            {
                child.GetImportDependentAssets(dependencies);
            }
        }

        public virtual void CollectDependencies(HashSet<ScriptableObject> objs, bool ownedOnly = true)
        {
            foreach (var child in children)
            {
                objs.Add(child);
                child.CollectDependencies(objs, ownedOnly);
            }
        }

        protected virtual void OnInvalidate(VFXModel model, InvalidationCause cause)
        {
            if (onInvalidateDelegate != null)
            {
                Profiler.BeginSample("VFXEditor.OnInvalidateDelegate");
                try
                {
                    onInvalidateDelegate(model, cause);
                }
                finally
                {
                    Profiler.EndSample();
                }
            }
        }

        public virtual void RefreshErrors()
        {
            GetGraph()?.errorManager.RefreshInvalidateReport(this);
        }

        protected virtual void OnAdded() { }
        protected virtual void OnRemoved() { }

        public virtual bool AcceptChild(VFXModel model, int index = -1)
        {
            return false;
        }

        public void AddChild(VFXModel model, int index = -1, bool notify = true)
        {
            int realIndex = index == -1 ? m_Children.Count : index;
            if (model.m_Parent != this || realIndex != GetIndex(model))
            {
                if (!AcceptChild(model, index))
                    throw new ArgumentException("Cannot attach " + model + " to " + this);

                model.Detach(notify && model.m_Parent != this); // Dont notify if the owner is already this to avoid double invalidation
                realIndex = index == -1 ? m_Children.Count : index; // Recompute as the child may have been removed

                m_Children.Insert(realIndex, model);
                model.m_Parent = this;
                model.OnAdded();

                if (notify)
                    Invalidate(InvalidationCause.kStructureChanged);
            }
        }

        public void RemoveChild(VFXModel model, bool notify = true)
        {
            if (model.m_Parent != this)
                return;

            model.OnRemoved();
            m_Children.Remove(model);
            model.m_Parent = null;

            if (notify)
                Invalidate(InvalidationCause.kStructureChanged);
        }

        public void RemoveAllChildren(bool notify = true)
        {
            while (m_Children.Count > 0)
                RemoveChild(m_Children[m_Children.Count - 1], notify);
        }

        public VFXModel GetParent()
        {
            return m_Parent;
        }

        public T GetFirstOfType<T>() where T : VFXModel
        {
            if (this is T)
                return this as T;

            var parent = GetParent();

            if (parent == null)
                return null;

            return parent.GetFirstOfType<T>();
        }

        public void Attach(VFXModel parent, bool notify = true)
        {
            parent.AddChild(this, -1, notify);
        }

        public void Detach(bool notify = true)
        {
            if (m_Parent == null)
                return;

            m_Parent.RemoveChild(this, notify);
        }

        public IEnumerable<VFXModel> children
        {
            get { return m_Children; }
        }

        public VFXModel this[int index]
        {
            get { return m_Children[index]; }
        }

        public Vector2 position
        {
            get { return m_UIPosition; }
            set
            {
                if (m_UIPosition != value)
                {
                    m_UIPosition = value;
                    Invalidate(InvalidationCause.kUIChanged);
                }
            }
        }

        public bool collapsed
        {
            get { return m_UICollapsed; }
            set
            {
                if (m_UICollapsed != value)
                {
                    m_UICollapsed = value;
                    Invalidate(InvalidationCause.kUIChanged);
                }
            }
        }

        public bool superCollapsed
        {
            get { return m_UISuperCollapsed; }
            set
            {
                if (m_UISuperCollapsed != value)
                {
                    m_UISuperCollapsed = value;
                    Invalidate(InvalidationCause.kUIChanged);
                }
            }
        }

        public int GetNbChildren()
        {
            return m_Children.Count;
        }

        public int GetIndex(VFXModel child)
        {
            return m_Children.IndexOf(child);
        }

        public object GetSettingValue(string name)
        {
            var setting = GetSetting(name);
            if (setting.field == null)
            {
                throw new ArgumentException(string.Format("Unable to find field {0} in {1}", name, GetType().ToString()));
            }
            return setting.value;
        }

        public void SetSettingValue(string name, object value)
        {
            SetSettingValue(name, value, true);
        }

        public void SetSettingValues(IEnumerable<KeyValuePair<string, object>> nameValues, bool notify = true)
        {
            bool hasChanged = false;
            foreach (var kvp in nameValues)
            {
                if (SetSettingValueAndReturnIfChanged(kvp.Key, kvp.Value))
                    hasChanged = true;
            }

            if (hasChanged && notify)
                Invalidate(InvalidationCause.kSettingChanged);
        }

        protected void SetSettingValue(string name, object value, bool notify)
        {
            bool hasChanged = SetSettingValueAndReturnIfChanged(name, value);
            if (hasChanged && notify)
                Invalidate(InvalidationCause.kSettingChanged);
        }

        private bool SetSettingValueAndReturnIfChanged(string name, object value)
        {
            var setting = GetSetting(name);
            if (setting.field == null)
            {
                throw new ArgumentException(string.Format("Unable to find field {0} in {1}", name, GetType().ToString()));
            }

            var currentValue = setting.value;
            var isUnityObject = setting.field.FieldType.IsSubclassOf(typeof(UnityEngine.Object));
            // Unity object Equals implementation consider null as equal which is not what we expect
            // Because then we cannot assign "None" (or null) anymore
            if ((isUnityObject && currentValue != value) || (!currentValue?.Equals(value) ?? value != null))
            {
                setting.field.SetValue(setting.instance, value);
                OnSettingModified(setting);
                if (setting.instance != (object)this)
                {
                    if (setting.instance is VFXModel model)
                        model.OnSettingModified(setting);
                }

                return true;
            }
            return false;
        }

        // Override this method to update other settings based on a setting modification
        // Use OnInvalidate with KSettingChanged and not this method to handle other side effects
        public virtual void OnSettingModified(VFXSetting setting) { }
        public virtual IEnumerable<int> GetFilteredOutEnumerators(string name) { return null; }

        public void Invalidate(InvalidationCause cause)
        {
            if (cause != InvalidationCause.kExpressionGraphChanged && cause != InvalidationCause.kExpressionInvalidated)
                Modified(cause == InvalidationCause.kUIChanged || cause == InvalidationCause.kUIChangedTransient);


            string sampleName = GetType().Name + "-" + name + "-" + cause;
            Profiler.BeginSample("VFXEditor.Invalidate" + sampleName);
            try
            {
                Invalidate(this, cause);
            }
            finally
            {
                Profiler.EndSample();
            }
        }

        [SerializeField]
        List<string> m_UIIgnoredErrors = new();


        public void IgnoreError(string error)
        {
            if (!m_UIIgnoredErrors.Contains(error))
                m_UIIgnoredErrors.Add(error);
        }

        public bool IsErrorIgnored(string error)
        {
            return m_UIIgnoredErrors.Contains(error);
        }

        public void ClearIgnoredErrors()
        {
            m_UIIgnoredErrors.Clear();
            RefreshErrors();
        }

        public bool HasIgnoredErrors()
        {
            return m_UIIgnoredErrors.Any();
        }

        internal virtual void GenerateErrors(VFXErrorReporter report)
        {
        }

        protected internal virtual void Invalidate(VFXModel model, InvalidationCause cause)
        {
            OnInvalidate(model, cause);
            if (m_Parent != null)
                m_Parent.Invalidate(model, cause);
            if (cause is InvalidationCause.kParamChanged or InvalidationCause.kExpressionValueInvalidated or InvalidationCause.kExpressionInvalidated or InvalidationCause.kSettingChanged)
                RefreshErrors();
        }

        private static Dictionary<Type, List<(FieldInfo field, VFXSettingAttribute attribute)>> s_CacheFieldByType;
        private static readonly BindingFlags kSettingsBindingFlag = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
        public static IEnumerable<(FieldInfo field, VFXSettingAttribute attribute)> GetFields(Type type)
        {
            s_CacheFieldByType ??= new();
            if (s_CacheFieldByType.TryGetValue(type, out var listOfSettings))
                return listOfSettings;

            listOfSettings = new List<(FieldInfo settings, VFXSettingAttribute attribute)>();
            foreach (var field in type.GetFields(kSettingsBindingFlag))
            {
                var attrArray = field.GetCustomAttributes(typeof(VFXSettingAttribute), true);
                    if (attrArray.Length == 1)
                    {
                        var attr = (VFXSettingAttribute)attrArray[0];
                    listOfSettings.Add((field, attr));
                }
            }
            s_CacheFieldByType.Add(type, listOfSettings);
            return listOfSettings;
        }

        public virtual VFXSetting GetSetting(string name)
        {
            return new VFXSetting(GetType().GetField(name, kSettingsBindingFlag), this);
        }

        protected static bool ShouldSettingBeListed(FieldInfo field, VFXSettingAttribute attribute, bool listHidden, VFXSettingAttribute.VisibleFlags flags, IEnumerable<string> filteredOutSettings)
        {
            return listHidden || (attribute.visibleFlags.HasFlag(flags)
                                  && !filteredOutSettings.Contains(field.Name));
        }

        public virtual IEnumerable<VFXSetting> GetSettings(bool listHidden, VFXSettingAttribute.VisibleFlags flags = VFXSettingAttribute.VisibleFlags.Default)
        {
            foreach (var settings in GetFields(GetType()))
            {
                if (ShouldSettingBeListed(settings.field, settings.attribute, listHidden, flags, filteredOutSettings))
                    yield return new VFXSetting(settings.field, this, settings.attribute.visibleFlags);
            }
        }

        static protected VFXExpression TransformExpression(VFXExpression input, SpaceableType dstSpaceType, VFXExpression matrix)
        {
            if (dstSpaceType == SpaceableType.Position)
            {
                input = new VFXExpressionTransformPosition(matrix, input);
            }
            else if (dstSpaceType == SpaceableType.Direction)
            {
                input = new VFXExpressionTransformDirection(matrix, input);
            }
            else if (dstSpaceType == SpaceableType.Matrix)
            {
                input = new VFXExpressionTransformMatrix(matrix, input);
            }
            else if (dstSpaceType == SpaceableType.Vector)
            {
                input = new VFXExpressionTransformVector(matrix, input);
            }
            else
            {
                //Not a transformable subSlot
            }

            return input;
        }

        static protected VFXExpression ConvertSpace(VFXExpression input, VFXSpace srcSpace, SpaceableType dstSpaceType, VFXSpace dstSpace)
        {
            if (dstSpace == VFXSpace.None || srcSpace == VFXSpace.None)
            {
                return input;
            }


            VFXExpression matrix = null;
            if (dstSpace == VFXSpace.Local)
            {
                matrix = VFXBuiltInExpression.WorldToLocal;
            }
            else if (dstSpace == VFXSpace.World)
            {
                matrix = VFXBuiltInExpression.LocalToWorld;
            }
            else
            {
                throw new InvalidOperationException("Cannot Convert to unknown space");
            }
            return TransformExpression(input, dstSpaceType, matrix);
        }

        static protected VFXExpression ConvertSpace(VFXExpression input, SpaceableType spaceType, VFXSpace space)
        {
            VFXExpression matrix = null;
            if (space == VFXSpace.Local)
            {
                matrix = VFXBuiltInExpression.WorldToLocal;
            }
            else if (space == VFXSpace.World)
            {
                matrix = VFXBuiltInExpression.LocalToWorld;
            }
            else
            {
                throw new InvalidOperationException("Cannot Convert to unknown space");
            }

            return TransformExpression(input, spaceType, matrix);
        }

        protected virtual IEnumerable<string> filteredOutSettings
        {
            get
            {
                return Enumerable.Empty<string>();
            }
        }

        public VisualEffectResource GetResource()
        {
            var graph = GetGraph();
            if (graph != null)
                return graph.visualEffectResource;
            return null;
        }

        public VFXGraph GetGraph()
        {
            switch (this)
            {
                case VFXGraph graph:
                    return graph;
                case VFXSlot { owner: VFXModel m }:
                   return m.GetGraph();
                case VFXData data:
                    return data.owners.FirstOrDefault()?.GetGraph();
                case { } m when m.GetParent() is { } parent:
                    return parent.GetGraph();
            }

            return null;
        }

        public IEnumerable<VFXModel> GetRecursiveChildren()
        {
            yield return this;

            foreach (var model in children.SelectMany(x => x.GetRecursiveChildren()))
            {
                yield return model;
            }
        }

        public static void UnlinkModel(VFXModel model, bool notify = true)
        {
            if (model is IVFXSlotContainer slotContainer)
            {
                VFXSlot slotToClean = null;
                do
                {
                    slotToClean = slotContainer.inputSlots.Concat(slotContainer.outputSlots).Append(slotContainer.activationSlot).FirstOrDefault(o => o != null && o.HasLink(true));
                    if (slotToClean)
                        slotToClean.UnlinkAll(true, notify);
                }
                while (slotToClean != null);
            }
        }

        public static void RemoveModel(VFXModel model, bool notify = true)
        {
            VFXGraph graph = model.GetGraph();
            if (graph != null)
                graph.UIInfos.Sanitize(graph); // Remove reference from groupInfos
            UnlinkModel(model);
            model.Detach(notify);
        }

        public static void ReplaceModel(VFXModel dst, VFXModel src, bool notify = true, bool unlink = true)
        {
            // UI
            dst.m_UIPosition = src.m_UIPosition;
            dst.m_UICollapsed = src.m_UICollapsed;
            dst.m_UISuperCollapsed = src.m_UISuperCollapsed;

            if (notify)
                dst.Invalidate(InvalidationCause.kUIChanged);

            VFXGraph graph = src.GetGraph();
            if (graph != null && graph.UIInfos != null && graph.UIInfos.groupInfos != null)
            {
                // Update group nodes
                foreach (var groupInfo in graph.UIInfos.groupInfos)
                    if (groupInfo.contents != null)
                        for (int i = 0; i < groupInfo.contents.Length; ++i)
                            if (groupInfo.contents[i].model == src)
                                groupInfo.contents[i].model = dst;
            }

            // Unlink everything
            if (unlink)
                UnlinkModel(src);

            // Replace model
            var parent = src.GetParent();
            int index = parent.GetIndex(src);
            src.Detach(notify);

            if (parent)
                parent.AddChild(dst, index, notify);
        }

        [SerializeField]
        protected VFXModel m_Parent;

        [SerializeField]
        protected List<VFXModel> m_Children;

        [SerializeField]
        protected Vector2 m_UIPosition;

        [SerializeField]
        protected bool m_UICollapsed;
        [SerializeField]
        protected bool m_UISuperCollapsed;
    }

    abstract class VFXModel<ParentType, ChildrenType> : VFXModel
        where ParentType : VFXModel
        where ChildrenType : VFXModel
    {
        public override bool AcceptChild(VFXModel model, int index = -1)
        {
            return index >= -1 && index <= m_Children.Count && model is ChildrenType;
        }

        public new ParentType GetParent()
        {
            return (ParentType)m_Parent;
        }

        public new int GetNbChildren()
        {
            return m_Children.Count;
        }

        public new ChildrenType this[int index]
        {
            get { return m_Children[index] as ChildrenType; }
        }

        public new IEnumerable<ChildrenType> children
        {
            get { return m_Children.Cast<ChildrenType>(); }
        }
    }
}
