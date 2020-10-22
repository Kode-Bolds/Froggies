using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class GameScreen : MonoBehaviour
{
    private VisualElement bottomPanel;
    private VisualElement topPanel;

    private Label resource1Text;
    private Label resource2Text;
    private Label resource3Text;

    public int resource1Value = 100;
    public int resource2Value = 100;
    public int resource3Value = 100;


    public VisualElement unitGrid;
    public VisualElement buttonGrid;
    public VisualElement mainUnit;


    public Image minimap;
    public RenderTexture miniMapRenderTexture;



    void OnEnable()
    {
        var rootVisualElement = GetComponent<UIDocument>().rootVisualElement;

        bottomPanel = GetComponent<UIDocument>().rootVisualElement.Q("BottomBar");
        topPanel = GetComponent<UIDocument>().rootVisualElement.Q("ResourceBar");

        // Resource Text
        resource1Text = rootVisualElement.Q<Label>("Resource1Text");
        resource1Text.text = resource1Value.ToString();
        resource2Text = rootVisualElement.Q<Label>("Resource2Text");
        resource2Text.text = resource2Value.ToString();
        resource3Text = rootVisualElement.Q<Label>("Resource3Text");
        resource3Text.text = resource3Value.ToString();

        // Selected Units Test
        unitGrid = GetComponent<UIDocument>().rootVisualElement.Q("UnitGrid");
        for (int i = 0; i < 9; i++)
        {
            AddToSelectedUnits();
        }

        // Buttons Test
        buttonGrid = GetComponent<UIDocument>().rootVisualElement.Q("ButtonGrid");
        for (int i = 0; i < 9; i++)
        {
            AddToButtons();
        }

        // Main Unit Test

        // hoveredUnit = TODO: implemented when an onHover is implemented on a UI "unit"
        mainUnit = GetComponent<UIDocument>().rootVisualElement.Q("MainUnit");



        // Minimap Test - RenderTexture
        minimap = rootVisualElement.Q<Image>("MapImage");
        minimap.image = miniMapRenderTexture;

    }

    private void Update()
    {
        minimap.image = miniMapRenderTexture;
    }


    public void AddToSelectedUnits()
    {
        VisualElement unit = new VisualElement();
        unit.AddToClassList("unit");

        //unit.style.backgroundImage = 

        unitGrid.Add(unit);
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
}
