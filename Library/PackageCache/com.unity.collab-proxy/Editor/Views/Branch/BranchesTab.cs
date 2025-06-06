using System;
using System.Collections.Generic;

using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

using Codice.Client.Common.Threading;
using Codice.CM.Common;
using GluonGui;
using PlasticGui;
using PlasticGui.WorkspaceWindow;
using PlasticGui.WorkspaceWindow.CodeReview;
using PlasticGui.WorkspaceWindow.QueryViews;
using PlasticGui.WorkspaceWindow.QueryViews.Branches;
using PlasticGui.WorkspaceWindow.Update;
using Unity.PlasticSCM.Editor.AssetUtils.Processor;
using Unity.PlasticSCM.Editor.Tool;
using Unity.PlasticSCM.Editor.UI;
using Unity.PlasticSCM.Editor.UI.Progress;
using Unity.PlasticSCM.Editor.UI.Tree;
using Unity.PlasticSCM.Editor.Views.Branches.Dialogs;
using Unity.PlasticSCM.Editor.Views.Changesets;

using GluonNewIncomingChangesUpdater = PlasticGui.Gluon.WorkspaceWindow.NewIncomingChangesUpdater;
using IGluonUpdateReport = PlasticGui.Gluon.IUpdateReport;

namespace Unity.PlasticSCM.Editor.Views.Branches
{
    internal partial class BranchesTab :
        IRefreshableView,
        IQueryRefreshableView,
        IBranchMenuOperations,
        ILaunchCodeReviewWindow
    {
        internal BranchesListView Table { get { return mBranchesListView; } }
        internal IBranchMenuOperations Operations { get { return this; } }

        internal BranchesTab(
            WorkspaceInfo wkInfo,
            WorkspaceWindow workspaceWindow,
            IViewSwitcher viewSwitcher,
            IMergeViewLauncher mergeViewLauncher,
            ViewHost viewHost,
            IUpdateReport updateReport,
            IGluonUpdateReport gluonUpdateReport,
            NewIncomingChangesUpdater developerNewIncomingChangesUpdater,
            GluonNewIncomingChangesUpdater gluonNewIncomingChangesUpdater,
            IShelvedChangesUpdater shelvedChangesUpdater,
            LaunchTool.IShowDownloadPlasticExeWindow showDownloadPlasticExeWindow,
            LaunchTool.IProcessExecutor processExecutor,
            WorkspaceOperationsMonitor workspaceOperationsMonitor,
            EditorWindow parentWindow,
            bool isGluonMode)
        {
            mWkInfo = wkInfo;
            mParentWindow = parentWindow;
            mGluonUpdateReport = gluonUpdateReport;
            mViewHost = viewHost;
            mWorkspaceWindow = workspaceWindow;
            mIsGluonMode = isGluonMode;
            mProgressControls = new ProgressControlsForViews();

            mDeveloperNewIncomingChangesUpdater = developerNewIncomingChangesUpdater;
            mGluonNewIncomingChangesUpdater = gluonNewIncomingChangesUpdater;
            mShelvedChangesUpdater = shelvedChangesUpdater;
            mShowDownloadPlasticExeWindow = showDownloadPlasticExeWindow;
            mProcessExecutor = processExecutor;
            mWorkspaceOperationsMonitor = workspaceOperationsMonitor;
            mShelvePendingChangesQuestionerBuilder =
                new ShelvePendingChangesQuestionerBuilder(parentWindow);
            mEnableSwitchAndShelveFeatureDialog = new EnableSwitchAndShelveFeature(
                PlasticGui.Plastic.API.GetRepositorySpec(mWkInfo),
                mParentWindow);

            BuildComponents(
                wkInfo,
                workspaceWindow,
                viewSwitcher,
                mergeViewLauncher,
                updateReport,
                developerNewIncomingChangesUpdater,
                shelvedChangesUpdater,
                mShelvePendingChangesQuestionerBuilder,
                mEnableSwitchAndShelveFeatureDialog,
                parentWindow);

            ((IRefreshableView)this).Refresh();
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

            TreeHeaderSettings.Save(
                mBranchesListView.multiColumnHeader.state,
                UnityConstants.BRANCHES_TABLE_SETTINGS_NAME);
        }

        internal void Update()
        {
            mProgressControls.UpdateProgress(mParentWindow);
        }

        internal void OnGUI()
        {
            DoActionsToolbar(mProgressControls);

            DoBranchesArea(
                mBranchesListView,
                mProgressControls.IsOperationRunning());
        }

        internal void DrawSearchFieldForTab()
        {
            DrawSearchField.For(
                mSearchField,
                mBranchesListView,
                UnityConstants.SEARCH_FIELD_WIDTH);
        }

        internal void DrawDateFilter()
        {
            GUI.enabled = !mProgressControls.IsOperationRunning();

            EditorGUI.BeginChangeCheck();

            mDateFilter.FilterType = (DateFilter.Type)
                EditorGUILayout.EnumPopup(
                    mDateFilter.FilterType,
                    EditorStyles.toolbarDropDown,
                    GUILayout.Width(100));

            if (EditorGUI.EndChangeCheck())
            {
                EnumPopupSetting<DateFilter.Type>.Save(
                    mDateFilter.FilterType,
                    UnityConstants.BRANCHES_DATE_FILTER_SETTING_NAME);

                ((IRefreshableView)this).Refresh();
            }

            GUI.enabled = true;
        }

        internal void SetWorkingObjectInfo(WorkingObjectInfo homeInfo)
        {
            lock (mLock)
            {
                mLoadedBranchId = homeInfo.BranchInfo.BranchId;
            }

            mBranchesListView.SetLoadedBranchId(mLoadedBranchId);
        }

        void IRefreshableView.Refresh()
        {
            // VCS-1005209 - There are scenarios where the list of branches need to check for incoming changes.
            // For example, deleting the active branch will automatically switch your workspace to the parent changeset,
            // which might have incoming changes.
            if (mDeveloperNewIncomingChangesUpdater != null)
                mDeveloperNewIncomingChangesUpdater.Update(DateTime.Now);

            if (mGluonNewIncomingChangesUpdater != null)
                mGluonNewIncomingChangesUpdater.Update(DateTime.Now);

            string query = GetBranchesQuery(mDateFilter);

            FillBranches(mWkInfo, query, BranchesSelection.
                GetSelectedRepObjectInfos(mBranchesListView));
        }

        //IQueryRefreshableView
        public void RefreshAndSelect(RepObjectInfo repObj)
        {
            string query = GetBranchesQuery(mDateFilter);

            FillBranches(mWkInfo, query, new List<RepObjectInfo> { repObj });
        }

        int IBranchMenuOperations.GetSelectedBranchesCount()
        {
            return BranchesSelection.GetSelectedBranchesCount(mBranchesListView);
        }

        bool IBranchMenuOperations.AreHiddenBranchesShown()
        {
            return false;
        }

        void IBranchMenuOperations.CreateBranch()
        {
            CreateBranchForMode();
        }

        void IBranchMenuOperations.CreateTopLevelBranch() { }

        void IBranchMenuOperations.SwitchToBranch()
        {
            SwitchToBranchForMode();
        }

        void IBranchMenuOperations.MergeBranch()
        {
            mBranchOperations.MergeBranch(
                BranchesSelection.GetSelectedRepository(mBranchesListView),
                BranchesSelection.GetSelectedBranch(mBranchesListView));
        }

        void IBranchMenuOperations.CherrypickBranch() { }

        void IBranchMenuOperations.MergeToBranch() { }

        void IBranchMenuOperations.PullBranch() { }

        void IBranchMenuOperations.PullRemoteBranch() { }

        void IBranchMenuOperations.SyncWithGit() { }

        void IBranchMenuOperations.PushBranch() { }

        void IBranchMenuOperations.DiffBranch() { }

        void IBranchMenuOperations.DiffWithAnotherBranch() { }

        void IBranchMenuOperations.ViewChangesets() { }

        void IBranchMenuOperations.RenameBranch()
        {
            RepositorySpec repSpec = BranchesSelection.GetSelectedRepository(mBranchesListView);
            BranchInfo branchInfo = BranchesSelection.GetSelectedBranch(mBranchesListView);

            BranchRenameData branchRenameData = RenameBranchDialog.GetBranchRenameData(
                repSpec,
                branchInfo,
                mParentWindow);

            mBranchOperations.RenameBranch(branchRenameData);
        }

        void IBranchMenuOperations.HideUnhideBranch()
        {
        }

        void IBranchMenuOperations.DeleteBranch()
        {
            var branchesToDelete = BranchesSelection.GetSelectedBranches(mBranchesListView);

            if (!DeleteBranchDialog.ConfirmDelete(branchesToDelete))
                return;

            mBranchOperations.DeleteBranch(
                BranchesSelection.GetSelectedRepositories(mBranchesListView),
                branchesToDelete,
                DeleteBranchOptions.IncludeChangesets,
                false);
        }

        void IBranchMenuOperations.CreateCodeReview()
        {
            RepositorySpec repSpec = BranchesSelection.GetSelectedRepository(mBranchesListView);
            BranchInfo branchInfo = BranchesSelection.GetSelectedBranch(mBranchesListView);

            NewCodeReviewBehavior choice = SelectNewCodeReviewBehavior.For(repSpec.Server);

            switch (choice)
            {
                case NewCodeReviewBehavior.CreateAndOpenInDesktop:
                    mBranchOperations.CreateCodeReview(repSpec, branchInfo, this);
                    break;
                case NewCodeReviewBehavior.RequestFromUnityCloud:
                    OpenRequestReviewPage.ForBranch(repSpec, branchInfo.BranchId);
                    break;
                case NewCodeReviewBehavior.Ask:
                default:
                    break;
            }
        }

        void ILaunchCodeReviewWindow.Show(
            WorkspaceInfo wkInfo,
            RepositorySpec repSpec,
            ReviewInfo reviewInfo,
            RepObjectInfo repObjectInfo,
            bool bShowReviewChangesTab)
        {
            LaunchTool.OpenCodeReview(
                mShowDownloadPlasticExeWindow,
                mProcessExecutor,
                repSpec,
                reviewInfo.Id,
                mIsGluonMode);
        }

        void IBranchMenuOperations.ViewPermissions() { }

        void SearchField_OnDownOrUpArrowKeyPressed()
        {
            mBranchesListView.SetFocusAndEnsureSelectedItem();
        }

        void OnBranchesListViewSizeChanged()
        {
            if (!mShouldScrollToSelection)
                return;

            mShouldScrollToSelection = false;
            TableViewOperations.ScrollToSelection(mBranchesListView);
        }

        void FillBranches(
            WorkspaceInfo wkInfo,
            string query,
            List<RepObjectInfo> branchesToSelect)
        {
            if (mIsRefreshing)
                return;

            mIsRefreshing = true;

            int defaultRow = TableViewOperations.
                GetFirstSelectedRow(mBranchesListView);

            ((IProgressControls)mProgressControls).ShowProgress(
                PlasticLocalization.GetString(
                    PlasticLocalization.Name.LoadingBranches));

            ViewQueryResult queryResult = null;

            IThreadWaiter waiter = ThreadWaiter.GetWaiter();
            waiter.Execute(
                /*threadOperationDelegate*/ delegate
                {
                    long loadedBranchId = GetLoadedBranchId(wkInfo);
                    lock(mLock)
                    {
                        mLoadedBranchId = loadedBranchId;
                    }

                    queryResult = new ViewQueryResult(
                        PlasticGui.Plastic.API.FindQuery(wkInfo, query));
                },
                /*afterOperationDelegate*/ delegate
                {
                    try
                    {
                        if (waiter.Exception != null)
                        {
                            ExceptionsHandler.DisplayException(waiter.Exception);
                            return;
                        }

                        UpdateBranchesList(
                            mBranchesListView,
                            queryResult,
                            mLoadedBranchId);

                        int branchesCount = GetBranchesCount(queryResult);

                        if (branchesCount == 0)
                        {
                            return;
                        }

                        BranchesSelection.SelectBranches(
                            mBranchesListView, branchesToSelect, defaultRow);
                    }
                    finally
                    {
                        ((IProgressControls)mProgressControls).HideProgress();
                        mIsRefreshing = false;
                    }
                });
        }

        static void UpdateBranchesList(
             BranchesListView branchesListView,
             ViewQueryResult queryResult,
             long loadedBranchId)
        {
            branchesListView.BuildModel(
                queryResult, loadedBranchId);

            branchesListView.Refilter();

            branchesListView.Sort();

            branchesListView.Reload();
        }

        static long GetLoadedBranchId(WorkspaceInfo wkInfo)
        {
            BranchInfo brInfo = PlasticGui.Plastic.API.GetWorkingBranch(wkInfo);

            if (brInfo != null)
                return brInfo.BranchId;

            return -1;
        }

        static int GetBranchesCount(
            ViewQueryResult queryResult)
        {
            if (queryResult == null)
                return 0;

            return queryResult.Count();
        }

        static string GetBranchesQuery(DateFilter dateFilter)
        {
            if (dateFilter.FilterType == DateFilter.Type.AllTime)
                return QueryConstants.BranchesBeginningQuery;

            string whereClause = QueryConstants.GetDateWhereClause(
                dateFilter.GetTimeAgo());

            return string.Format("{0} {1}",
                QueryConstants.BranchesBeginningQuery,
                whereClause);
        }

        static void DoActionsToolbar(ProgressControlsForViews progressControls)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            if (progressControls.IsOperationRunning())
            {
                DrawProgressForViews.ForIndeterminateProgress(
                    progressControls.ProgressData);
            }

            GUILayout.FlexibleSpace();

            EditorGUILayout.EndHorizontal();
        }

