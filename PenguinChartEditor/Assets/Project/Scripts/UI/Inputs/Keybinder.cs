using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class KeybindEditor : MonoBehaviour
{
    private const string ONE_MODIFIER = "OneModifier";
    private const string TWO_MODIFIERS = "TwoModifiers";
    [SerializeField] private TMP_Text label;
    [SerializeField] private Button primaryKeybindLabel;
    [SerializeField] private TMP_Text primaryKeybindLabelText;
    [SerializeField] private Button secondaryKeybindLabel;
    [SerializeField] private TMP_Text secondaryKeybindLabelText;

    private InputMap inputMap;
    private InputAction assignedAction;
    private List<int> actionIndeces;

    private void Awake()
    {
        primaryKeybindLabel.onClick.AddListener(RebindPrimary);
        secondaryKeybindLabel.onClick.AddListener(RebindSecondary);
    }

    private void RebindPrimary() => Rebind(0, primaryKeybindLabelText);
    private void RebindSecondary() => Rebind(1, secondaryKeybindLabelText);
    private void Rebind(int index, TMP_Text buttonText)
    {
        assignedAction.Disable();

        captureCompositeActions = true;

        var rebindingOperation = new InputActionRebindingExtensions.RebindingOperation();
        rebindingOperation.
            WithExpectedControlType("Button").
            WithCancelingThrough("<Keyboard>/escape").
            WithControlsExcluding("Mouse").
            // composites handled separately to preserve ordering (ctrl + alt + <button> vs alt + ctrl + <button)
            WithControlsExcluding("<Keyboard>/ctrl").
            WithControlsExcluding("<Keyboard>/leftCtrl").
            WithControlsExcluding("<Keyboard>/rightCtrl").
            WithControlsExcluding("<Keyboard>/shift").
            WithControlsExcluding("<Keyboard>/leftShift").
            WithControlsExcluding("<Keyboard>/rightShift").
            WithControlsExcluding("<Keyboard>/alt").
            WithControlsExcluding("<Keyboard>/leftAlt").
            WithControlsExcluding("<Keyboard>/rightAlt").
            WithControlsExcluding("<Keyboard>/anyKey"). // If any control is ignored (like the ones above), it still fires this. 
            WithControlsHavingToMatchPath("<Keyboard>").
            OnApplyBinding((x, y) => ProcessRebindOperation(x, y, index)).
            Start();

        buttonText.text = "...";
    }
        // todo: make manager class that handles spawning, saving, etc. of keybinds
        
    private void UpdateKeybindButtonDisplayText()
    {
        actionIndeces = DetectBindings();

        primaryKeybindLabelText.text = ConvertBindingToDisplayString(0);
        secondaryKeybindLabelText.text = ConvertBindingToDisplayString(1);
    }
    
    private string ConvertBindingToDisplayString(int actionIndecesIndex)
    {
        if (actionIndecesIndex >= actionIndeces.Count) return "--";
        var bindingIndex = actionIndeces[actionIndecesIndex];
        var binding = assignedAction.bindings[bindingIndex];

        switch (binding.path)
        {
            case TWO_MODIFIERS:
                var m1TwoMod = MiscTools.Capitalize(assignedAction.bindings[bindingIndex + 1].ToDisplayString());
                var m2 = MiscTools.Capitalize(assignedAction.bindings[bindingIndex + 2].ToDisplayString());
                var actionTwoMod = MiscTools.Capitalize(assignedAction.bindings[bindingIndex + 3].ToDisplayString());
                return
                    $"{m1TwoMod} + {m2} + {actionTwoMod}"; 
            case ONE_MODIFIER:
                var m1 = MiscTools.Capitalize(assignedAction.bindings[bindingIndex + 1].ToDisplayString());
                var action = MiscTools.Capitalize(assignedAction.bindings[bindingIndex + 2].ToDisplayString());
                return
                    $"{m1} + {action}"; 
            default:
                return binding.ToDisplayString();
        }
    }

    private bool captureCompositeActions = false;
    private List<string> capturedComposites = new List<string>();

    private const string CONTROL_PATH = "<Keyboard>/ctrl";
    private const string ALT_PATH = "<Keyboard>/alt";
    private const string SHIFT_PATH = "<Keyboard>/shift";
    private const string LEFT_COMMAND_PATH = "<Keyboard>/leftMeta";
    private const string RIGHT_COMMAND_PATH = "<Keyboard>/rightMeta";
    
    private void Update()
    {
        if (captureCompositeActions)
        {
            if (Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.RightControl))
            {
                if (!capturedComposites.Contains(CONTROL_PATH)) capturedComposites.Add(CONTROL_PATH);
            }

            if (Input.GetKeyDown(KeyCode.LeftAlt) || Input.GetKeyDown(KeyCode.RightAlt))
            {
                if (!capturedComposites.Contains(ALT_PATH)) capturedComposites.Add(ALT_PATH);
            }

            if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift))
            {
                if (!capturedComposites.Contains(SHIFT_PATH)) capturedComposites.Add(SHIFT_PATH);
            }
