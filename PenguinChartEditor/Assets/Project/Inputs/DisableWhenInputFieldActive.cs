using System.ComponentModel;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

[DisplayName("Disable On InputField")]
[Description("Prevent this action from firing when the current event system registers that an input field is selected.")]
public class DisableWhenInputActive : IInputInteraction
{
    public void Process(ref InputInteractionContext context)
    {
        if (EventSystem.current.currentSelectedGameObject != null)
        {
            if (EventSystem.current.currentSelectedGameObject.name.Contains("Custom Input")) return;
        }

        if (context.ControlIsActuated()) context.Performed();
    }

    public void Reset() { }
}
