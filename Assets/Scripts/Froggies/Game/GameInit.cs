using Kodebolds.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;

namespace Froggies
{
	//EXPLICITLY not adding usings here to can determine which systems here are Unity systems at first glance.
	public class GameInit : MonoBehaviour
	{
		private List<IDependency> m_dependencies;
		private List<IDependant> m_dependants;
		private GameStateManager m_gameStateManager;

		private static BehaviourUpdaterSystem m_behaviourUpdaterSystem;

		public List<KodeboldBehaviour> KodeboldBehaviours = new List<KodeboldBehaviour>();
		//TODO: Add capability for other dependencies such as SO's.

		public void OnEnable()
		{
			GetOrCreateWorld();

			m_dependencies = CreateDependencies(out GameStateManager gameStateManager);
			m_gameStateManager = gameStateManager;

			m_dependants = CreateDependants();

			//Add all dependencies that are also dependants to the dependants list.
			m_dependants.AddRange(Array.ConvertAll(m_dependencies.Where(dependency => dependency is IDependant).ToArray(), dependant => (IDependant)dependant));

			int count = m_dependants.Count;
			for (int dependantIndex = 0; dependantIndex < count; dependantIndex++)
			{
				m_dependants[dependantIndex].GetDependencies(new Dependencies(m_dependencies));
			}
		}

		private void Start()
		{
			int count = m_dependants.Count;
			for (int dependantIndex = 0; dependantIndex < count; dependantIndex++)
			{
				m_dependants[dependantIndex].Init();
			}

			m_gameStateManager.FinishInitialisation();
		}

