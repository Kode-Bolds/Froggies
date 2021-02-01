using Kodebolds.Core;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UIElements;

namespace Froggies
{
    [RequireComponent(typeof(UIDocument))]
    public class GameScreen : KodeboldBehaviour
    {
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


        public void AddSelectedUnits(int count)
        {
            unitGrid.Clear();

            for (int i = 0; i < count; i++)
            {
                VisualElement unit = new Button();
                unit.AddToClassList("unit");
                //unit.style.backgroundImage = 

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


            var rootVisualElement = GetComponent<UIDocument>().rootVisualElement;

            bottomPanel = GetComponent<UIDocument>().rootVisualElement.Q("BottomBar");
            topPanel = GetComponent<UIDocument>().rootVisualElement.Q("ResourceBar");


            // Minimap Test - RenderTexture
            minimap = rootVisualElement.Q<Image>("MapImage");
            minimap.image = miniMapRenderTexture;

            resource1Text = rootVisualElement.Q<Label>("Resource1Text");
            resource2Text = rootVisualElement.Q<Label>("Resource2Text");
            resource3Text = rootVisualElement.Q<Label>("Resource3Text");


            // Selected Units Test
            unitGrid = GetComponent<UIDocument>().rootVisualElement.Q("UnitGrid");

            // Buttons Test
            buttonGrid = GetComponent<UIDocument>().rootVisualElement.Q("ButtonGrid");
            for (int i = 0; i < 9; i++)
            {
                AddToButtons();
            }

            // Main Unit Test

            // hoveredUnit = TODO: implemented when an onHover is implemented on a UI "unit"
            mainUnit = GetComponent<UIDocument>().rootVisualElement.Q("MainUnit");



        }

        public override void UpdateBehaviour()
        {
            minimap.image = miniMapRenderTexture;

            var world = World.All;
            EntityManager entityManager = world[0].EntityManager;

            // RESOURCES
            EntityQuery entityQuery = entityManager.CreateEntityQuery(ComponentType.ReadWrite<Resources>());
            Resources resources = entityQuery.GetSingleton<Resources>();
            // entityQuery.SetSingleton<Resources>(res);
            // Resource Text
            resource1Text.text = resources.buildingMaterial.ToString();
            resource2Text.text = resources.food.ToString();
            resource3Text.text = resources.rareResource.ToString();


            // SELECTED ENTITIES
            EntityQuery selectedQuery = entityManager.CreateEntityQuery(ComponentType.ReadWrite<SelectedTag>());
            var entities = selectedQuery.ToEntityArray(Unity.Collections.Allocator.Persistent);
            if (entities.Length > 0)
            {
                AddSelectedUnits(entities.Length);
            }
            entities.Dispose();
        }

        public override void FreeBehaviour()
        {
        }
    }
}
