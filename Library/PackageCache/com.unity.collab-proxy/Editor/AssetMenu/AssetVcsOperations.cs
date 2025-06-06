using System;
using System.IO;
using System.Collections.Generic;

using UnityEditor;
using UnityEditor.VersionControl;

using Codice.Client.BaseCommands;
using Codice.Client.Commands;
using Codice.Client.Commands.WkTree;
using Codice.Client.Common;
using Codice.Client.Common.EventTracking;
using Codice.Client.Common.Threading;
using Codice.CM.Common;
using GluonGui;
using PlasticGui;
using PlasticGui.Gluon;
using PlasticGui.WorkspaceWindow;
using PlasticGui.WorkspaceWindow.Diff;
using PlasticGui.WorkspaceWindow.Items;
using Unity.PlasticSCM.Editor.AssetMenu.Dialogs;
using Unity.PlasticSCM.Editor.AssetsOverlays.Cache;
using Unity.PlasticSCM.Editor.AssetUtils;
using Unity.PlasticSCM.Editor.AssetUtils.Processor;
using Unity.PlasticSCM.Editor.Tool;
using Unity.PlasticSCM.Editor.UI;
using Unity.PlasticSCM.Editor.Views.PendingChanges.Dialogs;

using GluonCheckoutOperation = GluonGui.WorkspaceWindow.Views.WorkspaceExplorer.Explorer.Operations.CheckoutOperation;
using GluonUndoCheckoutOperation = GluonGui.WorkspaceWindow.Views.WorkspaceExplorer.Explorer.Operations.UndoCheckoutOperation;
using GluonAddoperation = GluonGui.WorkspaceWindow.Views.WorkspaceExplorer.Explorer.Operations.AddOperation;