		private void OnDestroy()
		{
			int count = m_dependencies.Count;
			for (int dependencyIndex = 0; dependencyIndex < count; dependencyIndex++)
			{
				m_dependencies[dependencyIndex].Free();
			}

			CleanupWorld();
		}

		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
		private static void CleanupWorld()
		{
			Unity.Entities.World.DisposeAllWorlds();
			WordStorage.Instance.Dispose();
			WordStorage.Instance = null;
			Unity.Entities.ScriptBehaviourUpdateOrder.UpdatePlayerLoop(null);
		}

#if UNITY_EDITOR
		[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
		private static void CreateEditorSystems()
		{
			GetOrCreateWorld().GetOrCreateSystem<EditorSystemsGroup>();
		}

		public class EditorSystemsGroup : Unity.Entities.ComponentSystemGroup
		{
			protected override void OnCreate()
			{
				Unity.Scenes.Editor.LiveLinkEditorSystemGroup liveLinkEditorSystemGroup = World.GetOrCreateSystem<Unity.Scenes.Editor.LiveLinkEditorSystemGroup>();
				liveLinkEditorSystemGroup.AddSystemToUpdateList(World.GetExistingSystem<Unity.Scenes.Editor.EditorSubSceneLiveLinkSystem>());
			}
		}
#endif

		[Unity.Entities.UpdateAfter(typeof(Unity.Physics.Systems.EndFramePhysicsSystem))]
		public class PostPhysicsSystemsGroup : Unity.Entities.ComponentSystemGroup { }

		[Unity.Entities.UpdateAfter(typeof(PostPhysicsSystemsGroup))]
		public class CommandStateProcessingSystemsGroup : Unity.Entities.ComponentSystemGroup { }

		[Unity.Entities.UpdateAfter(typeof(Unity.Transforms.TransformSystemGroup))]
		public class BehaviourUpdaterSystemsGroup : Unity.Entities.ComponentSystemGroup { }


		private static Unity.Entities.World GetOrCreateWorld()
		{
			if (Unity.Entities.World.DefaultGameObjectInjectionWorld != null && Unity.Entities.World.DefaultGameObjectInjectionWorld.Name == "KodeboldsWorld")
				return Unity.Entities.World.DefaultGameObjectInjectionWorld;

			Unity.Entities.World world = new Unity.Entities.World("KodeboldsWorld");
			Unity.Entities.World.DefaultGameObjectInjectionWorld = world;

			//INITIALISATION
			Unity.Entities.InitializationSystemGroup initSystemGroup = world.GetOrCreateSystem<Unity.Entities.InitializationSystemGroup>();

			initSystemGroup.AddSystemToUpdateList(world.GetOrCreateSystem<Unity.Entities.BeginInitializationEntityCommandBufferSystem>());
			initSystemGroup.AddSystemToUpdateList(world.GetOrCreateSystem<Unity.Entities.ConvertToEntitySystem>());
			initSystemGroup.AddSystemToUpdateList(world.GetOrCreateSystem<CameraSyncSystem>());
			initSystemGroup.AddSystemToUpdateList(world.GetOrCreateSystem<InputManagementSystem>());
			initSystemGroup.AddSystemToUpdateList(world.GetOrCreateSystem<InstantiationSystem>());
			initSystemGroup.AddSystemToUpdateList(world.GetOrCreateSystem<SpawningSystem>());
			initSystemGroup.AddSystemToUpdateList(world.GetOrCreateSystem<Unity.Entities.RetainBlobAssetSystem>());
			initSystemGroup.AddSystemToUpdateList(world.GetOrCreateSystem<Unity.Entities.UpdateWorldTimeSystem>());

			{
				Unity.Scenes.SceneSystemGroup sceneSystemGroup = world.GetOrCreateSystem<Unity.Scenes.SceneSystemGroup>();
				initSystemGroup.AddSystemToUpdateList(sceneSystemGroup);

				sceneSystemGroup.AddSystemToUpdateList(world.GetOrCreateSystem<Unity.Scenes.SceneSystem>());
				sceneSystemGroup.AddSystemToUpdateList(world.GetOrCreateSystem<Unity.Scenes.ResolveSceneReferenceSystem>());
				sceneSystemGroup.AddSystemToUpdateList(world.GetOrCreateSystem<Unity.Scenes.SceneSectionStreamingSystem>());
			}

			initSystemGroup.AddSystemToUpdateList(world.GetOrCreateSystem<Unity.Transforms.CopyInitialTransformFromGameObjectSystem>());

			//Must run after CopyInitialTransformFromGameObjectSystem.
			initSystemGroup.AddSystemToUpdateList(world.GetOrCreateSystem<FreezeRotationSystem>());

			initSystemGroup.AddSystemToUpdateList(world.GetOrCreateSystem<Unity.Entities.EndInitializationEntityCommandBufferSystem>());

			//SIMULATION
			Unity.Entities.SimulationSystemGroup simSystemGroup = world.GetOrCreateSystem<Unity.Entities.SimulationSystemGroup>();


			simSystemGroup.AddSystemToUpdateList(world.GetOrCreateSystem<Unity.Entities.BeginSimulationEntityCommandBufferSystem>());
			//simSystemGroup.AddSystemToUpdateList(world.GetOrCreateSystem<DebugStream>()); //Unity system, they just didn't put it in a namespace hurr durr.
			simSystemGroup.AddSystemToUpdateList(world.GetOrCreateSystem<Unity.Physics.Systems.BuildPhysicsWorld>());
			simSystemGroup.AddSystemToUpdateList(world.GetOrCreateSystem<Unity.Physics.Authoring.DisplayJointsSystem>());
			simSystemGroup.AddSystemToUpdateList(world.GetOrCreateSystem<Unity.Physics.Systems.StepPhysicsWorld>());
			simSystemGroup.AddSystemToUpdateList(world.GetOrCreateSystem<Unity.Physics.Systems.ExportPhysicsWorld>());
			simSystemGroup.AddSystemToUpdateList(world.GetOrCreateSystem<Unity.Physics.Authoring.DisplayContactsSystem>());
			simSystemGroup.AddSystemToUpdateList(world.GetOrCreateSystem<Unity.Physics.Authoring.DisplayBroadphaseAabbsSystem>());
			simSystemGroup.AddSystemToUpdateList(world.GetOrCreateSystem<Unity.Physics.Authoring.DisplayColliderAabbsSystem>());
			simSystemGroup.AddSystemToUpdateList(world.GetOrCreateSystem<Unity.Physics.Authoring.DisplayBodyColliders>());
			simSystemGroup.AddSystemToUpdateList(world.GetOrCreateSystem<Unity.Physics.Authoring.DisplayCollisionEventsSystem>());
			simSystemGroup.AddSystemToUpdateList(world.GetOrCreateSystem<Unity.Physics.Authoring.DisplayContactsSystem>());
			simSystemGroup.AddSystemToUpdateList(world.GetOrCreateSystem<Unity.Physics.Authoring.DisplayMassPropertiesSystem>());
			simSystemGroup.AddSystemToUpdateList(world.GetOrCreateSystem<Unity.Physics.Authoring.DisplayTriggerEventsSystem>());
			simSystemGroup.AddSystemToUpdateList(world.GetOrCreateSystem<Unity.Physics.Systems.EndFramePhysicsSystem>());

			{
				PostPhysicsSystemsGroup postPhysicsSystemsGroup = world.GetOrCreateSystem<PostPhysicsSystemsGroup>();
				simSystemGroup.AddSystemToUpdateList(postPhysicsSystemsGroup);

				postPhysicsSystemsGroup.AddSystemToUpdateList(world.GetOrCreateSystem<RaycastSystem>());
				postPhysicsSystemsGroup.AddSystemToUpdateList(world.GetOrCreateSystem<SpawningQueueSystem>());
				postPhysicsSystemsGroup.AddSystemToUpdateList(world.GetOrCreateSystem<SelectionSystem>());
				postPhysicsSystemsGroup.AddSystemToUpdateList(world.GetOrCreateSystem<CameraControlSystem>());
				postPhysicsSystemsGroup.AddSystemToUpdateList(world.GetOrCreateSystem<PathFindingSystem>());
				postPhysicsSystemsGroup.AddSystemToUpdateList(world.GetOrCreateSystem<HarvestingSystem>());
				postPhysicsSystemsGroup.AddSystemToUpdateList(world.GetOrCreateSystem<DepositSystem>());
				postPhysicsSystemsGroup.AddSystemToUpdateList(world.GetOrCreateSystem<UnitMoveSystem>());
			}

			{
				//These systems must be updated after all the game logic systems as state transitions add/remove components via an entity command buffer, meaning the command status data
				//will be out of sync with the state components until the next sync point (command buffer system).
				//We do this step at the end to avoid an additional sync point that would halt the main thread.
				CommandStateProcessingSystemsGroup commandStateProcessingSystemsGroup = world.GetOrCreateSystem<CommandStateProcessingSystemsGroup>();
				simSystemGroup.AddSystemToUpdateList(commandStateProcessingSystemsGroup);

				commandStateProcessingSystemsGroup.AddSystemToUpdateList(world.GetOrCreateSystem<FindAITargetSystem>());
				commandStateProcessingSystemsGroup.AddSystemToUpdateList(world.GetOrCreateSystem<CommandProcessSystem>());
				commandStateProcessingSystemsGroup.AddSystemToUpdateList(world.GetOrCreateSystem<StateTransitionSystem>());
			}

			{
				Unity.Transforms.TransformSystemGroup transformSystemGroup = world.GetOrCreateSystem<Unity.Transforms.TransformSystemGroup>();
				simSystemGroup.AddSystemToUpdateList(transformSystemGroup);

				transformSystemGroup.AddSystemToUpdateList(world.GetOrCreateSystem<Unity.Transforms.CopyTransformFromGameObjectSystem>());
				transformSystemGroup.AddSystemToUpdateList(world.GetOrCreateSystem<Unity.Transforms.EndFrameCompositeScaleSystem>());
				transformSystemGroup.AddSystemToUpdateList(world.GetOrCreateSystem<Unity.Transforms.EndFrameParentSystem>());
				transformSystemGroup.AddSystemToUpdateList(world.GetOrCreateSystem<Unity.Transforms.EndFramePostRotationEulerSystem>());
				transformSystemGroup.AddSystemToUpdateList(world.GetOrCreateSystem<Unity.Transforms.EndFrameRotationEulerSystem>());
				transformSystemGroup.AddSystemToUpdateList(world.GetOrCreateSystem<Unity.Transforms.EndFrameCompositeRotationSystem>());
				transformSystemGroup.AddSystemToUpdateList(world.GetOrCreateSystem<Unity.Transforms.EndFrameParentScaleInverseSystem>());
				transformSystemGroup.AddSystemToUpdateList(world.GetOrCreateSystem<Unity.Transforms.EndFrameTRSToLocalToParentSystem>());
				transformSystemGroup.AddSystemToUpdateList(world.GetOrCreateSystem<Unity.Transforms.EndFrameTRSToLocalToWorldSystem>());
				transformSystemGroup.AddSystemToUpdateList(world.GetOrCreateSystem<Unity.Transforms.EndFrameLocalToParentSystem>());
				transformSystemGroup.AddSystemToUpdateList(world.GetOrCreateSystem<Unity.Transforms.CopyTransformToGameObjectSystem>());
				transformSystemGroup.AddSystemToUpdateList(world.GetOrCreateSystem<Unity.Transforms.EndFrameWorldToLocalSystem>());
			}

			{
				BehaviourUpdaterSystemsGroup behaviourUpdaterSystemsGroup = world.GetOrCreateSystem<BehaviourUpdaterSystemsGroup>();
				simSystemGroup.AddSystemToUpdateList(behaviourUpdaterSystemsGroup);

				m_behaviourUpdaterSystem = world.GetOrCreateSystem<BehaviourUpdaterSystem>();

				behaviourUpdaterSystemsGroup.AddSystemToUpdateList(m_behaviourUpdaterSystem);
			}

			simSystemGroup.AddSystemToUpdateList(world.GetOrCreateSystem<Unity.Entities.EndSimulationEntityCommandBufferSystem>());


			//PRESENTATION
			Unity.Entities.PresentationSystemGroup presentationSystemGroup = world.GetOrCreateSystem<Unity.Entities.PresentationSystemGroup>();

			presentationSystemGroup.AddSystemToUpdateList(world.GetOrCreateSystem<Unity.Entities.BeginPresentationEntityCommandBufferSystem>());

			{
				Unity.Rendering.StructuralChangePresentationSystemGroup structuralChangePresentationSystemGroup = world.GetOrCreateSystem<Unity.Rendering.StructuralChangePresentationSystemGroup>();
				presentationSystemGroup.AddSystemToUpdateList(structuralChangePresentationSystemGroup);

				structuralChangePresentationSystemGroup.AddSystemToUpdateList(world.GetOrCreateSystem<Unity.Rendering.AddWorldAndChunkRenderBounds>());
				structuralChangePresentationSystemGroup.AddSystemToUpdateList(world.GetOrCreateSystem<Unity.Rendering.AddLodRequirementComponents>());
			}

			{
				Unity.Rendering.UpdatePresentationSystemGroup updatePresentationSystemGroup = world.GetOrCreateSystem<Unity.Rendering.UpdatePresentationSystemGroup>();
				presentationSystemGroup.AddSystemToUpdateList(updatePresentationSystemGroup);

				updatePresentationSystemGroup.AddSystemToUpdateList(world.GetOrCreateSystem<Unity.Rendering.RenderBoundsUpdateSystem>());
				updatePresentationSystemGroup.AddSystemToUpdateList(world.GetOrCreateSystem<Unity.Rendering.LodRequirementsUpdateSystem>());
			}

			presentationSystemGroup.AddSystemToUpdateList(world.GetOrCreateSystem<Unity.Rendering.RenderMeshSystemV2>());
			presentationSystemGroup.AddSystemToUpdateList(world.GetOrCreateSystem<EndFrameJobCompleteSystem>());

			Unity.Entities.ScriptBehaviourUpdateOrder.UpdatePlayerLoop(world);

			return world;
		}

		private List<IDependency> CreateDependencies(out GameStateManager gameStateManager)
		{
			List<IDependency> dependencies = new List<IDependency>();

			gameStateManager = new GameStateManager();
			dependencies.Add(gameStateManager);

			List<KodeboldBehaviour> kodeboldBehaviours = CreateBehaviours();
			m_behaviourUpdaterSystem.SetBehavioursList(kodeboldBehaviours);
			dependencies.AddRange(kodeboldBehaviours);

			//Get all our created Kodebold systems and add them into the dependencies
			Unity.Entities.World world = Unity.Entities.World.DefaultGameObjectInjectionWorld;
			int systemCount = world.Systems.Count;
			for (int systemIndex = 0; systemIndex < systemCount; systemIndex++)
				if (world.Systems[systemIndex] is KodeboldJobSystem system)
					dependencies.Add(system);

			return dependencies;
		}

		private List<IDependant> CreateDependants()
		{
			return new List<IDependant>();
		}

		private List<KodeboldBehaviour> CreateBehaviours()
		{
			List<KodeboldBehaviour> kodeboldBehaviours = new List<KodeboldBehaviour>();
			GameObject behaviourContainer = new GameObject("Behaviours");

			foreach (KodeboldBehaviour kodeboldBehaviour in KodeboldBehaviours)
			{
				kodeboldBehaviours.Add(Instantiate(kodeboldBehaviour, behaviourContainer.transform));
			}

			return kodeboldBehaviours;
		}
	}
}