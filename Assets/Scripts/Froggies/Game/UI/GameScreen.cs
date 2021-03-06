using Kodebolds.Core;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UIElements;

namespace Froggies
{
    [RequireComponent(typeof(UIDocument))]
    public class GameScreen : KodeboldBehaviour
    {
        private VisualElement rootVisualElement;

        private VisualElement bottomPanel;
        private VisualElement topPanel;

        private Label resource1Text;
        private Label resource2Text;
        private Label resource3Text;

        public VisualElement unitGrid;
        public VisualElement buttonGrid;
        public VisualElement mainUnit;

        public Image minimap;
        public RenderTexture miniMapRenderTexture;

        World.NoAllocReadOnlyCollection<World> world;
        EntityManager entityManager;
        EntityQuery selectedQuery;
        SelectionSystem selectionSystem;

        Unity.Collections.NativeArray<Entity> entities;

        protected override GameState ActiveGameState => GameState.Updating;

		public void AddSelectedUnits(int count)
        {
            unitGrid.Clear();

            for (int i = 0; i < count; i++)
            {
                Button unit = new Button();
                unit.clickable.clickedWithEventInfo += ChangeMainSelectedUnit;
                unit.AddToClassList("unit");
                unitGrid.Add(unit);
            }
        }

        public void AddToButtons()
        {
            Button button = new Button();
            button.AddToClassList("button");
            button.text = "Tony";
            // button.AddToClassList("unity-text-element");
            // button.AddToClassList("unity-button");
            // button.SetEnabled(false);

            buttonGrid.Add(button);
        }

        public void HideBottomPanel()
        {
            bottomPanel.style.display = DisplayStyle.None;
        }

        public void HideTopPanel()
        {
            topPanel.style.display = DisplayStyle.None;
        }

        public override void GetBehaviourDependencies(Dependencies dependencies)
        {
        }

        public override void InitBehaviour()
        {
            // Get the world and entity manager
            world = World.All;
            entityManager = world[0].EntityManager;

            // Root element of UI
            rootVisualElement = GetComponent<UIDocument>().rootVisualElement;

            bottomPanel = GetComponent<UIDocument>().rootVisualElement.Q("BottomBar");
            topPanel = GetComponent<UIDocument>().rootVisualElement.Q("ResourceBar");

            // Minimap
            minimap = rootVisualElement.Q<Image>("MapImage");
            minimap.image = miniMapRenderTexture;

            // Resources
            resource1Text = rootVisualElement.Q<Label>("Resource1Text");
            resource2Text = rootVisualElement.Q<Label>("Resource2Text");
            resource3Text = rootVisualElement.Q<Label>("Resource3Text");

            // Selected Units
            unitGrid = GetComponent<UIDocument>().rootVisualElement.Q("UnitGrid");
            selectedQuery = entityManager.CreateEntityQuery(ComponentType.ReadWrite<SelectedTag>());
            selectionSystem = world[0].GetExistingSystem<SelectionSystem>();

            // Action Buttons
            buttonGrid = GetComponent<UIDocument>().rootVisualElement.Q("ButtonGrid");
            for (int i = 0; i < 3; i++)
            {
                AddToButtons();
            }

            // hoveredUnit = TODO: implemented when an onHover is implemented on a UI "unit"
            mainUnit = GetComponent<UIDocument>().rootVisualElement.Q("MainUnit");



        }

        public override void UpdateBehaviour()
        {
            // Re-assign the new texture to the minimap
            minimap.image = miniMapRenderTexture;



            // // ---- RESOURCES ---- \\
            // EntityQuery entityQuery = entityManager.CreateEntityQuery(ComponentType.ReadWrite<Resources>());
            // Resources resources = entityQuery.GetSingleton<Resources>();
            // // entityQuery.SetSingleton<Resources>(res);
            // // Resource Text
            // resource1Text.text = resources.buildingMaterial.ToString();
            // resource2Text.text = resources.food.ToString();
            // resource3Text.text = resources.rareResource.ToString();


            // ---- SELECTED PLAYERS ---- \\
            if (selectionSystem.redrawSelectedUnits)
            {
                selectionSystem.redrawSelectedUnits = false;

                entities = selectedQuery.ToEntityArray(Unity.Collections.Allocator.Persistent);
                if (entities.Length > 0)
                {
                    AddSelectedUnits(entities.Length);
                }
                entities.Dispose();
            }
        }

        public override void FreeBehaviour()
        {
        }



        // UI BUTTON ON CLICK EVENTS \\
        public void ChangeMainSelectedUnit(EventBase eventBase)
        {
            //  mainUnit.visible = false;

            // TODO: SWITCH THE MAIN UNIT TO LINK WITH THE UNIT THAT WAS SELECTED
        }


        // UI BUTTON ON HOVER EVENTS \\
    }
}
