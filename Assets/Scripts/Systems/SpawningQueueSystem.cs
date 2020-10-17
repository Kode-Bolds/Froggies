using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;

[AlwaysUpdateSystem]
public class SpawningQueueSystem : KodeboldJobSystem
{
	private InputManagementSystem m_inputManagementSystem;
	private RaycastSystem m_raycastSystem;

	public NativeQueue<Translation> spawnQueue;
	public JobHandle spawnQueueDependencies;

	public override void GetSystemDependencies(Dependencies dependencies)
	{
		m_inputManagementSystem = dependencies.GetDependency<InputManagementSystem>();
		m_raycastSystem = dependencies.GetDependency<RaycastSystem>();
	}

	public override void InitSystem()
	{
		spawnQueue = new NativeQueue<Translation>(Allocator.Persistent);
	}

	public override void UpdateSystem()
	{
		Dependency = JobHandle.CombineDependencies(Dependency, m_raycastSystem.RaycastSystemDependency);

		if (m_inputManagementSystem.InputData.inputActions.spawn)
		{
			NativeArray<RaycastResult> raycastResult = m_raycastSystem.RaycastResult;
			NativeQueue<Translation> spawnQueueLocal = spawnQueue;

			Dependency = Job.WithReadOnly(raycastResult).WithCode(() =>
			{
				if (raycastResult[0].raycastTargetType == RaycastTargetType.Ground)
				{
					spawnQueueLocal.Enqueue(new Translation { Value = raycastResult[0].hitPosition + new float3(0, 1, 0) });
				}
			}).Schedule(Dependency);

			spawnQueueDependencies = Dependency;
		}
	}

	public override void FreeSystem()
	{
		spawnQueue.Dispose();
	}
}