        static void DoBranchesArea(
            BranchesListView branchesListView,
            bool isOperationRunning)
        {
            EditorGUILayout.BeginVertical();

            GUI.enabled = !isOperationRunning;

            Rect rect = GUILayoutUtility.GetRect(0, 100000, 0, 100000);

            branchesListView.OnGUI(rect);

            GUI.enabled = true;

            EditorGUILayout.EndVertical();
        }

        void BuildComponents(
            WorkspaceInfo wkInfo,
            IWorkspaceWindow workspaceWindow,
            IViewSwitcher viewSwitcher,
            IMergeViewLauncher mergeViewLauncher,
            IUpdateReport updateReport,
            NewIncomingChangesUpdater developerNewIncomingChangesUpdater,
            IShelvedChangesUpdater shelvedChangesUpdater,
            IShelvePendingChangesQuestionerBuilder shelvePendingChangesQuestionerBuilder,
            SwitchAndShelve.IEnableSwitchAndShelveFeatureDialog enableSwitchAndShelveFeatureDialog,
            EditorWindow parentWindow)
        {
            mSearchField = new SearchField();
            mSearchField.downOrUpArrowKeyPressed += SearchField_OnDownOrUpArrowKeyPressed;

            DateFilter.Type dateFilterType =
                EnumPopupSetting<DateFilter.Type>.Load(
                    UnityConstants.BRANCHES_DATE_FILTER_SETTING_NAME,
                    DateFilter.Type.LastMonth);
            mDateFilter = new DateFilter(dateFilterType);

            BranchesListHeaderState headerState =
                BranchesListHeaderState.GetDefault();

            TreeHeaderSettings.Load(headerState,
                UnityConstants.BRANCHES_TABLE_SETTINGS_NAME,
                (int)BranchesListColumn.CreationDate, false);

            mBranchesListView = new BranchesListView(
                headerState,
                BranchesListHeaderState.GetColumnNames(),
                new BranchesViewMenu(this, mGluonNewIncomingChangesUpdater != null),
                sizeChangedAction: OnBranchesListViewSizeChanged);

            mBranchesListView.Reload();

            mBranchOperations = new BranchOperations(
                wkInfo,
                workspaceWindow,
                mergeViewLauncher,
                this,
                ViewType.BranchesView,
                mProgressControls,
                updateReport,
                new ContinueWithPendingChangesQuestionerBuilder(viewSwitcher, parentWindow),
                shelvePendingChangesQuestionerBuilder,
                new ApplyShelveWithConflictsQuestionerBuilder(),
                developerNewIncomingChangesUpdater,
                shelvedChangesUpdater,
                enableSwitchAndShelveFeatureDialog);
        }

