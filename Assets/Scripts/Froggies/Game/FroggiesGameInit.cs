using Kodebolds.Core;
using System.Collections.Generic;

namespace Froggies
{
	public class FroggiesGameInit : GameInit
	{
		[Unity.Entities.UpdateAfter(typeof(Unity.Physics.Systems.EndFramePhysicsSystem))]
		public class PostPhysicsSystemsGroup : Unity.Entities.ComponentSystemGroup { }

		[Unity.Entities.UpdateAfter(typeof(PostPhysicsSystemsGroup))]
		public class CommandStateProcessingSystemsGroup : Unity.Entities.ComponentSystemGroup { }

		protected override Unity.Entities.World GetOrCreateWorld()
		{
			m_worldName = "Froggies";
			Unity.Entities.World world = GetOrCreateWorldInternal();

			//INITIALISATION
			Unity.Entities.InitializationSystemGroup initSystemGroup = world.GetOrCreateSystem<Unity.Entities.InitializationSystemGroup>();

			initSystemGroup.AddSystemToUpdateList(world.GetOrCreateSystem<CameraSyncSystem>());
			initSystemGroup.AddSystemToUpdateList(world.GetOrCreateSystem<InstantiationSystem>());
			initSystemGroup.AddSystemToUpdateList(world.GetOrCreateSystem<SpawningSystem>());

			//Must run after CopyInitialTransformFromGameObjectSystem.
			initSystemGroup.AddSystemToUpdateList(world.GetOrCreateSystem<FreezeRotationSystem>());


			//SIMULATION
			Unity.Entities.SimulationSystemGroup simSystemGroup = world.GetOrCreateSystem<Unity.Entities.SimulationSystemGroup>();

			{
				PostPhysicsSystemsGroup postPhysicsSystemsGroup = world.GetOrCreateSystem<PostPhysicsSystemsGroup>();
				simSystemGroup.AddSystemToUpdateList(postPhysicsSystemsGroup);

				postPhysicsSystemsGroup.AddSystemToUpdateList(world.GetOrCreateSystem<RaycastSystem>());
				postPhysicsSystemsGroup.AddSystemToUpdateList(world.GetOrCreateSystem<SpawningQueueSystem>());
				postPhysicsSystemsGroup.AddSystemToUpdateList(world.GetOrCreateSystem<SelectionSystem>());
				postPhysicsSystemsGroup.AddSystemToUpdateList(world.GetOrCreateSystem<CameraControlSystem>());
				postPhysicsSystemsGroup.AddSystemToUpdateList(world.GetOrCreateSystem<HarvestingSystem>());
				postPhysicsSystemsGroup.AddSystemToUpdateList(world.GetOrCreateSystem<DepositSystem>());
				postPhysicsSystemsGroup.AddSystemToUpdateList(world.GetOrCreateSystem<UnitMoveSystem>());
				postPhysicsSystemsGroup.AddSystemToUpdateList(world.GetOrCreateSystem<CombatSystem>());
				postPhysicsSystemsGroup.AddSystemToUpdateList(world.GetOrCreateSystem<ProjectileSystem>());
				postPhysicsSystemsGroup.AddSystemToUpdateList(world.GetOrCreateSystem<DeathSystem>());
			}

			{
				//These systems must be updated after all the game logic systems as state transitions add/remove components via an entity command buffer, meaning the command status data
				//will be out of sync with the state components until the next sync point (command buffer system).
				//We do this step at the end to avoid an additional sync point that would halt the main thread.
				CommandStateProcessingSystemsGroup commandStateProcessingSystemsGroup = world.GetOrCreateSystem<CommandStateProcessingSystemsGroup>();
				simSystemGroup.AddSystemToUpdateList(commandStateProcessingSystemsGroup);

				commandStateProcessingSystemsGroup.AddSystemToUpdateList(world.GetOrCreateSystem<FindAITargetSystem>());
				commandStateProcessingSystemsGroup.AddSystemToUpdateList(world.GetOrCreateSystem<CommandProcessSystem>());
				commandStateProcessingSystemsGroup.AddSystemToUpdateList(world.GetOrCreateSystem<PathFindingSystem>());
				commandStateProcessingSystemsGroup.AddSystemToUpdateList(world.GetOrCreateSystem<StateTransitionSystem>());
			}


			//PRESENTATION
			Unity.Entities.PresentationSystemGroup presentationSystemGroup = world.GetOrCreateSystem<Unity.Entities.PresentationSystemGroup>();

			presentationSystemGroup.AddSystemToUpdateList(world.GetOrCreateSystem<EndFrameJobCompleteSystem>());

			Unity.Entities.ScriptBehaviourUpdateOrder.UpdatePlayerLoop(world);

			return world;
		}

		protected override List<IDependency> CreateAdditionalDependencies()
		{
			List<IDependency> additionalDependencies = new List<IDependency>();

			additionalDependencies.Add(new GameDataContainer());

			return additionalDependencies;
		}
	}
}