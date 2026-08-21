using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/*
 * Two issues with the Unity Input System have led to common and extremely annoying events in Penguin.
 * 1. If you have a binding that is "ctrl + f" and a binding that is "f", and you press "ctrl + f", the Unity Input
 * System makes zero effort to help you and instead leaves you to figure out that both bindings will fire on that press.
 * 2. If you are focused on an input field, all of your bindings will still fire normally.
 * 3. If you have multiple "filter" type bindings on a script (e.g. interactions that solve 1. and 2., those will double
 * fire because of the way interactions are designed (you usually have to manually enact the event at the end of the
 * interaction in these cases). They are more meant to "modify" the output of the command, less "filter." However, the
 * alternative (of which I can see) to a filter-type interaction is to MANUALLY CHECK EVERY TIME YOU FIRE AN ACTION
 * that these two EXTREMELY COMMON scenarios are not currently in play. Which is extremely fucking dumb and goes against
 * fundamental programming practices. 
 * 
 * 1. and 2. are not necessarily bad choices, as they aren't really Unity's responsibility. It is absolutely more me
 * wanting them to solve my problems for me. I'm still pissed off about it though. This is a terrible, terrible system.
 */

public class PreventCommonInputConflicts : IInputInteraction
{
    // Since bindings are rebindable, we must divert some computing power to check for issues that come with the
    // unity input system. These also have to be on the same script because splitting these up double-runs the binding
    public void Process(ref InputInteractionContext context)
    {
        if (!context.ControlIsActuated()) return;
        var bindings = context.action.bindings;
        var paths = bindings.Select(x => x.path).ToList();

        // Disable on modifiers
        // Issue: <modifier> + <bind> and <bind> both trigger on <modifier> + <bind>. Let's not do that? 
        // (genuinely why is this not already a built in feature???? unity wtf????)
        
        Dictionary<string, bool> modifierBindings = new()
        {
            ["<Keyboard>/alt"] = Keyboard.current.altKey.isPressed,
            ["<Keyboard>/shift"] = Keyboard.current.shiftKey.isPressed,
            ["<Keyboard>/ctrl"] = Keyboard.current.ctrlKey.isPressed
        };
        
        foreach (var modifierBinding in modifierBindings)
        {
            if (modifierBinding.Value && !paths.Contains(modifierBinding.Key)) return;
        }
        
        // Disable on Input Field
        // Bindings will still trigger on input fields when typing. Womp. Let's also not do that???
        
        var activeObj = EventSystem.current.currentSelectedGameObject;
        if (activeObj != null && activeObj.GetComponent<TMP_InputField>() != null) return;
        
        context.Performed();
    }

    public void Reset() {}
}
