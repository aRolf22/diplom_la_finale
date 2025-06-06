using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.VFX;

namespace UnityEditor.VFX
{
    [VFXGizmo(typeof(Transform))]
    class VFXTransformGizmo : VFXSpaceableGizmo<Transform>
    {
        IProperty<Vector3> m_PositionProperty;
        IProperty<Vector3> m_AnglesProperty;
        IProperty<Vector3> m_ScaleProperty;

        public override void RegisterEditableMembers(IContext context)
        {
            m_PositionProperty = context.RegisterProperty<Vector3>("position");
            m_AnglesProperty = context.RegisterProperty<Vector3>("angles");
            m_ScaleProperty = context.RegisterProperty<Vector3>("scale");
        }

        public override void OnDrawSpacedGizmo(Transform transform)
        {
            TransformGizmo(
                transform.position,
                transform.angles,
                transform.scale,
                m_PositionProperty,
                m_AnglesProperty,
                m_ScaleProperty);
        }

        public override Bounds OnGetSpacedGizmoBounds(Transform value)
        {
            return new Bounds(value.position, value.scale); //TODO take orientation in account
        }
    }
}
