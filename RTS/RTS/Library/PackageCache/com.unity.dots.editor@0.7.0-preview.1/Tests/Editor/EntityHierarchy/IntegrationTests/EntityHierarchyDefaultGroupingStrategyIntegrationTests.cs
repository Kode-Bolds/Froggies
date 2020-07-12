using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Unity.Collections;
using Unity.Scenes;
using Unity.Scenes.Editor;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Unity.Entities.Editor.Tests
{
    [TestFixture]
    [SuppressMessage("ReSharper", "Unity.InefficientPropertyAccess")]
    class EntityHierarchyDefaultGroupingStrategyIntegrationTests
    {
        const string k_AssetsFolderRoot = "Assets";
        const string k_SceneExtension = "unity";

        const string k_SceneName = "DefaultGroupingStrategyTest";
        const string k_SubSceneName = "SubScene";

        string m_TestAssetsDirectory;

        Scene m_Scene;
        SubScene m_SubScene;
        GameObject m_SubSceneRoot;
        bool m_PreviousLiveLinkState;
        World m_PreviousWorld;
        EntityManager m_Manager;
        TestHierarchyHelper m_AssertHelper;

        MockHierarchy m_Container;

        IEnumerator UpdateLiveLink()
        {
            LiveLinkConnection.GlobalDirtyLiveLink();
            yield return SkipAnEditorFrameAndUpdateWorld();
        }

        IEnumerator SkipAnEditorFrameAndUpdateWorld()
        {
            yield return SkipAnEditorFrame();
            m_Container.World.Update();
        }

        static IEnumerator SkipAnEditorFrame()
        {
            EditorUpdateUtility.EditModeQueuePlayerLoopUpdate();

            // Yield twice to ensure EditorApplication.update was invoked before resuming.
            yield return null;
            yield return null;
        }

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            string path;
            do
            {
                path = Path.GetRandomFileName();
            }
            while (AssetDatabase.IsValidFolder(Path.Combine(k_AssetsFolderRoot, path)));

            m_PreviousLiveLinkState = SubSceneInspectorUtility.LiveLinkEnabledInEditMode;
            SubSceneInspectorUtility.LiveLinkEnabledInEditMode = true;

            var guid = AssetDatabase.CreateFolder(k_AssetsFolderRoot, path);
            m_TestAssetsDirectory = AssetDatabase.GUIDToAssetPath(guid);

            m_Scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);
            var mainScenePath = Path.Combine(m_TestAssetsDirectory, $"{k_SceneName}.{k_SceneExtension}");
            EditorSceneManager.SaveScene(m_Scene, mainScenePath);
            SceneManager.SetActiveScene(m_Scene);

            // Temp context GameObject, necessary to create an empty subscene
            var targetGO = new GameObject(k_SubSceneName);

            var subsceneArgs = new SubSceneContextMenu.NewSubSceneArgs(targetGO, m_Scene, SubSceneContextMenu.NewSubSceneMode.EmptyScene);
            m_SubScene = SubSceneContextMenu.CreateNewSubScene(targetGO.name, subsceneArgs, InteractionMode.AutomatedAction);

            m_SubSceneRoot = m_SubScene.gameObject;

            Object.DestroyImmediate(targetGO);
            EditorSceneManager.SaveScene(m_Scene);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            Object.DestroyImmediate(m_SubSceneRoot);
            AssetDatabase.DeleteAsset(m_TestAssetsDirectory);
            SceneWithBuildConfigurationGUIDs.ClearBuildSettingsCache();
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene);

            SubSceneInspectorUtility.LiveLinkEnabledInEditMode = m_PreviousLiveLinkState;
        }

        [SetUp]
        public virtual void Setup()
        {
            m_PreviousWorld = World.DefaultGameObjectInjectionWorld;
            DefaultWorldInitialization.Initialize("Test World", true);
            var world = World.DefaultGameObjectInjectionWorld;
            m_Manager = world.EntityManager;

            m_Container = new MockHierarchy
            {
                Strategy = new EntityHierarchyDefaultGroupingStrategy(world),
                QueryDesc = null,
                World = world
            };

            m_AssertHelper = new TestHierarchyHelper(m_Container.Strategy);
            EntityHierarchyDiffSystem.Register(m_Container);

            world.GetOrCreateSystem<SceneSystem>().LoadSceneAsync(m_SubScene.SceneGUID, new SceneSystem.LoadParameters
            {
                Flags = SceneLoadFlags.BlockOnImport | SceneLoadFlags.BlockOnStreamIn
            });

            world.Update();
        }

        [TearDown]
        public void TearDown()
        {
            EntityHierarchyDiffSystem.Unregister(m_Container);
            m_Container.Strategy.Dispose();
            m_Container.Strategy = null;
            m_AssertHelper = null;

            m_Container.World.Dispose();
            m_Container.World = null;

            World.DefaultGameObjectInjectionWorld = m_PreviousWorld;
            m_PreviousWorld = null;
            m_Manager = default;

            ScriptBehaviourUpdateOrder.UpdatePlayerLoop(null);

            TearDownSubScene();
        }

        void TearDownSubScene()
        {
            foreach (GameObject rootGO in m_SubScene.EditingScene.GetRootGameObjects())
                Object.DestroyImmediate(rootGO);
        }

        [UnityTest]
        public IEnumerator TestSetup_ProducesExpectedResult()
        {
            // Initial setup is clean and all is as expected
            Assert.That(m_SubScene.name, Is.EqualTo(k_SubSceneName));
            Assert.That(m_SubScene.SceneName, Is.EqualTo(k_SubSceneName));
            Assert.That(m_SubScene.CanBeLoaded(), Is.True);
            Assert.That(m_SubScene.IsLoaded, Is.True);
            Assert.That(m_SubScene.EditingScene.isLoaded, Is.True);
            Assert.That(m_SubScene.EditingScene.isSubScene, Is.True);

            Assert.That(m_SubSceneRoot.name, Is.EqualTo(k_SubSceneName));
            Assert.That(m_SubSceneRoot, Is.EqualTo(m_SubScene.gameObject));

            Assert.That(m_SubSceneRoot.GetComponent<SubScene>(), Is.Not.Null);
            Assert.That(m_SubSceneRoot.transform.childCount, Is.EqualTo(0));

            Assert.That(m_Scene.rootCount, Is.EqualTo(1));
            Assert.That(m_SubScene.EditingScene.rootCount, Is.EqualTo(0));

            // Adding a GameObject to a SubScene
            var go = new GameObject("go");
            SceneManager.MoveGameObjectToScene(go, m_SubScene.EditingScene);

            Assert.That(m_SubScene.EditingScene.rootCount, Is.EqualTo(1));
            Assert.That(m_SubScene.EditingScene.GetRootGameObjects()[0], Is.EqualTo(go));
            Assert.That(go.scene, Is.EqualTo(m_SubScene.EditingScene));

            // Parenting into a SubScene
            var childGO = new GameObject("childGO");
            Assert.That(childGO.scene, Is.EqualTo(m_Scene));

            childGO.transform.parent = go.transform;
            Assert.That(childGO.scene, Is.EqualTo(m_SubScene.EditingScene));

            // Expected Entities: 1. WorldTime - 2. SubScene - 3. SceneSection
            Assert.That(m_Manager.UniversalQuery.CalculateEntityCount(), Is.EqualTo(3));

            yield return UpdateLiveLink();

            // Expected Entities: 1. WorldTime - 2. SubScene - 3. SceneSection - 4. Converted `go` - 5. Converted `childGO`
            Assert.That(m_Manager.UniversalQuery.CalculateEntityCount(), Is.EqualTo(5));

            // TearDown properly cleans-up the SubScene
            TearDownSubScene();

            Assert.That(m_SubScene.EditingScene.rootCount, Is.EqualTo(0));

            yield return UpdateLiveLink();

            // Expected Entities: 1. WorldTime - 2. SubScene - 3. SceneSection
            Assert.That(m_Manager.UniversalQuery.CalculateEntityCount(), Is.EqualTo(3));
        }

        [UnityTest]
        public IEnumerator BasicSubsceneBasedParenting_ProducesExpectedResult()
        {
            // Adding a GameObject to a SubScene
            var go = new GameObject("go");
            SceneManager.MoveGameObjectToScene(go, m_SubScene.EditingScene);

            yield return UpdateLiveLink();

            var (expectedHierarchy, subScene, _, entityId) = CreateBaseHierarchyForSubscene();
            subScene.AddChild(new EntityHierarchyNodeId(NodeKind.Entity, entityId)); // "go" Converted Entity

            m_AssertHelper.AssertHierarchyByKind(expectedHierarchy.Build());

            // Asserts that the names of the scenes were correctly found.
            using (var rootChildren = m_Container.Strategy.GetChildren(EntityHierarchyNodeId.Root, Allocator.Temp))
            {
                var sceneNode = rootChildren.Single(child => child.Kind == NodeKind.Scene);
                Assert.That(m_Container.Strategy.GetNodeName(sceneNode), Is.EqualTo(k_SceneName));

                using (var sceneChildren = m_Container.Strategy.GetChildren(sceneNode, Allocator.Temp))
                {
                    // Only Expecting a single child here
                    var subsceneNode = sceneChildren[0];
                    Assert.That(m_Container.Strategy.GetNodeName(subsceneNode), Is.EqualTo(k_SubSceneName));
                }
            }
        }

        // Scenario:
        // - Add GameObject to root of subscene
        // - Add second GameObject to root of subscene
        // - Parent second GameObject to first
        // - Unparent second GameObject from first
        // - Delete second GameObject
        [UnityTest]
        public IEnumerator SubsceneBasedParenting_Scenario1()
        {
            // Adding a GameObject to a SubScene
            var go = new GameObject("go");
            SceneManager.MoveGameObjectToScene(go, m_SubScene.EditingScene);
            yield return UpdateLiveLink();

            // Adding a second GameObject to a SubScene
            var go2 = new GameObject("go2");
            SceneManager.MoveGameObjectToScene(go2, m_SubScene.EditingScene);
            yield return UpdateLiveLink();

            var (expectedHierarchy, subScene, _, entityId) = CreateBaseHierarchyForSubscene();
            subScene.AddChildren(
                new EntityHierarchyNodeId(NodeKind.Entity, entityId++),// "go" Converted Entity
                new EntityHierarchyNodeId(NodeKind.Entity, entityId)); // "go2" Converted Entity
            m_AssertHelper.AssertHierarchyByKind(expectedHierarchy.Build());

            // Parent second GameObject to first
            go2.transform.parent = go.transform;
            yield return UpdateLiveLink();
            yield return SkipAnEditorFrameAndUpdateWorld(); // Ensuring that all parenting phases have completed

            (expectedHierarchy, subScene, _, entityId) = CreateBaseHierarchyForSubscene();
            subScene.AddChild(new EntityHierarchyNodeId(NodeKind.Entity, entityId++))    // "go" Converted Entity
                        .AddChild(new EntityHierarchyNodeId(NodeKind.Entity, entityId)); // "go2" Converted Entity
            m_AssertHelper.AssertHierarchyByKind(expectedHierarchy.Build());

            // Unparent second GameObject from first
            go2.transform.parent = null;
            yield return UpdateLiveLink();
            yield return SkipAnEditorFrameAndUpdateWorld(); // Ensuring that all parenting phases have completed

            (expectedHierarchy, subScene, _, entityId) = CreateBaseHierarchyForSubscene();
            subScene.AddChildren(
                new EntityHierarchyNodeId(NodeKind.Entity, entityId++),// "go" Converted Entity
                new EntityHierarchyNodeId(NodeKind.Entity, entityId)); // "go2" Converted Entity
            m_AssertHelper.AssertHierarchyByKind(expectedHierarchy.Build());

            // Delete second GameObject
            Object.DestroyImmediate(go2);
            yield return UpdateLiveLink();

            (expectedHierarchy, subScene, _, entityId) = CreateBaseHierarchyForSubscene();
            subScene.AddChild(new EntityHierarchyNodeId(NodeKind.Entity, entityId)); // "go" Converted Entity

            m_AssertHelper.AssertHierarchyByKind(expectedHierarchy.Build());
        }

        // Creates the basic hierarchy for a single scene with a single subscene.
        static (TestHierarchy.TestNode root, TestHierarchy.TestNode subScene, int nextSceneId, int nextEntityId) CreateBaseHierarchyForSubscene()
        {
            var entityId = 0;
            var sceneId = 0;

            var rootNode = TestHierarchy.CreateRoot();

            rootNode.AddChildren(
                new EntityHierarchyNodeId(NodeKind.Entity, entityId++),                                  // World Time Entity
                new EntityHierarchyNodeId(NodeKind.Entity, entityId++),                                  // SubScene Entity
                new EntityHierarchyNodeId(NodeKind.Entity, entityId++));                                 // SceneSection Entity

            var subSceneNode =
                rootNode.AddChild(new EntityHierarchyNodeId(NodeKind.Scene, sceneId++))                  // Main Scene
                                        .AddChild(new EntityHierarchyNodeId(NodeKind.Scene, sceneId++)); // SubScene

            return (rootNode, subSceneNode, sceneId, entityId);
        }

        class MockHierarchy : IEntityHierarchy
        {
            public IEntityHierarchyGroupingStrategy Strategy { get; set; }
            public EntityQueryDesc QueryDesc { get; set; }
            public World World { get; set; }
            public void OnStructuralChangeDetected() { /* NOOP */ }
        }
    }
}