        SearchField mSearchField;
        bool mIsRefreshing;

        DateFilter mDateFilter;
        bool mShouldScrollToSelection;
        BranchesListView mBranchesListView;
        BranchOperations mBranchOperations;

        long mLoadedBranchId = -1;
        object mLock = new object();

        LaunchTool.IProcessExecutor mProcessExecutor;
        LaunchTool.IShowDownloadPlasticExeWindow mShowDownloadPlasticExeWindow;

        readonly WorkspaceOperationsMonitor mWorkspaceOperationsMonitor;
        readonly bool mIsGluonMode;
        readonly ViewHost mViewHost;
        readonly IGluonUpdateReport mGluonUpdateReport;
        readonly WorkspaceWindow mWorkspaceWindow;

        readonly WorkspaceInfo mWkInfo;
        readonly ProgressControlsForViews mProgressControls;
        readonly EditorWindow mParentWindow;
        readonly NewIncomingChangesUpdater mDeveloperNewIncomingChangesUpdater;
        readonly GluonNewIncomingChangesUpdater mGluonNewIncomingChangesUpdater;
        readonly IShelvePendingChangesQuestionerBuilder mShelvePendingChangesQuestionerBuilder;
        readonly IShelvedChangesUpdater mShelvedChangesUpdater;
        readonly SwitchAndShelve.IEnableSwitchAndShelveFeatureDialog mEnableSwitchAndShelveFeatureDialog;
    }
}
