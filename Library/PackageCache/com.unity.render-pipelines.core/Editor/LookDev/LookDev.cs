using UnityEngine.Rendering;
using UnityEngine.Rendering.LookDev;
using UnityEditorInternal;
using UnityEngine;
using System.Linq;

namespace UnityEditor.Rendering.LookDev
{
    /// <summary>
    /// Main entry point for scripting LookDev
    /// </summary>
    public static class LookDev
    {
        const string lastRenderingDataSavePath = "Library/LookDevConfig.asset";

        //TODO: ensure only one displayer at time for the moment
        static IViewDisplayer s_ViewDisplayer;
        static IEnvironmentDisplayer s_EnvironmentDisplayer;
        static Compositer s_Compositor;
        static StageCache s_Stages;
        static Context s_CurrentContext;

        internal static IDataProvider dataProvider
            => RenderPipelineManager.currentPipeline as IDataProvider;

        /// <summary>
        /// Get all the data used in LookDev currently (views, layout, debug... )
        /// </summary>
        internal static Context currentContext
        {
            //Lazy init: load it when needed instead in static even if you do not support lookdev
            get
            {
                if (s_CurrentContext == null || s_CurrentContext.Equals(null))
                {
                    // In case the context is still alive somewhere but the static reference has been lost, we can find the object back with this
                    s_CurrentContext = TryFindCurrentContext();
                    if (s_CurrentContext != null)
                        return s_CurrentContext;

                    s_CurrentContext = LoadConfigInternal();
                    if (s_CurrentContext == null)
                        s_CurrentContext = defaultContext;

                    ReloadStage(false);
                }
                return s_CurrentContext;
            }
            private set => s_CurrentContext = value;
        }

        static Context TryFindCurrentContext()
        {
            Context context = s_CurrentContext;

            if (context != null)
                return context;

            return Resources.FindObjectsOfTypeAll<Context>().FirstOrDefault();
        }

        static Context defaultContext
        {
            get
            {
                var context = UnityEngine.ScriptableObject.CreateInstance<Context>();
                context.hideFlags = HideFlags.HideAndDontSave;
                context.Init();
                return context;
            }
        }

        //[TODO: not compatible with multiple displayer. To rework if needed]
        internal static IViewDisplayer currentViewDisplayer => s_ViewDisplayer;
        internal static IEnvironmentDisplayer currentEnvironmentDisplayer => s_EnvironmentDisplayer;

        [MenuItem("Window/Rendering/Look Dev", false, 10001)]
        static void OpenLookDev() => Open();

        [MenuItem("Window/Rendering/Look Dev", true, 10001)]
        static bool LookDevAvailable() => supported;

        internal static bool waitingConfigure { get; private set; } = true;

        /// <summary>State of the LookDev window</summary>
        public static bool open { get; private set; }

        /// <summary>
        /// Does LookDev is supported with the current render pipeline?
        /// </summary>
        public static bool supported => dataProvider != null;

        /// <summary>
        /// Reset all LookDevs datas to the default configuration
        /// </summary>
        public static void ResetConfig()
            => currentContext = defaultContext;

        static Context LoadConfigInternal(string path = lastRenderingDataSavePath)
        {
            var objs = InternalEditorUtility.LoadSerializedFileAndForget(path);
            Context context = (objs.Length > 0 ? objs[0] : null) as Context;
            if (context != null && !context.Equals(null))
                context.Init();
            return context;
        }

        /// <summary>
        /// Load a different set of datas
        /// </summary>
        /// <param name="path">Path where to load</param>
        internal static void LoadConfig(string path = lastRenderingDataSavePath)
        {
            var last = LoadConfigInternal(path);
            if (last != null)
                currentContext = last;
        }

        /// <summary>
        /// Save the current set of datas
        /// </summary>
        /// <param name="path">[optional] Path to save. By default, saved in Library folder</param>
        internal static void SaveConfig(string path = lastRenderingDataSavePath)
        {
            if (currentContext != null && !currentContext.Equals(null))
                InternalEditorUtility.SaveToSerializedFileAndForget(new[] { currentContext }, path, true);
        }

        /// <summary>Open the LookDev window</summary>
        public static void Open()
        {
            EditorWindow.GetWindow<DisplayWindow>();
        }

        /// <summary>Close the LookDev window</summary>
        public static void Close()
        {
            (s_ViewDisplayer as EditorWindow)?.Close();
            s_ViewDisplayer = null;
            (s_EnvironmentDisplayer as EditorWindow)?.Close();
            s_EnvironmentDisplayer = null;
        }

        internal static void Initialize(DisplayWindow window)
        {
            s_ViewDisplayer = window;
            s_EnvironmentDisplayer = window;
            open = true;

            // Lookdev Initialize can be called when the window is re-created by the editor layout system.
            // In that case, the current context won't be null and there might be objects to reload from the temp ID
            ConfigureLookDev(reloadWithTemporaryID: TryFindCurrentContext() != null);
        }

