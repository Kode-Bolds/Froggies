using System;
using System.Collections;
using System.Collections.Generic;
using Froggies;
using Kodebolds.Core;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

[CreateAssetMenu(menuName = "Data/MapData", fileName = "Assets/Data/NewMap")]
public class MapData : ScriptableObject
{
    public int cellSize;
    public int2 gridSize;
    public MapNode [,] grid;
    public Vector3 origin;
}
