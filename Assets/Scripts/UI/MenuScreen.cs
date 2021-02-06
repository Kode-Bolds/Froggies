using Unity.Entities;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class MenuScreen : KodeboldBehaviour
{
    private VisualElement bottomPanel;
    private VisualElement topPanel;

    private Label titleText;

    public VisualElement startButton;




    public override void GetBehaviourDependencies(Dependencies dependencies)
    {
    }

    public override void InitBehaviour()
    {
    }

    public override void UpdateBehaviour()
    {
    }

    public override void FreeBehaviour()
    {
    }

    //  START BUTTON ONCLICK
    public void StartGame(EventBase eventBase)
    {
        // TODO: SWITCH THE MAIN UNIT TO LINK WITH THE UNIT THAT WAS SELECTED
    }
}
