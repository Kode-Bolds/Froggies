using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public unsafe struct MapNode 
{
   public MapNode* parent;
   public MapNode* child;
   public float3 position;
   public int2 gridPosition;
   public OccupiedBy occupiedBy;    
   public NodeState state;
   public float g;
   public float h;
   public float f;
}

public enum NodeState
{
    Untested,
    Closed,
    Open
}
public enum OccupiedBy
{
  Nothing = 0,
  Unit = 1,
  Building = 2,
  Environment = 3
}

