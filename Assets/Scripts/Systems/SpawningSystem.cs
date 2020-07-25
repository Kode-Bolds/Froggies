using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

[AlwaysUpdateSystem]
public class SpawningSystem : KodeboldJobSystem
{
	private InputManagementSystem m_inputManagementSystem;
	private RaycastSystem m_raycastSystem;
	private EndSimulationEntityCommandBufferSystem m_entityCommandBuffer;
	private bool m_spawnedStartupEntities;

	public override void GetSystemDependencies(Dependencies dependencies)
	{
		m_inputManagementSystem = dependencies.GetDependency<InputManagementSystem>();
		m_raycastSystem = dependencies.GetDependency<RaycastSystem>();
	}

	public override void InitSystem()
	{
		m_entityCommandBuffer = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
	}

	public override void UpdateSystem()
	{
		if(!m_spawnedStartupEntities)
		{
			EntityCommandBuffer ecb = m_entityCommandBuffer.CreateCommandBuffer();

			Dependency = Entities.ForEach((ref OnStartPrefabData onStartPrefabData) =>
			{
				ecb.Instantiate(onStartPrefabData.resources);
			}).Schedule(Dependency);

			m_entityCommandBuffer.AddJobHandleForProducer(Dependency);
			m_spawnedStartupEntities = true;
		}

		Dependency = JobHandle.CombineDependencies(Dependency, m_raycastSystem.RaycastSystemDependency);

		if (m_inputManagementSystem.InputData.inputActions.spawn)
		{
			EntityCommandBuffer ecb = m_entityCommandBuffer.CreateCommandBuffer();
			NativeArray<RaycastResult> raycastResult = m_raycastSystem.RaycastResult;

			Dependency = Entities.WithReadOnly(raycastResult).ForEach((ref RuntimePrefabData runtimePrefabData) =>
			{
				if (raycastResult[0].raycastTargetType == RaycastTargetType.Ground)
				{
					Rotation rotation = GetComponent<Rotation>(runtimePrefabData.aiDrone);
					Translation translation = new Translation { Value = raycastResult[0].hitPosition + new float3(0, 1, 0) };

					Entity e = ecb.Instantiate(runtimePrefabData.aiDrone);

					ecb.SetComponent(e, translation);
					ecb.SetComponent(e, new LocalToWorld { Value = new float4x4(rotation.Value, translation.Value) });
				}
			}).Schedule(Dependency);

			m_entityCommandBuffer.AddJobHandleForProducer(Dependency);
		}
	}

	public override void FreeSystem()
	{

	}
}