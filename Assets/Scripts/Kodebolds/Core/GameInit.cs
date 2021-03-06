using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using UnityEngine;

namespace Kodebolds.Core
{
	//EXPLICITLY not adding usings here so we can determine which systems here are Unity systems at first glance.
	public abstract class GameInit : MonoBehaviour
	{
		protected static string m_worldName = "Kodebolds";

		protected List<IDependency> m_dependencies;
		protected List<IDependant> m_dependants;
		protected GameStateManager m_gameStateManager;

		protected static InitialisationBehaviourUpdaterSystem m_initialisationBehaviourUpdaterSystem;
		protected static UpdateBehaviourUpdaterSystem m_updateBehaviourUpdaterSystem;
		public GameObject BehaviourContainer;


		public List<KodeboldBehaviour> InitialisationKodeboldBehaviours = new List<KodeboldBehaviour>();
		public List<KodeboldBehaviour> UpdateKodeboldBehaviours = new List<KodeboldBehaviour>();
		public List<KodeboldSO> KodeboldScriptableObjects = new List<KodeboldSO>();

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
				if (m_dependencies[dependencyIndex] is KodeboldJobSystem system)
					system.Free();
				else if (m_dependencies[dependencyIndex] is KodeboldBehaviour behaviour)
					behaviour.Free();
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
			GetOrCreateWorldInternal().GetOrCreateSystem<EditorSystemsGroup>();
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

		protected static Unity.Entities.World GetOrCreateWorldInternal()
		{
			if (Unity.Entities.World.DefaultGameObjectInjectionWorld != null && Unity.Entities.World.DefaultGameObjectInjectionWorld.Name == m_worldName)
				return Unity.Entities.World.DefaultGameObjectInjectionWorld;

			Unity.Entities.World world = new Unity.Entities.World(m_worldName);
			Unity.Entities.World.DefaultGameObjectInjectionWorld = world;

			//INITIALISATION
			Unity.Entities.InitializationSystemGroup initSystemGroup = world.GetOrCreateSystem<Unity.Entities.InitializationSystemGroup>();

			initSystemGroup.AddSystemToUpdateList(world.GetOrCreateSystem<Unity.Entities.BeginInitializationEntityCommandBufferSystem>());
			initSystemGroup.AddSystemToUpdateList(world.GetOrCreateSystem<Unity.Entities.ConvertToEntitySystem>());
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

			m_initialisationBehaviourUpdaterSystem = world.GetOrCreateSystem<InitialisationBehaviourUpdaterSystem>();
			initSystemGroup.AddSystemToUpdateList(m_initialisationBehaviourUpdaterSystem);

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

			m_updateBehaviourUpdaterSystem = world.GetOrCreateSystem<UpdateBehaviourUpdaterSystem>();
			simSystemGroup.AddSystemToUpdateList(m_updateBehaviourUpdaterSystem);

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

		protected abstract Unity.Entities.World GetOrCreateWorld();

		private List<IDependency> CreateDependencies(out GameStateManager gameStateManager)
		{
			List<IDependency> dependencies = new List<IDependency>();

			dependencies.Add(gameStateManager = new GameStateManager());

			dependencies.AddRange(CreateAdditionalDependencies());
			dependencies.AddRange(CreateBehaviours());
			dependencies.AddRange(KodeboldScriptableObjects);

			//Get all our created Kodebold systems and add them into the dependencies
			Unity.Entities.World world = Unity.Entities.World.DefaultGameObjectInjectionWorld;
			int systemCount = world.Systems.Count;
			for (int systemIndex = 0; systemIndex < systemCount; systemIndex++)
				if (world.Systems[systemIndex] is KodeboldJobSystem system)
					dependencies.Add(system);

			return dependencies;
		}

		protected abstract List<IDependency> CreateAdditionalDependencies();

		private List<IDependant> CreateDependants()
		{
			return new List<IDependant>();
		}

		private List<KodeboldBehaviour> CreateBehaviours()
		{
			List<KodeboldBehaviour> initKodeboldBehaviours = new List<KodeboldBehaviour>();
			List<KodeboldBehaviour> updateKodeboldBehaviours = new List<KodeboldBehaviour>();

			foreach (KodeboldBehaviour kodeboldBehaviour in InitialisationKodeboldBehaviours)
			{
				initKodeboldBehaviours.Add(Instantiate(kodeboldBehaviour, BehaviourContainer.transform));
			}
			m_initialisationBehaviourUpdaterSystem.SetBehavioursList(initKodeboldBehaviours);
			updateKodeboldBehaviours.AddRange(BehaviourContainer.GetComponentsInChildren<KodeboldBehaviour>());

			foreach (KodeboldBehaviour kodeboldBehaviour in UpdateKodeboldBehaviours)
			{
				updateKodeboldBehaviours.Add(Instantiate(kodeboldBehaviour, BehaviourContainer.transform));
			}
			m_updateBehaviourUpdaterSystem.SetBehavioursList(updateKodeboldBehaviours);

			return initKodeboldBehaviours.Concat(updateKodeboldBehaviours).ToList();
		}
	}
}