#if UNITY_STANDALONE_OSX
            if (Input.GetKeyDown(KeyCode.LeftCommand))
            {
                if (!capturedComposites.Contains(LEFT_COMMAND_PATH)) capturedComposites.Add(LEFT_COMMAND_PATH)
            }

            if (Input.GetKeyDown(KeyCode.RightCommand))
            {
                if (!capturedComposites.Contains(RIGHT_COMMAND_PATH)) capturedComposites.Add(RIGHT_COMMAND_PATH)
            }
#endif
        }
    }

    private void ProcessRebindOperation(
        InputActionRebindingExtensions.RebindingOperation operation, 
        string path,
        int actionIndex
        )
    {
        print(path);
        if (actionIndex < actionIndeces.Count)
        {
            // remove existing action to prep for new action
            assignedAction.ChangeBinding(actionIndeces[actionIndex]).Erase();
        }
        
        switch (capturedComposites.Count)
        {
            // Order matters here. With("Modifier").With("Binding") will appear differently from vice versa. 
            case >= 2:
                assignedAction.AddCompositeBinding("TwoModifiers").
                    With("Modifier", capturedComposites[0]).
                    With("Modifier", capturedComposites[1]).
                    With("Binding", path);
                break;
            case >= 1:
                assignedAction.AddCompositeBinding("OneModifier").
                    With("Modifier", capturedComposites[0]).
                    With("Binding", path);
                break;
            default:
                assignedAction.AddBinding(path);
                break;
        }
        
        operation.Dispose();
        assignedAction.Enable();
        capturedComposites.Clear();
        actionIndeces = DetectBindings();
        UpdateKeybindButtonDisplayText();

        print("[Bindings]");
        foreach (var binding in assignedAction.bindings)
        {
            print(binding.effectivePath);
            print($"dsp: {binding.ToDisplayString()}");
        }

        foreach (var idx in actionIndeces)
        {
            print(idx);
        }
    }
    
    public void Initialize(InputAction assignedAction)
    {
        this.assignedAction = assignedAction;
        label.text = assignedAction.name;
        
        UpdateKeybindButtonDisplayText();
        
        gameObject.SetActive(true);
    }

    private List<int> DetectBindings()
    {
        var foundActionIndeces = new List<int>();
        for (int i = 0; i < assignedAction.bindings.Count; i++)
        {
            var identifier = assignedAction.bindings[i];
            foundActionIndeces.Add(i);

            if (identifier.path == ONE_MODIFIER)
            {
                i += 2; // modifier + control
            }

            if (identifier.path == TWO_MODIFIERS)
            {
                i += 3; // modifier + modifier + control
            }
        }

        return foundActionIndeces;
    }
}