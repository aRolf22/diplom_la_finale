using System.Collections.Generic;
using System.IO;
using UnityEngine.U2D;
using UnityEngine;

namespace UnityEditor.U2D.Sprites
{
    internal class SpriteDataExt : SpriteRect
    {
        public float tessellationDetail = 0;

        // The following lists are to be left un-initialized.
        // If they never loaded or assign explicitly, we avoid writing empty list to metadata.
        public List<Vector2[]> spriteOutline;
        public List<Vertex2DMetaData> vertices;
        public List<int> indices;
        public List<Vector2Int> edges;
        public List<Vector2[]> spritePhysicsOutline;
        public List<SpriteBone> spriteBone;

        long m_InternalID;

        internal SpriteDataExt(SerializedObject so)
        {
            var ti = so.targetObject as TextureImporter;
            name = Path.GetFileNameWithoutExtension(ti.assetPath);
            alignment = (SpriteAlignment)so.FindProperty("m_Alignment").intValue;
            border = ti.spriteBorder;
            pivot = SpriteEditorUtility.GetPivotValue(alignment, ti.spritePivot);
            tessellationDetail = so.FindProperty("m_SpriteTessellationDetail").floatValue;

            int width = 0, height = 0;
            ti.GetWidthAndHeight(ref width, ref height);
            rect = new Rect(0, 0, width, height);

            var guidSP = so.FindProperty("m_SpriteSheet.m_SpriteID");
            spriteID = new GUID(guidSP.stringValue);

            m_InternalID = so.FindProperty("m_SpriteSheet.m_InternalID").longValue;
            customData = so.FindProperty("m_SpriteSheet.m_CustomData").stringValue;
        }

        internal SpriteDataExt(SerializedProperty sp)
        {
            rect = sp.FindPropertyRelative("m_Rect").rectValue;
            border = sp.FindPropertyRelative("m_Border").vector4Value;
            name = sp.FindPropertyRelative("m_Name").stringValue;
            alignment = (SpriteAlignment)sp.FindPropertyRelative("m_Alignment").intValue;
            pivot = SpriteEditorUtility.GetPivotValue(alignment, sp.FindPropertyRelative("m_Pivot").vector2Value);
            tessellationDetail = sp.FindPropertyRelative("m_TessellationDetail").floatValue;
            spriteID = new GUID(sp.FindPropertyRelative("m_SpriteID").stringValue);
            m_InternalID = sp.FindPropertyRelative("m_InternalID").longValue;
            customData = sp.FindPropertyRelative("m_CustomData").stringValue;
        }

        internal SpriteDataExt(SpriteDataExt sr)
        {
            originalName = sr.originalName;
            name = sr.name;
            border = sr.border;
            tessellationDetail = 0;
            rect = sr.rect;
            spriteID = sr.spriteID;
            m_InternalID = sr.internalID;
            alignment = sr.alignment;
            pivot = sr.pivot;
            spriteOutline = new List<Vector2[]>();
            vertices = new List<Vertex2DMetaData>();
            indices = new List<int>();
            edges = new List<Vector2Int>();
            spritePhysicsOutline = new List<Vector2[]>();
            spriteBone = new List<SpriteBone>();
            customData = sr.customData;
        }

        internal SpriteDataExt(SpriteRect sr)
        {
            originalName = sr.originalName;
            name = sr.name;
            border = sr.border;
            tessellationDetail = 0;
            rect = sr.rect;
            spriteID = sr.spriteID;
            m_InternalID = sr.spriteID.GetHashCode();
            alignment = sr.alignment;
            pivot = sr.pivot;
            customData = sr.customData;
            spriteOutline = new List<Vector2[]>();
            vertices = new List<Vertex2DMetaData>();
            indices = new List<int>();
            edges = new List<Vector2Int>();
            spritePhysicsOutline = new List<Vector2[]>();
            spriteBone = new List<SpriteBone>();
        }

