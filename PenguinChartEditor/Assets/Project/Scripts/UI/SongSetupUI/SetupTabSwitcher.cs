using UnityEngine;

public class SetupTabSwitcher : BasicPenguinTab<SetupTabSwitcher>
{
    public GameObject controlledComponent;

    protected override void OnSwitchOff()
    {
        controlledComponent.SetActive(false);
    }

    protected override void OnSwitchOn()
    {
        controlledComponent.SetActive(true);
    }
}