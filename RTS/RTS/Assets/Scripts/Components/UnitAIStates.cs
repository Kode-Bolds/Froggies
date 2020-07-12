using Unity.Entities;

public enum AIState
{
	Idle = 0,
	MovingToPosition = 1,
	MovingToHarvest = 2,
	Harvesting = 3,
	MovingToAttack = 4,
	Attacking = 5,
	MovingToDeposit = 6
}

public struct IdleState : IComponentData { }
public struct MovingToPositionState : IComponentData { }
public struct MovingToHarvestState : IComponentData { }
public struct HarvestingState : IComponentData { }
public struct MovingToDepositState : IComponentData { }
public struct MovingToAttackState : IComponentData { }
public struct AttackingState : IComponentData { }

