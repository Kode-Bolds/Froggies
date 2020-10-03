using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;

public enum RaycastTargetType
{
	Nothing = 0,
	Unit = 1,
	Ground = 2,
	ResourceNode = 3,
	Enemy = 4
}

public struct RaycastResult
{
	public RaycastTargetType raycastTargetType;
	public Entity raycastTargetEntity;
	public float3 hitPosition;
}

public class RaycastSystem : KodeboldJobSystem
{
	private EntityQuery m_entityQuery;
	private InputManagementSystem m_inputManagementSystem;
	private EndFrameJobCompleteSystem endFrameJobCompleteSystem;
	private BuildPhysicsWorld m_physicsWorldBuilder;
	private EndFramePhysicsSystem endFramePhysicsSystem;

	private CollisionFilter m_collisionFilter;
	private CollisionFilter m_selectionCollisionFilter;
	private NativeArray<RaycastResult> m_raycastResult;
	private NativeArray<RaycastResult> m_selectionRaycastResult;
	public NativeArray<RaycastResult> RaycastResult => m_raycastResult;
	public NativeArray<RaycastResult> SelectionRaycastResult => m_selectionRaycastResult;

	private JobHandle m_raycastSystemDependency;
	public JobHandle RaycastSystemDependency => m_raycastSystemDependency;

	public override void GetSystemDependencies(Dependencies dependencies)
	{
		m_inputManagementSystem = dependencies.GetDependency<InputManagementSystem>();
		endFrameJobCompleteSystem = dependencies.GetDependency<EndFrameJobCompleteSystem>();
	}

	public override void InitSystem()
	{
		m_physicsWorldBuilder = World.GetOrCreateSystem<BuildPhysicsWorld>();
		endFramePhysicsSystem = World.GetOrCreateSystem<EndFramePhysicsSystem>();

		m_collisionFilter = new CollisionFilter
		{
			BelongsTo = PhysicsCategories.MouseRaycast,
			CollidesWith = PhysicsCategories.Ground | PhysicsCategories.Units | PhysicsCategories.ResourceNode
		};
		m_selectionCollisionFilter = new CollisionFilter
		{
			BelongsTo = PhysicsCategories.MouseRaycast,
			CollidesWith = PhysicsCategories.Ground
		};

		m_raycastResult = new NativeArray<RaycastResult>(1, Allocator.Persistent);
		m_selectionRaycastResult = new NativeArray<RaycastResult>(1, Allocator.Persistent);

		m_entityQuery = GetEntityQuery(ComponentType.ReadOnly<Translation>(), ComponentType.ReadOnly<Camera>());
	}

	public override void UpdateSystem()
	{
		CollisionWorld collisionWorld = m_physicsWorldBuilder.PhysicsWorld.CollisionWorld;
		NativeArray<Translation> cameraTranslation = m_entityQuery.ToComponentDataArrayAsync<Translation>(Allocator.TempJob, out JobHandle cameraTranslationHandle);
		float3 mousePos = m_inputManagementSystem.InputData.mouseInput.mouseWorldPos;
		CollisionFilter collisionFilter = m_collisionFilter;
		NativeArray<RaycastResult> raycastResult = m_raycastResult;

		ComponentDataFromEntity<UnitTag> unitTagLookup = GetComponentDataFromEntity<UnitTag>();
		Dependency = JobHandle.CombineDependencies(Dependency, cameraTranslationHandle);

		Dependency = Job
		.WithReadOnly(cameraTranslation)
		.WithCode(() =>
		{
			float3 cameraPos = cameraTranslation[0].Value;
			RaycastResult result;
			if (InputManagementSystem.CastRayFromMouse(cameraPos, mousePos, 1000.0f, out Unity.Physics.RaycastHit closestHit, collisionFilter, collisionWorld))
            {
                Entity hitEntity = closestHit.Entity;

                if(HasComponent<EnemyTag>(hitEntity))
                {
                    result = new RaycastResult
                    {
                        raycastTargetType = RaycastTargetType.Enemy,
                        hitPosition = closestHit.Position,
                        raycastTargetEntity = closestHit.Entity
                    };
                    raycastResult[0] = result;

                    return;
                }

                if (HasComponent<UnitTag>(hitEntity))
                {
                    result = new RaycastResult
                    {
                        raycastTargetType = RaycastTargetType.Unit,
                        hitPosition = closestHit.Position,
                        raycastTargetEntity = closestHit.Entity
                    };
                    raycastResult[0] = result;

                    return;
                }

                if (HasComponent<ResourceNode>(hitEntity))
                {
                    result = new RaycastResult
                    {
                        raycastTargetType = RaycastTargetType.ResourceNode,
                        hitPosition = closestHit.Position,
                        raycastTargetEntity = closestHit.Entity
                    };
                    raycastResult[0] = result;

                    return;
                }
				result = new RaycastResult
                {
                    raycastTargetType = RaycastTargetType.Ground,
                    hitPosition = closestHit.Position,
                    raycastTargetEntity = closestHit.Entity
                };
                raycastResult[0] = result;

            }
            else
            {
                result = new RaycastResult
                {
                    raycastTargetType = RaycastTargetType.Nothing
                };
                raycastResult[0] = result;
            }
		}).Schedule(JobHandle.CombineDependencies(Dependency, endFramePhysicsSystem.GetOutputDependency()));


		CollisionFilter selectionFilter = m_selectionCollisionFilter;
		NativeArray<RaycastResult> selectionRaycastResult = m_selectionRaycastResult;

		Dependency = Job
		.WithDeallocateOnJobCompletion(cameraTranslation)
		.WithReadOnly(cameraTranslation)
		.WithCode(() =>
		{
			float3 cameraPos = cameraTranslation[0].Value;
			RaycastResult result;
			if (InputManagementSystem.CastRayFromMouse(cameraPos, mousePos, 1000.0f, out Unity.Physics.RaycastHit closestHit, selectionFilter, collisionWorld))
			{
				result = new RaycastResult
				{
					raycastTargetType = RaycastTargetType.Ground,
					hitPosition = closestHit.Position,
					raycastTargetEntity = closestHit.Entity
				};
				selectionRaycastResult[0] = result;
			}
			else
			{
				result = new RaycastResult
				{
					raycastTargetType = RaycastTargetType.Nothing
				};
				selectionRaycastResult[0] = result;
			}

		}).Schedule(Dependency);

		m_raycastSystemDependency = Dependency;

		//Not entirely sure why this is needed, physics system doesn't seem to recognise that this job is a dependency for it in the next frame.
		//There's no way to pass it manually so have to complete at end of frame. Potentially caused by using Job.WithCode() instead of Entities.ForEach().
		endFrameJobCompleteSystem.AddJobHandleToComplete(m_raycastSystemDependency);
	}

	public override void FreeSystem()
	{
		m_raycastResult.Dispose();
		m_selectionRaycastResult.Dispose();
	}
}
