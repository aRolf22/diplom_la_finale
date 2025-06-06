using System;
using System.Collections.Generic;
using System.IO;

using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

using Codice.Client.Commands;
using Codice.Client.Common;
using Codice.Client.Common.EventTracking;
using Codice.Client.Common.Threading;
using Codice.CM.Common;
using Codice.CM.Common.Mount;
using PlasticGui;
using PlasticGui.WorkspaceWindow;
using PlasticGui.WorkspaceWindow.BrowseRepository;
using PlasticGui.WorkspaceWindow.Diff;
using Unity.PlasticSCM.Editor.AssetUtils;
using Unity.PlasticSCM.Editor.Tool;
using Unity.PlasticSCM.Editor.UI;
using Unity.PlasticSCM.Editor.UI.Progress;
using Unity.PlasticSCM.Editor.UI.Tree;
using Unity.PlasticSCM.Editor.Views.Diff.Dialogs;
using Unity.PlasticSCM.Editor.Views.History;

namespace Unity.PlasticSCM.Editor.Views.Diff
{
    internal class DiffPanel :
        IDiffTreeViewMenuOperations,
        DiffTreeViewMenu.IMetaMenuOperations,
        UndeleteClientDiffsOperation.IGetRestorePathDialog
    {
        internal DiffPanel(
            WorkspaceInfo wkInfo,
            IWorkspaceWindow workspaceWindow,
            IRefreshView refreshView,
            IViewSwitcher viewSwitcher,
            IHistoryViewLauncher historyViewLauncher,
            LaunchTool.IShowDownloadPlasticExeWindow showDownloadPlasticExeWindow,
            EditorWindow parentWindow,
            bool isGluonMode)
        {
            mWkInfo = wkInfo;
            mWorkspaceWindow = workspaceWindow;
            mRefreshView = refreshView;
            mViewSwitcher = viewSwitcher;
            mHistoryViewLauncher = historyViewLauncher;
            mShowDownloadPlasticExeWindow = showDownloadPlasticExeWindow;
            mParentWindow = parentWindow;
            mGuiMessage = new UnityPlasticGuiMessage();
            mIsGluonMode = isGluonMode;

            BuildComponents();

            mProgressControls = new ProgressControlsForViews();
        }

        internal void ClearInfo()
        {
            ClearData();

            mParentWindow.Repaint();
        }

        internal void UpdateInfo(
            MountPointWithPath mountWithPath,
            ChangesetInfo csetInfo)
        {
            FillData(mountWithPath, csetInfo);

            mParentWindow.Repaint();
        }

        internal void OnEnable()
        {
            mSearchField.downOrUpArrowKeyPressed +=
                SearchField_OnDownOrUpArrowKeyPressed;
        }

        internal void OnDisable()
        {
            mSearchField.downOrUpArrowKeyPressed -=
                SearchField_OnDownOrUpArrowKeyPressed;
        }

        internal void Update()
        {
            mProgressControls.UpdateProgress(mParentWindow);
        }

        internal void OnGUI()
        {
            EditorGUILayout.BeginVertical();

            DoActionsToolbar(
                mDiffs,
                mDiffsBranchResolver,
                mProgressControls,
                mIsSkipMergeTrackingButtonVisible,
                mIsSkipMergeTrackingButtonChecked,
                mSearchField,
                mDiffTreeView);

            DoDiffTreeViewArea(
                mDiffTreeView,
                mEmptyStateContent,
                mProgressControls.IsOperationRunning());

            if (mProgressControls.HasNotification())
            {
                DrawProgressForViews.ForNotificationArea(
                    mProgressControls.ProgressData);
            }

            EditorGUILayout.EndVertical();
        }

        void IDiffTreeViewMenuOperations.SaveRevisionAs()
        {
            TrackFeatureUseEvent.For(
                PlasticGui.Plastic.API.GetRepositorySpec(mWkInfo),
                TrackFeatureUseEvent.Features.SaveRevisionFromDiff);

            ClientDiffInfo clientDiffInfo =
                DiffSelection.GetSelectedDiff(mDiffTreeView);
            RepositorySpec repSpec = clientDiffInfo.DiffWithMount.Mount.RepSpec;
            RevisionInfo revision = clientDiffInfo.DiffWithMount.Difference.RevInfo;

            string defaultFileName = DefaultRevisionName.Get(
                Path.GetFileName(clientDiffInfo.DiffWithMount.Difference.Path), revision.Changeset);
            string destinationPath = SaveAction.GetDestinationPath(
                mWkInfo.ClientPath,
                clientDiffInfo.DiffWithMount.Difference.Path,
                defaultFileName);

            if (string.IsNullOrEmpty(destinationPath))
                return;

            SaveRevisionOperation.SaveRevision(
                repSpec,
                destinationPath,
                revision,
                mProgressControls);
        }

        SelectedDiffsGroupInfo IDiffTreeViewMenuOperations.GetSelectedDiffsGroupInfo()
        {
            return SelectedDiffsGroupInfo.BuildFromSelectedNodes(
                DiffSelection.GetSelectedDiffsWithoutMeta(mDiffTreeView));
        }

        void IDiffTreeViewMenuOperations.Diff()
        {
            ClientDiffInfo clientDiffInfo =
                DiffSelection.GetSelectedDiff(mDiffTreeView);

            DiffOperation.DiffClientDiff(
                mWkInfo,
                clientDiffInfo.DiffWithMount.Mount.Mount,
                clientDiffInfo.DiffWithMount.Difference,
                PlasticExeLauncher.BuildForDiffRevision(mWkInfo, mIsGluonMode, mShowDownloadPlasticExeWindow),
                imageDiffLauncher: null);
        }

        void IDiffTreeViewMenuOperations.History()
        {
            ClientDiffInfo clientDiffInfo =
                DiffSelection.GetSelectedDiff(mDiffTreeView);

            mHistoryViewLauncher.ShowHistoryView(
                clientDiffInfo.DiffWithMount.Mount.RepSpec,
                clientDiffInfo.DiffWithMount.Difference.RevInfo.ItemId,
                clientDiffInfo.DiffWithMount.Difference.Path,
                clientDiffInfo.DiffWithMount.Difference.IsDirectory);
        }

        void IDiffTreeViewMenuOperations.RevertChanges()
        {
            RevertClientDiffsOperation.RevertChanges(
                mWkInfo,
                DiffSelection.GetSelectedDiffs(mDiffTreeView),
                mWorkspaceWindow,
                mProgressControls,
                mGuiMessage,
                AfterRevertOrUndeleteOperation);
        }

        void IDiffTreeViewMenuOperations.Undelete()
        {
            UndeleteClientDiffsOperation.Undelete(
                mWkInfo,
                DiffSelection.GetSelectedDiffs(mDiffTreeView),
                mRefreshView,
                mProgressControls,
                this,
                mGuiMessage,
                AfterRevertOrUndeleteOperation);
        }

        void IDiffTreeViewMenuOperations.UndeleteToSpecifiedPaths()
        {
            UndeleteClientDiffsOperation.UndeleteToSpecifiedPaths(
                mWkInfo,
                DiffSelection.GetSelectedDiffs(mDiffTreeView),
                mRefreshView,
                mProgressControls,
                this,
                mGuiMessage,
                AfterRevertOrUndeleteOperation);
        }

        void IDiffTreeViewMenuOperations.Annotate()
        {
        }

        void IDiffTreeViewMenuOperations.CopyFilePath(bool relativePath)
        {
            EditorGUIUtility.systemCopyBuffer = GetFilePathList.FromClientDiffInfos(
                DiffSelection.GetSelectedDiffsWithoutMeta(mDiffTreeView),
                relativePath,
                mWkInfo.ClientPath);
        }

        bool DiffTreeViewMenu.IMetaMenuOperations.SelectionHasMeta()
        {
            return mDiffTreeView.SelectionHasMeta();
        }

        void DiffTreeViewMenu.IMetaMenuOperations.DiffMeta()
        {
            ClientDiffInfo clientDiffInfo =
                DiffSelection.GetSelectedDiff(mDiffTreeView);

            ClientDiffInfo clientDiffInfoMeta =
                mDiffTreeView.GetMetaDiff(clientDiffInfo);

            DiffOperation.DiffClientDiff(
                mWkInfo,
                clientDiffInfoMeta.DiffWithMount.Mount.Mount,
                clientDiffInfoMeta.DiffWithMount.Difference,
                PlasticExeLauncher.BuildForDiffRevision(mWkInfo, mIsGluonMode, mShowDownloadPlasticExeWindow),
                imageDiffLauncher: null);
        }

        GetRestorePathData
            UndeleteClientDiffsOperation.IGetRestorePathDialog.GetRestorePath(
                string wkPath, string restorePath, string explanation,
                bool isDirectory, bool showSkipButton)
        {
            return GetRestorePathDialog.GetRestorePath(
                wkPath, restorePath, explanation, isDirectory,
                showSkipButton, mParentWindow);
        }

        void DiffTreeViewMenu.IMetaMenuOperations.HistoryMeta()
        {
            ClientDiffInfo clientDiffInfo =
                DiffSelection.GetSelectedDiff(mDiffTreeView);

            ClientDiffInfo clientDiffInfoMeta =
                mDiffTreeView.GetMetaDiff(clientDiffInfo);

            mHistoryViewLauncher.ShowHistoryView(
                clientDiffInfoMeta.DiffWithMount.Mount.RepSpec,
                clientDiffInfoMeta.DiffWithMount.Difference.RevInfo.ItemId,
                clientDiffInfoMeta.DiffWithMount.Difference.Path,
                clientDiffInfoMeta.DiffWithMount.Difference.IsDirectory);
        }

        void SearchField_OnDownOrUpArrowKeyPressed()
        {
            mDiffTreeView.SetFocusAndEnsureSelectedItem();
        }

        void AfterRevertOrUndeleteOperation()
        {
            RefreshAsset.UnityAssetDatabase();

            mViewSwitcher.ShowPendingChanges();
        }

        void ClearData()
        {
            mSelectedMountWithPath = null;
            mSelectedChangesetInfo = null;

            mDiffs = null;

            ClearDiffs();
        }

        void FillData(
            MountPointWithPath mountWithPath,
            ChangesetInfo csetInfo)
        {
            mSelectedMountWithPath = mountWithPath;
            mSelectedChangesetInfo = csetInfo;

            ((IProgressControls)mProgressControls).ShowProgress(
                PlasticLocalization.GetString(PlasticLocalization.Name.Loading));

            mIsSkipMergeTrackingButtonVisible = false;

            IThreadWaiter waiter = ThreadWaiter.GetWaiter(100);
            waiter.Execute(
                /*threadOperationDelegate*/ delegate
                {
                    mDiffs = PlasticGui.Plastic.API.GetChangesetDifferences(
                        mountWithPath, csetInfo);

                    mDiffsBranchResolver = BuildBranchResolver.ForDiffs(mDiffs);
                },
                /*afterOperationDelegate*/ delegate
                {
                    ((IProgressControls)mProgressControls).HideProgress();

                    if (waiter.Exception != null)
                    {
                        ExceptionsHandler.DisplayException(waiter.Exception);
                        return;
                    }

                    if (mSelectedMountWithPath != mountWithPath ||
                        mSelectedChangesetInfo != csetInfo)
                        return;

                    if (mDiffs == null || mDiffs.Count == 0)
                    {
                        ClearDiffs();
                        return;
                    }

                    mIsSkipMergeTrackingButtonVisible =
                        ClientDiffList.HasMerges(mDiffs);

                    bool skipMergeTracking =
                        mIsSkipMergeTrackingButtonVisible &&
                        mIsSkipMergeTrackingButtonChecked;

                    UpdateDiffTreeView(
                        mWkInfo,
                        mDiffs,
                        mDiffsBranchResolver,
                        skipMergeTracking,
                        mDiffTreeView);
                });
        }

        void ClearDiffs()
        {
            mIsSkipMergeTrackingButtonVisible = false;

            ClearDiffTreeView(mDiffTreeView);
        }

        static void ClearDiffTreeView(
            DiffTreeView diffTreeView)
        {
            diffTreeView.ClearModel();

            diffTreeView.Reload();
        }

        static void UpdateDiffTreeView(
            WorkspaceInfo wkInfo,
            List<ClientDiff> diffs,
            BranchResolver brResolver,
            bool skipMergeTracking,
            DiffTreeView diffTreeView)
        {
            diffTreeView.BuildModel(
                wkInfo, diffs, brResolver, skipMergeTracking);

            diffTreeView.Refilter();

            diffTreeView.Sort();

            diffTreeView.Reload();
        }

        void DoActionsToolbar(
            List<ClientDiff> diffs,
            BranchResolver brResolver,
            ProgressControlsForViews progressControls,
            bool isSkipMergeTrackingButtonVisible,
            bool isSkipMergeTrackingButtonChecked,
            SearchField searchField,
            DiffTreeView diffTreeView)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (progressControls.IsOperationRunning())
            {
                DrawProgressForViews.ForIndeterminateProgress(
                    progressControls.ProgressData);
            }

            GUILayout.FlexibleSpace();

            if (isSkipMergeTrackingButtonVisible)
            {
                DoSkipMergeTrackingButton(
                    diffs, brResolver,
                    isSkipMergeTrackingButtonChecked,
                    diffTreeView);
            }

            DrawSearchField.For(
                searchField,
                diffTreeView,
                UnityConstants.SEARCH_FIELD_WIDTH);

            EditorGUILayout.EndHorizontal();
        }

