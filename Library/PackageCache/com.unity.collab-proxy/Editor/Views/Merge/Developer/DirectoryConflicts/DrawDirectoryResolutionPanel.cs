﻿using System;

using UnityEditor;
using UnityEngine;

using Codice.Client.BaseCommands.Merge;
using Codice.CM.Common.Merge;
using PlasticGui;
using PlasticGui.WorkspaceWindow.Merge;
using Unity.PlasticSCM.Editor.UI;

namespace Unity.PlasticSCM.Editor.Views.Merge.Developer.DirectoryConflicts
{
    internal static class DrawDirectoryResolutionPanel
    {
        internal static void ForConflict(
            MergeChangeInfo conflict,
            int pendingConflictsCount,
            DirectoryConflictUserInfo conflictUserInfo,
            DirectoryConflictAction[] actions,
            Action<MergeChangeInfo> resolveConflictAction,
            GUIContent validationLabel,
            bool isPanelEnabled,
            ref ConflictResolutionState state)
        {
            bool isResolveButtonEnabled;
            string validationMessage = null;

            GetValidationData(
                conflict,
                state,
                out isResolveButtonEnabled,
                out validationMessage);

            GUILayout.Space(2);
            DoHeader(
                conflictUserInfo.ConflictTitle,
                conflict,
                resolveConflictAction,
                isResolveButtonEnabled,
                isPanelEnabled,
                ref state);
            DoConflictExplanation(conflictUserInfo.ConflictExplanation);
            DoSourceAndDestinationLabels(
                conflictUserInfo.SourceOperation,
                conflictUserInfo.DestinationOperation);
            DoResolutionOptions(
                actions,
                validationLabel,
                validationMessage,
                isPanelEnabled,
                ref state);
            DoApplyActionsForNextConflictsCheck(pendingConflictsCount, isPanelEnabled, ref state);
            GUILayout.Space(10);
        }

