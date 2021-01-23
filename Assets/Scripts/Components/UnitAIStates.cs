using Unity.Entities;

public enum AIState
{
	None = 0,
	Idle = 1,
	MovingToPosition = 2,
	MovingToHarvest = 3,
	Harvesting = 4,
	MovingToAttack = 5,
	Attacking = 6,
	MovingToDeposit = 7
}

public struct IdleState : IComponentData { }
public struct MovingToPositionState : IComponentData { }
public struct MovingToHarvestState : IComponentData { }
public struct HarvestingState : IComponentData { }
public struct MovingToDepositState : IComponentData { }
public struct MovingToAttackState : IComponentData { }
public struct AttackingState : IComponentData { }

