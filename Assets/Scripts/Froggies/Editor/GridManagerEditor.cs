using UnityEngine;
using UnityEditor;

namespace Froggies.EditorScripts
{
    public class GridManagerEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            base.OnInspectorGUI();
        }
        
        [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected)]
        static void DrawGrid(GridManager gridManager, GizmoType gizmoType)
        {
            Gizmos.color = Color.cyan;
            for (int i = 0; i <= gridManager.m_gridSize.x; i++)
            {
                Vector3 start = gridManager.transform.position + new Vector3(gridManager.m_cellSize * i, 0, 0);
                Vector3 end = start + new Vector3(0, 0, gridManager.m_gridSize.y * gridManager.m_cellSize);

                Gizmos.DrawLine(start, end);
            }

            for (int i = 0; i <= gridManager.m_gridSize.y; i++)
            {
                Vector3 start = gridManager.transform.position + new Vector3(0, 0, gridManager.m_cellSize * i);
                Vector3 end = start + new Vector3(gridManager.m_gridSize.x * gridManager.m_cellSize, 0, 0);

                Gizmos.DrawLine(start, end);
            }
        }
    }
}