        static void DoHeader(
            string conflictName,
            MergeChangeInfo conflict,
            Action<MergeChangeInfo> resolveConflictAction,
            bool isResolveButtonEnabled,
            bool isPanelEnabled,
            ref ConflictResolutionState state)
        {
            EditorGUILayout.BeginHorizontal();

            GUILayout.Label(conflictName,
                UnityStyles.DirectoryConflicts.TitleLabel);

            GUI.enabled = isResolveButtonEnabled && isPanelEnabled;

            GUILayout.Space(5);

            if (GUILayout.Button(PlasticLocalization.GetString(
                PlasticLocalization.Name.ResolveDirectoryConflict)))
            {
                resolveConflictAction(conflict);
            }

            GUI.enabled = true;

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        static void DoConflictExplanation(string explanation)
        {
            GUILayout.Space(5);
            GUILayout.Label(explanation, EditorStyles.wordWrappedLabel);
        }

        static void DoSourceAndDestinationLabels(
            string sourceOperation,
            string destinationOperation)
        {
            GUILayout.Space(5);

            GUIStyle boldLabelStyle = UnityStyles.DirectoryConflicts.BoldLabel;

            GUIContent srcLabel = new GUIContent(
                PlasticLocalization.GetString(
                    PlasticLocalization.Name.Source));

            GUIContent dstLabel = new GUIContent(
                PlasticLocalization.GetString(
                    PlasticLocalization.Name.Destination));

            float maxWidth = GetMaxWidth(srcLabel, dstLabel, boldLabelStyle);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(25);
            GUILayout.Label(srcLabel, boldLabelStyle, GUILayout.Width(maxWidth));
            GUILayout.Label(sourceOperation, EditorStyles.label);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(25);
            GUILayout.Label(dstLabel, boldLabelStyle, GUILayout.Width(maxWidth));
            GUILayout.Label(destinationOperation, EditorStyles.label);
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        static void DoResolutionOptions(
            DirectoryConflictAction[] actions,
            GUIContent validationLabel,
            string validationMessage,
            bool isPanelEnabled,
            ref ConflictResolutionState state)
        {
            GUILayout.Space(10);
            GUILayout.Label(PlasticLocalization.GetString(
                PlasticLocalization.Name.ResolveDirectoryConflictChooseOption));

            GUI.enabled = isPanelEnabled;

            foreach (DirectoryConflictAction action in actions)
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(25);
                if (GUILayout.Toggle(
                    state.ResolveAction == action.ActionKind,
                    action.ActionText,
                    EditorStyles.radioButton))
                {
                    state.ResolveAction = action.ActionKind;
                }

                if (action.ActionKind == DirectoryConflictResolveActions.Rename)
                {
                    GUI.enabled = isPanelEnabled && state.ResolveAction == DirectoryConflictResolveActions.Rename;
                    state.RenameValue = GUILayout.TextField(
                        state.RenameValue,
                        UnityStyles.DirectoryConflicts.FileNameTextField,
                        GUILayout.Width(250));
                    GUI.enabled = isPanelEnabled;

                    if (!string.IsNullOrEmpty(validationMessage))
                    {
                        validationLabel.text = validationMessage;

                        GUILayout.Label(
                            validationLabel,
                            UnityStyles.DirectoryConflictResolution.WarningLabel,
                            GUILayout.Height(UnityConstants.DIR_CONFLICT_VALIDATION_WARNING_LABEL_HEIGHT));
                    }
                }

                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }

            GUI.enabled = true;
        }

        static void DoApplyActionsForNextConflictsCheck(
            int pendingConflictsCount,
            bool isPanelEnabled,
            ref ConflictResolutionState state)
        {
            if (pendingConflictsCount == 0)
                return;

            GUILayout.Space(5);

            bool isCheckEnabled = state.ResolveAction != DirectoryConflictResolveActions.Rename;
            bool isChecked = state.IsApplyActionsForNextConflictsChecked & isCheckEnabled;

            GUI.enabled = isCheckEnabled && isPanelEnabled;
            EditorGUILayout.BeginHorizontal();

            state.IsApplyActionsForNextConflictsChecked = GUILayout.Toggle(
                isChecked,
                GetApplyActionCheckButtonText(pendingConflictsCount));

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            GUI.enabled = true;
        }

        static bool IsValidName(
            string name,
            DirectoryConflict conflict,
            out string errorMessage)
        {
            if (string.IsNullOrEmpty(name))
            {
                errorMessage = PlasticLocalization.GetString(
                    PlasticLocalization.Name.InputItemNameMessage);
                return false;
            }

            if (name == DirectoryConflictResolutionInfo.GetOldItemName(conflict))
            {
                errorMessage = PlasticLocalization.GetString(
                    PlasticLocalization.Name.ProvideDifferentItemNameForRenameResolution);
                return false;
            }

            errorMessage = null;
            return true;
        }

        internal static void GetValidationData(
            MergeChangeInfo conflict,
            ConflictResolutionState state,
            out bool isResolveButtonEnabled,
            out string renameWarningMessage)
        {
            if (state.ResolveAction != DirectoryConflictResolveActions.Rename)
            {
                renameWarningMessage = string.Empty;
                isResolveButtonEnabled = true;
                return;
            }

            isResolveButtonEnabled = IsValidName(
                state.RenameValue,
                conflict.DirectoryConflict,
                out renameWarningMessage);
        }

        static float GetMaxWidth(
            GUIContent label1,
            GUIContent label2,
            GUIStyle style)
        {
            Vector2 srcLabelSize = style.CalcSize(label1);
            Vector2 dstLabelSize = style.CalcSize(label2);

            return Math.Max(srcLabelSize.x, dstLabelSize.x);
        }

        static string GetApplyActionCheckButtonText(int pendingConflictsCount)
        {
            if (pendingConflictsCount > 1)
                return PlasticLocalization.GetString(
                    PlasticLocalization.Name.ApplyActionForNextConflictsCheckButtonPlural,
                    pendingConflictsCount);

            return PlasticLocalization.GetString(
                PlasticLocalization.Name.ApplyActionForNextConflictsCheckButtonSingular);
        }
    }
}
