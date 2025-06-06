using UnityEditor;
using UnityEngine;

using Codice.CM.Common;
using PlasticGui;

namespace Unity.PlasticSCM.Editor.UI.Tree
{
    internal static class DrawTreeViewEmptyState
    {
        internal static void For(
            Rect rect,
            GUIContent content)
        {
            Vector2 contentSize = GetContentSize(content);

            GUI.BeginGroup(rect);

            DrawLabel(
                content,
                contentSize,
                (rect.width - contentSize.x) / 2,
                rect.height / 2);

            GUI.EndGroup();
        }

        internal static void For(
            Rect rect,
            GUIContent content,
            Texture2D icon)
        {
            Vector2 contentSize = GetContentSize(content);

            GUI.BeginGroup(rect);

            DrawLabelWithIcon(
                content,
                contentSize,
                (rect.width - contentSize.x) / 2,
                rect.height / 2,
                icon);

            GUI.EndGroup();
        }

        internal static void ForInviteMembers(
            Rect rect,
            GUIContent textContent,
            Texture2D icon,
            RepositorySpec repSpec)
        {
            Vector2 textContentSize = GetContentSize(textContent);

            GUIContent linkContent = new GUIContent(PlasticLocalization.Name.InviteOtherTeamMembers.GetString());
            Vector2 linkContentSize = GetContentSize(linkContent, EditorStyles.linkLabel);

            float textContentOffsetY = (rect.height - (textContentSize.y + linkContentSize.y)) / 2;
            float linkContentOffsetY = textContentOffsetY + textContentSize.y;

            GUI.BeginGroup(rect);

            DrawLabelWithIcon(
                textContent,
                textContentSize,
                (rect.width - textContentSize.x) / 2,
                textContentOffsetY,
                icon);

            DrawInviteMembersLink(
                repSpec,
                linkContent,
                linkContentSize,
                (rect.width - linkContentSize.x) / 2,
                linkContentOffsetY);

            GUI.EndGroup();
        }

        static void DrawLabel(
            GUIContent content,
            Vector2 contentSize,
            float offsetX,
            float offsetY)
        {
            GUI.Label(
                new Rect(offsetX, offsetY, contentSize.x, contentSize.y),
                content,
                UnityStyles.Tree.StatusLabel);
        }

        static void DrawLabelWithIcon(
            GUIContent content,
            Vector2 contentSize,
            float offsetX,
            float offsetY,
            Texture2D icon)
        {
            int iconSize = UnityConstants.TREEVIEW_STATUS_ICON_SIZE;
            int padding = UnityConstants.TREEVIEW_STATUS_CONTENT_PADDING;

            float iconOffsetX = offsetX - iconSize + padding;
            float contentOffsetX = offsetX + iconSize - padding;

            GUI.DrawTexture(
                new Rect(iconOffsetX, offsetY + padding, iconSize, iconSize),
                icon,
                ScaleMode.ScaleToFit);

            DrawLabel(
                content,
                contentSize,
                contentOffsetX,
                offsetY);
        }

        static void DrawInviteMembersLink(
            RepositorySpec repSpec,
            GUIContent linkContent,
            Vector2 linkContentSize,
            float offsetX,
            float offsetY)
        {
            Rect buttonPosition = new Rect(
                offsetX,
                offsetY,
                linkContentSize.x,
                linkContentSize.y);

            EditorGUIUtility.AddCursorRect(buttonPosition, MouseCursor.Link);

            if (GUI.Button(buttonPosition, linkContent, EditorStyles.linkLabel))
            {
                OpenInviteUsersPage.Run(repSpec, UnityUrl.UnityDashboard.UnityCloudRequestSource.Editor);
            }
        }

        static Vector2 GetContentSize(GUIContent content)
        {
            return GetContentSize(content, UnityStyles.Tree.StatusLabel);
        }

        static Vector2 GetContentSize(GUIContent content, GUIStyle guiStyle)
        {
            return guiStyle.CalcSize(content);
        }
    }
}
