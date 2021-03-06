using System;
using System.Collections;
using System.Collections.Generic;
using Froggies;
using Kodebolds.Core;
using Unity.Collections;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEngine;

namespace Froggies.EditorScripts
{
    public class MapAuthoringEditor : EditorWindow
    {
        public static MapData map;
        public Vector2Int newGridSize;
        public int newCellSize;
        public Vector3 newOrigin;
        public Quaternion rotation;

        private const string mapdataPath = "Assets/Data/NewMap.asset";

        [MenuItem("Kodebolds/MapAuthoring")]
        public static void ShowWindow()
        {
            GetWindow(typeof(MapAuthoringEditor));
        }

        void OnEnable()
        {
            SceneView.onSceneGUIDelegate -= this.OnSceneGUI;
            SceneView.onSceneGUIDelegate += this.OnSceneGUI;
            rotation = Quaternion.identity;
        }

        void OnDestroy()
        {
            SceneView.onSceneGUIDelegate -= this.OnSceneGUI;
        }

        private void OnInspectorUpdate()
        {
            Repaint();
        }

        void OnSceneGUI(SceneView sceneView)
        {
            if (map == null)
                return;

            
            Vector2 viewportPoint = SceneView.GetAllSceneCameras()[0].WorldToViewportPoint(newOrigin);
                if (viewportPoint.x > 0 && viewportPoint.x < 1
                                    && viewportPoint.y > 0 && viewportPoint.y < 1)
            {
                Handles.TransformHandle(ref newOrigin, ref rotation);
            }
            
            //Draw real SO grid in blue
            Handles.color = Color.blue;

            for (int i = 0; i <= map.gridSize.x; i++)
            {
                Vector3 start = map.origin + new Vector3(map.cellSize * i, 0, 0);
                Vector3 end = start + new Vector3(0, 0, map.gridSize.y * map.cellSize);

                Handles.DrawLine(start, end);
            }

            for (int i = 0; i <= map.gridSize.y; i++)
            {
                Vector3 start = map.origin + new Vector3(0, 0, map.cellSize * i);
                Vector3 end = start + new Vector3(map.gridSize.x * map.cellSize, 0, 0);

                Handles.DrawLine(start, end);
            }

            if (newCellSize != map.cellSize || !newGridSize.x.Equals(map.gridSize.x) || !newGridSize.y.Equals(map.gridSize.y) || math.abs((newOrigin - map.origin).magnitude) > 0.1f)
            {
                //Draw "new" grid in red
                Handles.color = Color.red;

                for (int i = 0; i <= newGridSize.x; i++)
                {
                    Vector3 start = newOrigin + new Vector3(newCellSize * i, 0, 0);
                    Vector3 end = start + new Vector3(0, 0, newGridSize.y * newCellSize);

                    Handles.DrawLine(start, end);
                }

                for (int i = 0; i <= newGridSize.y; i++)
                {
                    Vector3 start = newOrigin + new Vector3(0, 0, newCellSize * i);
                    Vector3 end = start + new Vector3(newGridSize.x * newCellSize, 0, 0);

                    Handles.DrawLine(start, end);
                }
            }
        }

        void OnGUI()
        {
            EditorGUI.BeginChangeCheck();
            map = (MapData) EditorGUILayout.ObjectField(map, typeof(MapData), false);
             
            if (EditorGUI.EndChangeCheck())
            {
                LoadMap();
            }
            
            newGridSize = EditorGUILayout.Vector2IntField("Grid size", newGridSize);
            newCellSize = EditorGUILayout.IntField("Cell size", newCellSize);
            newOrigin = EditorGUILayout.Vector3Field("Origin", newOrigin);


            if (GUILayout.Button("Create new map"))
            {
                //Create new SO and load it
                Undo.RecordObject(this, "create new map");
                CreateMap();
            }

            if (GUILayout.Button("Update map"))
            {
                Undo.RecordObject(this, "update map");
                UpdateMap();
            }

            if (GUILayout.Button("Reset changes"))
            {
                Undo.RecordObject(this, "Reset map");
                LoadMap();
            }
        }

        private void CreateMap()
        {
            map = CreateInstance<MapData>();
            string uniquePath = AssetDatabase.GenerateUniqueAssetPath(mapdataPath);
            AssetDatabase.CreateAsset(map, uniquePath);

            UpdateMap();
        }

        private void UpdateMap()
        {
            map.cellSize = newCellSize;
            map.gridSize.x = newGridSize.x;
            map.gridSize.y = newGridSize.y;
            map.grid = new MapNode[map.gridSize.x, map.gridSize.y];
            map.origin = newOrigin;

            for (int x = 0; x < map.gridSize.x; x++)
            {
                for (int z = 0; z < map.gridSize.y; z++)
                {
                    map.grid[x, z] = new MapNode
                    {
                        position = (float3)map.origin + new float3(x * map.cellSize,
                            0, z * map.cellSize),
                        gridPosition = new int2(x + map.cellSize / 2, z + map.cellSize / 2)
                    };
                }
            }

            GridSnap[] snappers = FindObjectsOfType<GridSnap>();
            for (int i = 0; i < snappers.Length; ++i)
            {
                snappers[i].SnapToGrid();
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private void LoadMap()
        {
            newCellSize = map.cellSize;
            newGridSize.x = map.gridSize.x;
            newGridSize.y = map.gridSize.y;
            newOrigin = map.origin;
        }
    }
}