using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;


public struct StateTransition : IBufferElementData
{
    public Entity entity;
	public AIState aiState;
	public TargetData target;
}
