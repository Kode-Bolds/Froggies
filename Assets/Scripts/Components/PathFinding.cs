using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public struct PathFinding : IComponentData
{
    public bool requestedPath;
    public bool hasPath;
    public int2 currentNode;
    public int2 targetNode;
    public int currentIndexOnPath;
}

[InternalBufferCapacity(256)]
public struct PathNode : IBufferElementData
{
    public float3 position;
    public int2 gridPosition;
}


