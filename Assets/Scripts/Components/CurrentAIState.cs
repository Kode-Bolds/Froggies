using Unity.Entities;


public struct CurrentAIState : IComponentData
{
	public AIState currentAIState;
	public AIState requestedAIState;
	public TargetData requestedAIStateTargetData;

	public void RequestStateChange(AIState requestedState, TargetData targetData = default)
	{
		requestedAIState = requestedState;
		requestedAIStateTargetData = targetData;
	}

	public void CompleteStateChange()
	{
		requestedAIState = AIState.None;
		requestedAIStateTargetData = default;
	}
}
