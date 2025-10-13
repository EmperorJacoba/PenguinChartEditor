using System.ComponentModel;
using UnityEngine;
using UnityEngine.InputSystem;

[DisplayName("Disable Keyboard Modifiers")]
[Description("Prevent this action from firing \"performed\" when any of these modifier keys are pressed. Useful for controls that have a regular and then a c/a/s variant.")]
public class DisableModifiers : IInputInteraction
{
    public bool disableOnShift = true;
    public bool disableOnAlt = true;
    public bool disableOnControl = true;
    public void Process(ref InputInteractionContext context)
    {
        if (Keyboard.current.altKey.isPressed && disableOnAlt) return;
        if (Keyboard.current.shiftKey.isPressed && disableOnShift) return;
        if (Keyboard.current.ctrlKey.isPressed && disableOnControl) return;

        if (context.ControlIsActuated()) context.Performed();
    }

    public void Reset() { }
}
