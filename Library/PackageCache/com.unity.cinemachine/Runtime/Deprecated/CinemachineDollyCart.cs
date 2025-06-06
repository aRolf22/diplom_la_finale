#if !CINEMACHINE_NO_CM2_SUPPORT
using System;
using UnityEngine;
using UnityEngine.Serialization;

namespace Unity.Cinemachine
{
    /// <summary>
    /// This is a deprecated component.  Use CinemachineSplineCart instead.
    /// </summary>
    [Obsolete("CinemachineDollyCart has been deprecated. Use CinemachineSplineCart instead.")]
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [AddComponentMenu("")] // Don't display in add component menu
    public class CinemachineDollyCart : MonoBehaviour
    {
        /// <summary>The path to follow</summary>
        [Tooltip("The path to follow")]
        public CinemachinePathBase m_Path;
        
        /// <summary>This enum defines the options available for the update method.</summary>
        public enum UpdateMethod
        {
            /// <summary>Updated in normal MonoBehaviour Update.</summary>
            Update,
            /// <summary>Updated in sync with the Physics module, in FixedUpdate</summary>
            FixedUpdate,
            /// <summary>Updated in normal MonoBehaviour LateUpdate</summary>
            LateUpdate
        };

        /// <summary>When to move the cart, if Velocity is non-zero</summary>
        [Tooltip("When to move the cart, if Velocity is non-zero")]
        public UpdateMethod m_UpdateMethod = UpdateMethod.Update;

        /// <summary>How to interpret the Path Position</summary>
        [Tooltip("How to interpret the Path Position.  If set to Path Units, values are as follows: 0 represents the "
            + "first waypoint on the path, 1 is the second, and so on.  Values in-between are points on the path in "
            + "between the waypoints.  If set to Distance, then Path Position represents distance along the path.")]
        public CinemachinePathBase.PositionUnits m_PositionUnits = CinemachinePathBase.PositionUnits.Distance;

        /// <summary>Move the cart with this speed</summary>
        [Tooltip("Move the cart with this speed along the path.  The value is interpreted according to the Position Units setting.")]
        [FormerlySerializedAs("m_Velocity")]
        public float m_Speed;

        /// <summary>The cart's current position on the path, in distance units</summary>
        [Tooltip("The position along the path at which the cart will be placed.  This can be animated directly or, "
            + "if the velocity is non-zero, will be updated automatically.  The value is interpreted according to the "
            + "Position Units setting.")]
        [FormerlySerializedAs("m_CurrentDistance")]
        public float m_Position;

        void FixedUpdate()
        {
            if (m_UpdateMethod == UpdateMethod.FixedUpdate)
                SetCartPosition(m_Position + m_Speed * Time.deltaTime);
        }

        void Update()
        {
            float speed = Application.isPlaying ? m_Speed : 0;
            if (m_UpdateMethod == UpdateMethod.Update)
                SetCartPosition(m_Position + speed * Time.deltaTime);
        }

        void LateUpdate()
        {
            if (!Application.isPlaying)
                SetCartPosition(m_Position);
            else if (m_UpdateMethod == UpdateMethod.LateUpdate)
                SetCartPosition(m_Position + m_Speed * Time.deltaTime);
        }

        void SetCartPosition(float distanceAlongPath)
        {
            if (m_Path != null)
            {
                m_Position = m_Path.StandardizeUnit(distanceAlongPath, m_PositionUnits);
                transform.position = m_Path.EvaluatePositionAtUnit(m_Position, m_PositionUnits);
                transform.rotation = m_Path.EvaluateOrientationAtUnit(m_Position, m_PositionUnits);
            }
        }

        // Helper to upgrade to CM3
        internal void UpgradeToCm3(CinemachineSplineCart c)
        {
            c.UpdateMethod = (CinemachineSplineCart.UpdateMethods)m_UpdateMethod; // enum values match
            switch (m_PositionUnits)
            {
                case CinemachinePathBase.PositionUnits.PathUnits: c.PositionUnits = UnityEngine.Splines.PathIndexUnit.Knot; break;
                case CinemachinePathBase.PositionUnits.Distance: c.PositionUnits = UnityEngine.Splines.PathIndexUnit.Distance; break;
                case CinemachinePathBase.PositionUnits.Normalized: c.PositionUnits = UnityEngine.Splines.PathIndexUnit.Normalized; break;
            }
            c.AutomaticDolly.Enabled = true;
            c.AutomaticDolly.Method = new SplineAutoDolly.FixedSpeed { Speed = m_Speed };
            c.SplinePosition = m_Position;
            if (m_Path != null)
                c.Spline = m_Path.GetComponent<UnityEngine.Splines.SplineContainer>();
        }
    }
}
#endif