        void DoSkipMergeTrackingButton(
            List<ClientDiff> diffs,
            BranchResolver brResolver,
            bool isSkipMergeTrackingButtonChecked,
            DiffTreeView diffTreeView)
        {
            bool wasChecked = isSkipMergeTrackingButtonChecked;

            GUIContent buttonContent = new GUIContent(
                PlasticLocalization.GetString(
                    PlasticLocalization.Name.SkipDiffMergeTracking));

            GUIStyle buttonStyle = new GUIStyle(EditorStyles.toolbarButton);

            float buttonWidth = buttonStyle.CalcSize(buttonContent).x + 10;

            Rect toggleRect = GUILayoutUtility.GetRect(
                buttonContent, buttonStyle, GUILayout.Width(buttonWidth));

            bool isChecked = GUI.Toggle(
                toggleRect, wasChecked, buttonContent, buttonStyle);

            if (wasChecked == isChecked)
                return;

            // if user just checked the skip merge tracking button
            if (isChecked)
            {
                TrackFeatureUseEvent.For(
                    PlasticGui.Plastic.API.GetRepositorySpec(mWkInfo),
                    TrackFeatureUseEvent.Features.ChangesetViewSkipMergeTrackingButton);
            }

            UpdateDiffTreeView(mWkInfo, diffs, brResolver, isChecked, diffTreeView);

            mIsSkipMergeTrackingButtonChecked = isChecked;
        }

