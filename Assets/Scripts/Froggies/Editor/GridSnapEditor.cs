using System.Collections;
using System.Collections.Generic;
using Froggies;
using Froggies.EditorScripts;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Froggies.EditorScripts
{
    [CustomEditor(typeof(GridSnap))]
    public class GridSnapEditor : Editor
    {
        private GridSnap m_gridSnap;
        private bool dragging = false;

        private void OnEnable()
        {
            m_gridSnap = target as GridSnap;
        }

        private void OnSceneGUI()
        {
            if (Selection.activeGameObject != m_gridSnap.gameObject) return;

            if (Event.current.type == EventType.MouseDown && Event.current.button == 0)
            {
                dragging = true;
            }

            if (dragging)
            {
                m_gridSnap.SnapToGrid();
            }

            if (Event.current.type == EventType.MouseUp && Event.current.button == 0)
            {
                m_gridSnap.SnapToGrid();
                dragging = false;
            }
        }


    }
}