        public void Apply(SerializedObject so)
        {
            so.FindProperty("m_Alignment").intValue = (int)alignment;
            so.FindProperty("m_SpriteBorder").vector4Value = border;
            so.FindProperty("m_SpritePivot").vector2Value = pivot;
            so.FindProperty("m_SpriteTessellationDetail").floatValue = tessellationDetail;
            so.FindProperty("m_SpriteSheet.m_SpriteID").stringValue = spriteID.ToString();
            so.FindProperty("m_SpriteSheet.m_InternalID").longValue = m_InternalID;
            so.FindProperty("m_SpriteSheet.m_CustomData").stringValue = customData;

            var sp = so.FindProperty("m_SpriteSheet");
            if (spriteBone != null)
                SpriteBoneDataTransfer.Apply(sp, spriteBone);
            if (spriteOutline != null)
                SpriteOutlineDataTransfer.Apply(sp, spriteOutline);
            if (spritePhysicsOutline != null)
                SpritePhysicsOutlineDataTransfer.Apply(sp, spritePhysicsOutline);
            if (vertices != null)
                SpriteMeshDataTransfer.Apply(sp, vertices, indices, edges);
        }

        public void Apply(SerializedProperty sp)
        {
            sp.FindPropertyRelative("m_Rect").rectValue = rect;
            sp.FindPropertyRelative("m_Name").stringValue = name;
            sp.FindPropertyRelative("m_Border").vector4Value = border;
            sp.FindPropertyRelative("m_Alignment").intValue = (int)alignment;
            sp.FindPropertyRelative("m_Pivot").vector2Value = pivot;
            sp.FindPropertyRelative("m_TessellationDetail").floatValue = tessellationDetail;
            sp.FindPropertyRelative("m_SpriteID").stringValue = spriteID.ToString();
            sp.FindPropertyRelative("m_InternalID").longValue = m_InternalID;
            sp.FindPropertyRelative("m_CustomData").stringValue = customData;

            if (spriteBone != null)
                SpriteBoneDataTransfer.Apply(sp, spriteBone);
            if (spriteOutline != null)
                SpriteOutlineDataTransfer.Apply(sp, spriteOutline);
            if (spritePhysicsOutline != null)
                SpritePhysicsOutlineDataTransfer.Apply(sp, spritePhysicsOutline);
            if (vertices != null)
                SpriteMeshDataTransfer.Apply(sp, vertices, indices, edges);
        }

        public void CopyFromSpriteRect(SpriteRect spriteRect)
        {
            alignment = spriteRect.alignment;
            border = spriteRect.border;
            name = spriteRect.name;
            pivot = spriteRect.pivot;
            rect = spriteRect.rect;
            spriteID = spriteRect.spriteID;
            customData = spriteRect.customData;
        }

        public long internalID
        {
            get
            {
                if (m_InternalID == 0)
                    m_InternalID = spriteID.GetHashCode();

                return m_InternalID;
            }
            set => m_InternalID = value;
        }
    }

    internal class SpriteNameFileIdPairExt : SpriteNameFileIdPair
    {
        private const string k_NameField = "first";
        private const string k_FileIdField = "second";

        long m_InternalId;

        public SpriteNameFileIdPairExt(string name, GUID guid, long internalId)
            : base(name, guid)
        {
            m_InternalId = internalId;
        }

        public long internalID
        {
            get
            {
                if (m_InternalId == 0L)
                    m_InternalId = GetFileGUID().GetHashCode();
                return m_InternalId;
            }
            set => m_InternalId = value;
        }

        public static SpriteNameFileIdPairExt GetValue(SerializedProperty sp)
        {
            var name = sp.FindPropertyRelative(k_NameField).stringValue;
            var id = sp.FindPropertyRelative(k_FileIdField).longValue;
            return new SpriteNameFileIdPairExt(name, GUID.Generate(), id);
        }

        public void Apply(SerializedProperty sp)
        {
            sp.FindPropertyRelative(k_NameField).stringValue = name;
            sp.FindPropertyRelative(k_FileIdField).longValue = internalID;
        }
    }
}