namespace Unity.PlasticSCM.Editor.AssetMenu
{
    internal class AssetVcsOperations :
        IAssetMenuVCSOperations,
        IAssetFilesFilterPatternsMenuOperations
    {
        internal interface IAssetSelection
        {
            AssetList GetSelectedAssets();
        }

        internal AssetVcsOperations(
            WorkspaceInfo wkInfo,
            IWorkspaceWindow workspaceWindow,
            IViewSwitcher viewSwitcher,
            IHistoryViewLauncher historyViewLauncher,
            ViewHost viewHost,
            WorkspaceOperationsMonitor workspaceOperationsMonitor,
            NewIncomingChangesUpdater newIncomingChangesUpdater,
            ShelvedChangesUpdater shelvedChangesUpdater,
            IAssetStatusCache assetStatusCache,
            IMergeViewLauncher mergeViewLauncher,
            IGluonViewSwitcher gluonViewSwitcher,
            IAssetSelection assetSelection,
            LaunchTool.IShowDownloadPlasticExeWindow showDownloadPlasticExeWindow,
            bool isGluonMode)
        {
            mWkInfo = wkInfo;
            mWorkspaceWindow = workspaceWindow;
            mViewSwitcher = viewSwitcher;
            mHistoryViewLauncher = historyViewLauncher;
            mViewHost = viewHost;
            mWorkspaceOperationsMonitor = workspaceOperationsMonitor;
            mNewIncomingChangesUpdater = newIncomingChangesUpdater;
            mShelvedChangesUpdater = shelvedChangesUpdater;
            mAssetStatusCache = assetStatusCache;
            mMergeViewLauncher = mergeViewLauncher;
            mGluonViewSwitcher = gluonViewSwitcher;
            mAssetSelection = assetSelection;
            mShowDownloadPlasticExeWindow = showDownloadPlasticExeWindow;
            mIsGluonMode = isGluonMode;

            mGuiMessage = new UnityPlasticGuiMessage();
            mProgressControls = new EditorProgressControls(mGuiMessage);
        }

        void IAssetMenuVCSOperations.ShowPendingChanges()
        {
            mViewSwitcher.ShowPendingChanges();
        }

        void IAssetMenuVCSOperations.Add()
        {
            List<string> selectedPaths = GetSelectedPaths.ForOperation(
                mWkInfo.ClientPath,
                mAssetSelection.GetSelectedAssets(),
                mAssetStatusCache,
                AssetMenuOperations.Add);

            if (mIsGluonMode)
            {
                GluonAddoperation.Add(
                    mViewHost,
                    mProgressControls,
                    mGuiMessage,
                    selectedPaths.ToArray(),
                    false,
                    RefreshAsset.VersionControlCache);
                return;
            }

            AddOperation.Run(
                mWorkspaceWindow,
                mProgressControls,
                null,
                null,
                selectedPaths,
                false,
                mNewIncomingChangesUpdater,
                mShelvedChangesUpdater,
                RefreshAsset.VersionControlCache);
        }

        void IAssetMenuVCSOperations.Checkout()
        {
            List<string> selectedPaths = GetSelectedPaths.ForOperation(
                mWkInfo.ClientPath,
                mAssetSelection.GetSelectedAssets(),
                mAssetStatusCache,
                AssetMenuOperations.Checkout);

            if (mIsGluonMode)
            {
                GluonCheckoutOperation.Checkout(
                    mViewHost,
                    mProgressControls,
                    mGuiMessage,
                    selectedPaths.ToArray(),
                    false,
                    RefreshAsset.VersionControlCache,
                    mWkInfo);
                return;
            }

            CheckoutOperation.Checkout(
                mWorkspaceWindow,
                null,
                mProgressControls,
                selectedPaths,
                mNewIncomingChangesUpdater,
                mShelvedChangesUpdater,
                RefreshAsset.VersionControlCache,
                mWkInfo);
        }

        void IAssetMenuVCSOperations.Checkin()
        {
            List<string> selectedPaths = GetSelectedPaths.ForOperation(
                mWkInfo.ClientPath,
                mAssetSelection.GetSelectedAssets(),
                mAssetStatusCache,
                AssetMenuOperations.Checkin);

            if (!CheckinDialog.CheckinPaths(
                    mWkInfo,
                    selectedPaths,
                    mAssetStatusCache,
                    mIsGluonMode,
                    mWorkspaceWindow,
                    mViewHost,
                    mWorkspaceOperationsMonitor,
                    mGuiMessage,
                    mMergeViewLauncher,
                    mGluonViewSwitcher))
                return;

            RefreshAsset.UnityAssetDatabase();
        }

        void IAssetMenuVCSOperations.Undo()
        {
            List<string> selectedPaths = GetSelectedPaths.ForOperation(
                mWkInfo.ClientPath,
                mAssetSelection.GetSelectedAssets(),
                mAssetStatusCache,
                AssetMenuOperations.Undo);

            SaveAssets.ForPathsWithoutConfirmation(
                mWkInfo.ClientPath, selectedPaths, mWorkspaceOperationsMonitor);

            if (mIsGluonMode)
            {
                GluonUndoCheckoutOperation.UndoCheckout(
                    mWkInfo,
                    mViewHost,
                    mProgressControls,
                    selectedPaths.ToArray(),
                    false,
                    RefreshAsset.UnityAssetDatabase);
                return;
            }

            UndoCheckoutOperation.Run(
                mWorkspaceWindow,
                null,
                mProgressControls,
                selectedPaths,
                mNewIncomingChangesUpdater,
                mShelvedChangesUpdater,
                RefreshAsset.UnityAssetDatabase);
        }

        void IAssetMenuVCSOperations.ShowDiff()
        {
            string selectedPath = AssetsSelection.GetSelectedPath(
                mWkInfo.ClientPath,
                mAssetSelection.GetSelectedAssets());

            DiffInfo diffInfo = null;

            IThreadWaiter waiter = ThreadWaiter.GetWaiter(10);
            waiter.Execute(
                /*threadOperationDelegate*/ delegate
                {
                    string symbolicName = GetSymbolicName(selectedPath);
                    string extension = Path.GetExtension(selectedPath);

                    diffInfo = PlasticGui.Plastic.API.BuildDiffInfoForDiffWithPrevious(
                        selectedPath, symbolicName, selectedPath, extension, mWkInfo);
                },
                /*afterOperationDelegate*/ delegate
                {
                    if (waiter.Exception != null)
                    {
                        ExceptionsHandler.DisplayException(waiter.Exception);
                        return;
                    }

                    DiffOperation.DiffWithPrevious(
                        diffInfo,
                        PlasticExeLauncher.BuildForShowDiff(mWkInfo, mIsGluonMode, mShowDownloadPlasticExeWindow),
                        null);
                });
        }

        void IAssetMenuVCSOperations.ShowHistory()
        {
            Asset selectedAsset = AssetsSelection.GetSelectedAsset(
                mWkInfo.ClientPath,
                mAssetSelection.GetSelectedAssets());

            string selectedPath = Path.GetFullPath(selectedAsset.path);

            WorkspaceTreeNode node = PlasticGui.Plastic.API.
                GetWorkspaceTreeNode(selectedPath);

            mHistoryViewLauncher.ShowHistoryView(
                node.RepSpec,
                node.RevInfo.ItemId,
                selectedPath,
                selectedAsset.isFolder);
        }

        void IAssetFilesFilterPatternsMenuOperations.AddFilesFilterPatterns(
            FilterTypes type,
            FilterActions action,
            FilterOperationType operation)
        {
            List<string> selectedPaths = AssetsSelection.GetSelectedPaths(
                mWkInfo.ClientPath,
                mAssetSelection.GetSelectedAssets());

            string[] rules = FilterRulesGenerator.GenerateRules(
                selectedPaths, mWkInfo.ClientPath, action, operation);

            bool isApplicableToAllWorkspaces = !mIsGluonMode;
            bool isAddOperation = operation == FilterOperationType.Add;

            FilterRulesConfirmationData filterRulesConfirmationData =
                FilterRulesConfirmationDialog.AskForConfirmation(
                    rules, isAddOperation, isApplicableToAllWorkspaces, EditorWindow.focusedWindow);

            AddFilesFilterPatternsOperation.Run(
                mWkInfo, mWorkspaceWindow, type, operation, filterRulesConfirmationData);
        }

        static string GetSymbolicName(string selectedPath)
        {
            WorkspaceTreeNode node = PlasticGui.Plastic.API.
                GetWorkspaceTreeNode(selectedPath);

            string branchName = string.Empty;
            BranchInfoCache.TryGetBranchName(
                node.RepSpec, node.RevInfo.BranchId, out branchName);

            string userName = PlasticGui.Plastic.API.GetUserName(
                node.RepSpec.Server, node.RevInfo.Owner);

            string symbolicName = string.Format(
                "cs:{0}@{1} {2} {3}",
                node.RevInfo.Changeset,
                string.Format("br:{0}", branchName),
                userName,
                "Workspace Revision");

            return symbolicName;
        }

        readonly WorkspaceInfo mWkInfo;
        readonly IViewSwitcher mViewSwitcher;
        readonly IHistoryViewLauncher mHistoryViewLauncher;
        readonly IWorkspaceWindow mWorkspaceWindow;
        readonly ViewHost mViewHost;
        readonly WorkspaceOperationsMonitor mWorkspaceOperationsMonitor;
        readonly NewIncomingChangesUpdater mNewIncomingChangesUpdater;
        readonly ShelvedChangesUpdater mShelvedChangesUpdater;
        readonly IAssetStatusCache mAssetStatusCache;
        readonly IMergeViewLauncher mMergeViewLauncher;
        readonly IGluonViewSwitcher mGluonViewSwitcher;
        readonly bool mIsGluonMode;
        readonly GuiMessage.IGuiMessage mGuiMessage;
        readonly EditorProgressControls mProgressControls;
        readonly IAssetSelection mAssetSelection;
        readonly LaunchTool.IShowDownloadPlasticExeWindow mShowDownloadPlasticExeWindow;
    }
}
