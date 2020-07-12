using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

[System.Serializable]
public struct Store : IComponentData
{
    public float depositRadius;
    //Capacity?
    //Transfer multiplier?
}