        [Callbacks.DidReloadScripts]
        static void OnEditorReload()
        {
            var windows = Resources.FindObjectsOfTypeAll<DisplayWindow>();
            s_ViewDisplayer = windows.Length > 0 ? windows[0] : null;
            s_EnvironmentDisplayer = windows.Length > 0 ? windows[0] : null;
            open = s_ViewDisplayer != null;
            if (open)
                ConfigureLookDev(reloadWithTemporaryID: true);
        }

        static void ConfigureLookDev(bool reloadWithTemporaryID)
        {
            open = true;
            waitingConfigure = true;
            if (s_CurrentContext == null || s_CurrentContext.Equals(null))
                LoadConfig();
            WaitingSRPReloadForConfiguringRenderer(5, reloadWithTemporaryID: reloadWithTemporaryID);
        }

        static void WaitingSRPReloadForConfiguringRenderer(int maxAttempt, bool reloadWithTemporaryID, int attemptNumber = 0)
        {
            if (supported)
            {
                waitingConfigure = false;
                ConfigureRenderer(reloadWithTemporaryID);
                LinkViewDisplayer();
                LinkEnvironmentDisplayer();
                ReloadStage(reloadWithTemporaryID);
            }
            else if (attemptNumber < maxAttempt)
                EditorApplication.delayCall +=
                    () => WaitingSRPReloadForConfiguringRenderer(maxAttempt, reloadWithTemporaryID, ++attemptNumber);
            else
                waitingConfigure = false;
        }

        static void ConfigureRenderer(bool reloadWithTemporaryID)
        {
            s_Stages?.Dispose(); //clean previous occurrence on reloading
            s_Stages = new StageCache(dataProvider);
            s_Compositor?.Dispose(); //clean previous occurrence on reloading
            s_Compositor = new Compositer(s_ViewDisplayer, dataProvider, s_Stages);
        }

        static void LinkViewDisplayer()
        {
            s_ViewDisplayer.OnClosed += () =>
            {
                s_Compositor?.Dispose();
                s_Compositor = null;
                s_Stages?.Dispose();
                s_Stages = null;
                s_ViewDisplayer = null;
                //currentContext = null;

                SaveConfig();

                open = false;
            };
            s_ViewDisplayer.OnLayoutChanged += (layout, envPanelOpen) =>
            {
                currentContext.layout.viewLayout = layout;
                currentContext.layout.showedSidePanel = envPanelOpen;
                SaveConfig();
            };
            s_ViewDisplayer.OnChangingObjectInView += (go, index, localPos) =>
            {
                switch (index)
                {
                    case ViewCompositionIndex.First:
                    case ViewCompositionIndex.Second:
                        currentContext.GetViewContent((ViewIndex)index).UpdateViewedObject(go);
                        SaveContextChangeAndApply((ViewIndex)index);
                        break;
                    case ViewCompositionIndex.Composite:
                        ViewIndex viewIndex = s_Compositor.GetViewFromComposition(localPos);
                        currentContext.GetViewContent(viewIndex).UpdateViewedObject(go);
                        SaveContextChangeAndApply(viewIndex);
                        break;
                }
            };
            s_ViewDisplayer.OnChangingEnvironmentInView += (obj, index, localPos) =>
            {
                switch (index)
                {
                    case ViewCompositionIndex.First:
                    case ViewCompositionIndex.Second:
                        currentContext.GetViewContent((ViewIndex)index).UpdateEnvironment(obj);
                        SaveContextChangeAndApply((ViewIndex)index);
                        break;
                    case ViewCompositionIndex.Composite:
                        ViewIndex viewIndex = s_Compositor.GetViewFromComposition(localPos);
                        currentContext.GetViewContent(viewIndex).UpdateEnvironment(obj);
                        SaveContextChangeAndApply(viewIndex);
                        break;
                }
            };
        }

        static void LinkEnvironmentDisplayer()
        {
            s_EnvironmentDisplayer.OnChangingEnvironmentLibrary += UpdateEnvironmentLibrary;
        }

        static void UpdateEnvironmentLibrary(EnvironmentLibrary library)
        {
            LookDev.currentContext.UpdateEnvironmentLibrary(library);
        }

        static void ReloadStage(bool reloadWithTemporaryID)
        {
            currentContext.GetViewContent(ViewIndex.First).LoadAll(reloadWithTemporaryID);
            ApplyContextChange(ViewIndex.First);
            currentContext.GetViewContent(ViewIndex.Second).LoadAll(reloadWithTemporaryID);
            ApplyContextChange(ViewIndex.Second);
        }

        static void ApplyContextChange(ViewIndex index)
        {
            s_Stages.UpdateSceneObjects(index);
            s_Stages.UpdateSceneLighting(index, dataProvider);
            s_ViewDisplayer.Repaint();
        }

        /// <summary>Update the rendered element with element in the context</summary>
        /// <param name="index">The index of the stage to update</param>
        internal static void SaveContextChangeAndApply(ViewIndex index)
        {
            SaveConfig();
            ApplyContextChange(index);
        }
    }
}
