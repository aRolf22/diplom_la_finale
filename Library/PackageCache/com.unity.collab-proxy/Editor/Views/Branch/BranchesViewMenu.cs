using UnityEditor;
using UnityEngine;

using PlasticGui;
using PlasticGui.WorkspaceWindow.QueryViews.Branches;
using Unity.PlasticSCM.Editor.UI;

namespace Unity.PlasticSCM.Editor.Views.Branches
{
    internal class BranchesViewMenu
    {
        internal GenericMenu Menu { get { return mMenu; } }

        internal BranchesViewMenu(
            IBranchMenuOperations branchMenuOperations,
            bool isGluonMode)
        {
            mBranchMenuOperations = branchMenuOperations;
            mIsGluonMode = isGluonMode;

            BuildComponents();
        }

        internal void Popup()
        {
            mMenu = new GenericMenu();

            UpdateMenuItems(mMenu);

            mMenu.ShowAsContext();
        }

        internal bool ProcessKeyActionIfNeeded(Event e)
        {
            BranchMenuOperations operationToExecute = GetMenuOperations(e);

            if (operationToExecute == BranchMenuOperations.None)
                return false;

            BranchMenuOperations operations =
                BranchMenuUpdater.GetAvailableMenuOperations(
                    mBranchMenuOperations.GetSelectedBranchesCount(),
                    mIsGluonMode,
                    false);

            if (!operations.HasFlag(operationToExecute))
                return false;

            ProcessMenuOperation(operationToExecute, mBranchMenuOperations);
            return true;
        }

        void CreateBranchMenuItem_Click()
        {
            mBranchMenuOperations.CreateBranch();
        }

        void SwitchToBranchMenuItem_Click()
        {
            mBranchMenuOperations.SwitchToBranch();
        }

        void RenameBranchMenuItem_Click()
        {
            mBranchMenuOperations.RenameBranch();
        }

        void DeleteBranchMenuItem_Click()
        {
            mBranchMenuOperations.DeleteBranch();
        }

        void MergeBranchMenuItem_Click()
        {
            mBranchMenuOperations.MergeBranch();
        }

        void CreateCodeReviewMenuItem_Click()
        {
            mBranchMenuOperations.CreateCodeReview();
        }

        void UpdateMenuItems(GenericMenu menu)
        {
            BranchMenuOperations operations = BranchMenuUpdater.GetAvailableMenuOperations(
                mBranchMenuOperations.GetSelectedBranchesCount(), mIsGluonMode, false);

            AddBranchMenuItem(
                mCreateBranchMenuItemContent,
                menu,
                operations,
                BranchMenuOperations.CreateBranch,
                CreateBranchMenuItem_Click);

            AddBranchMenuItem(
                mSwitchToBranchMenuItemContent,
                menu,
                operations,
                BranchMenuOperations.SwitchToBranch,
                SwitchToBranchMenuItem_Click);

            if (!mIsGluonMode)
            {
                menu.AddSeparator(string.Empty);

                AddBranchMenuItem(
                    mMergeBranchMenuItemContent,
                    menu,
                    operations,
                    BranchMenuOperations.MergeBranch,
                    MergeBranchMenuItem_Click);
            }

            menu.AddSeparator(string.Empty);

            AddBranchMenuItem(
                mRenameBranchMenuItemContent,
                menu,
                operations,
                BranchMenuOperations.Rename,
                RenameBranchMenuItem_Click);

            AddBranchMenuItem(
                mDeleteBranchMenuItemContent,
                menu,
                operations,
                BranchMenuOperations.Delete,
                DeleteBranchMenuItem_Click);

            menu.AddSeparator(string.Empty);

            AddBranchMenuItem(
                mCreateCodeReviewMenuItemContent,
                menu,
                operations,
                BranchMenuOperations.CreateCodeReview,
                CreateCodeReviewMenuItem_Click);
        }

        static void AddBranchMenuItem(
            GUIContent menuItemContent,
            GenericMenu menu,
            BranchMenuOperations operations,
            BranchMenuOperations operationsToCheck,
            GenericMenu.MenuFunction menuFunction)
        {
            if (operations.HasFlag(operationsToCheck))
            {
                menu.AddItem(
                    menuItemContent,
                    false,
                    menuFunction);
                return;
            }

            menu.AddDisabledItem(menuItemContent);
        }

        static void ProcessMenuOperation(
            BranchMenuOperations operationToExecute,
            IBranchMenuOperations branchMenuOperations)
        {
            if (operationToExecute == BranchMenuOperations.Delete)
            {
                branchMenuOperations.DeleteBranch();
                return;
            }

            if (operationToExecute == BranchMenuOperations.MergeBranch)
            {
                branchMenuOperations.MergeBranch();
                return;
            }
        }

        static BranchMenuOperations GetMenuOperations(Event e)
        {
            if (Keyboard.IsKeyPressed(e, KeyCode.Delete))
                return BranchMenuOperations.Delete;

            if (Keyboard.IsControlOrCommandKeyPressed(e) &&
                Keyboard.IsKeyPressed(e, KeyCode.M))
                return BranchMenuOperations.MergeBranch;

            return BranchMenuOperations.None;
        }

        void BuildComponents()
        {
            mCreateBranchMenuItemContent = new GUIContent(
                PlasticLocalization.GetString(PlasticLocalization.Name.BranchMenuItemCreateBranch));
            mSwitchToBranchMenuItemContent = new GUIContent(
                PlasticLocalization.GetString(PlasticLocalization.Name.BranchMenuItemSwitchToBranch));
            mMergeBranchMenuItemContent = new GUIContent(string.Format("{0} {1}",
                PlasticLocalization.GetString(PlasticLocalization.Name.BranchMenuItemMergeFromBranch),
                GetPlasticShortcut.ForMerge()));
            mRenameBranchMenuItemContent = new GUIContent(
                PlasticLocalization.GetString(PlasticLocalization.Name.BranchMenuItemRenameBranch));
            mDeleteBranchMenuItemContent = new GUIContent(string.Format("{0} {1}",
                PlasticLocalization.GetString(PlasticLocalization.Name.BranchMenuItemDeleteBranch),
                GetPlasticShortcut.ForDelete()));
            mCreateCodeReviewMenuItemContent = new GUIContent(
                PlasticLocalization.Name.BranchMenuCreateANewCodeReview.GetString());
        }

        GenericMenu mMenu;

        GUIContent mCreateBranchMenuItemContent;
        GUIContent mSwitchToBranchMenuItemContent;
        GUIContent mMergeBranchMenuItemContent;
        GUIContent mRenameBranchMenuItemContent;
        GUIContent mDeleteBranchMenuItemContent;
        GUIContent mCreateCodeReviewMenuItemContent;

        readonly IBranchMenuOperations mBranchMenuOperations;
        readonly bool mIsGluonMode;
    }
}
