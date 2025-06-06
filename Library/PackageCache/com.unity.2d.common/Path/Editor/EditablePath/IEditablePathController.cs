﻿using System;
using UnityEngine;

namespace UnityEditor.U2D.Common.Path
{
    internal interface IEditablePathController
    {
        IEditablePath editablePath { get; set; }
        IEditablePath closestEditablePath { get; }
        ISnapping<Vector3> snapping { get; set; }
        bool enableSnapping { get; }
        void RegisterUndo(string name);
        void ClearSelection();
        void SelectPoint(int index, bool select);
        void CreatePoint(int index, Vector3 position);
        void RemoveSelectedPoints();
        void MoveSelectedPoints(Vector3 delta);
        void MoveEdge(int index, Vector3 delta);
        void SetLeftTangent(int index, Vector3 position, bool setToLinear, bool mirror, Vector3 cachedRightTangent, TangentMode cachedTangentMode);
        void SetRightTangent(int index, Vector3 position, bool setToLinear, bool mirror, Vector3 cachedLeftTangent, TangentMode cachedTangentMode);
        void ClearClosestPath();
        void AddClosestPath(float distance);
    }
}
