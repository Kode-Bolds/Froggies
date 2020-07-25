using System.Collections;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

[GenerateAuthoringComponent]
public struct StateTransitionQueue : IBufferElementData
{
	public AIState aiState;
	public TargetData target;
}
