using UnityEditor;
using UnityEngine;

using PlasticGui;
using PlasticGui.WorkspaceWindow.Merge;

namespace Unity.PlasticSCM.Editor.Views.Merge.Developer.DirectoryConflicts
{
    internal class DeleteMoveMenu : MergeViewDirectoryConflictMenu.IDirectoryConflictMenu
    {
        internal DeleteMoveMenu(
            IMergeViewMenuOperations mergeViewMenuOperations)
        {
            mMergeViewMenuOperations = mergeViewMenuOperations;

            BuildComponents();
        }

        GenericMenu MergeViewDirectoryConflictMenu.IDirectoryConflictMenu.Menu
        {
            get { return mMenu; }
        }

        void MergeViewDirectoryConflictMenu.IDirectoryConflictMenu.Popup()
        {
            mMenu = new GenericMenu();

            UpdateMenuItems(mMenu);

            mMenu.ShowAsContext();
        }

        void ViewDestination_Click()
        {
            mMergeViewMenuOperations.OpenDstRevision();
        }

        void ViewSource_Click()
        {
            mMergeViewMenuOperations.OpenSrcRevision();
        }

        void DiffSourceDestination_Click()
        {
            mMergeViewMenuOperations.DiffSourceWithDestination();
        }

        void UpdateMenuItems(GenericMenu menu)
        {
            SelectedMergeChangesGroupInfo info =
                mMergeViewMenuOperations.GetSelectedMergeChangesGroupInfo();

            menu.AddItem(mViewSourceMenuItemContent, false, ViewSource_Click);

            menu.AddItem(mViewDestinationMenuItemContent, false, ViewDestination_Click);

            if (info.SelectedConflict.DirectoryConflict.SourceAndDestinationAreSameType())
                menu.AddItem(mDiffSourceDestinationMenuItemContent, false, DiffSourceDestination_Click);
        }

        void BuildComponents()
        {
            mViewSourceMenuItemContent = new GUIContent(
                PlasticLocalization.Name.DeleteMoveConflictViewSource.GetString());

            mViewDestinationMenuItemContent = new GUIContent(
                PlasticLocalization.Name.DeleteMoveConflictViewDestination.GetString());

            mDiffSourceDestinationMenuItemContent = new GUIContent(
                PlasticLocalization.Name.DiffSourceWithDestinationDeleteMove.GetString());
        }

        GenericMenu mMenu;

        GUIContent mViewSourceMenuItemContent;
        GUIContent mViewDestinationMenuItemContent;
        GUIContent mDiffSourceDestinationMenuItemContent;

        readonly IMergeViewMenuOperations mMergeViewMenuOperations;
    }
}