        static void DoDiffTreeViewArea(
            DiffTreeView diffTreeView,
            GUIContent emptyStateContent,
            bool isOperationRunning)
        {
            GUI.enabled = !isOperationRunning;

            Rect rect = GUILayoutUtility.GetRect(0, 100000, 0, 100000);

            diffTreeView.OnGUI(rect);

            emptyStateContent.text = GetEmptyStateMessage(diffTreeView);

            if (!string.IsNullOrEmpty(emptyStateContent.text))
                DrawTreeViewEmptyState.For(rect, emptyStateContent);

            GUI.enabled = true;
        }

        static string GetEmptyStateMessage(DiffTreeView diffTreeView)
        {
            if (diffTreeView.GetRows().Count > 0)
                return string.Empty;

            return string.IsNullOrEmpty(diffTreeView.searchString) ?
                PlasticLocalization.Name.NoContentToCompareExplanation.GetString() :
                PlasticLocalization.Name.DiffsEmptyState.GetString();
        }

        void BuildComponents()
        {
            mSearchField = new SearchField();
            mSearchField.downOrUpArrowKeyPressed += SearchField_OnDownOrUpArrowKeyPressed;

            mDiffTreeView = new DiffTreeView(new DiffTreeViewMenu(this, this));
            mDiffTreeView.Reload();
        }

        bool mIsSkipMergeTrackingButtonVisible;
        bool mIsSkipMergeTrackingButtonChecked;

        ChangesetInfo mSelectedChangesetInfo;
        MountPointWithPath mSelectedMountWithPath;

        volatile List<ClientDiff> mDiffs;
        volatile BranchResolver mDiffsBranchResolver;

        SearchField mSearchField;
        DiffTreeView mDiffTreeView;

        readonly GUIContent mEmptyStateContent = new GUIContent(string.Empty);
        readonly ProgressControlsForViews mProgressControls;
        readonly GuiMessage.IGuiMessage mGuiMessage;
        readonly EditorWindow mParentWindow;
        readonly IRefreshView mRefreshView;
        readonly IWorkspaceWindow mWorkspaceWindow;
        readonly IHistoryViewLauncher mHistoryViewLauncher;
        readonly IViewSwitcher mViewSwitcher;
        readonly LaunchTool.IShowDownloadPlasticExeWindow mShowDownloadPlasticExeWindow;
        readonly WorkspaceInfo mWkInfo;
        readonly bool mIsGluonMode;
    }
}
