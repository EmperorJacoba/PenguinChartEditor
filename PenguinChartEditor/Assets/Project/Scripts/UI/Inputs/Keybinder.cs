using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class KeybindEditor : MonoBehaviour
{
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
        inputMap = new InputMap();
        inputMap.Enable();
        Initialize(inputMap.StandardCommands.Copy);

        primaryKeybindLabel.onClick.AddListener(RebindPrimary);
        secondaryKeybindLabel.onClick.AddListener(RebindSecondary);
    }

    private void RebindPrimary() => Rebind(0);
    private void RebindSecondary() => Rebind(1);
    private void Rebind(int index)
    {
        assignedAction.Disable();

        captureCompositeActions = true;

        var rebindingOperation = new InputActionRebindingExtensions.RebindingOperation();
        rebindingOperation.
            WithExpectedControlType("Button").
            WithCancelingThrough("<Keyboard>/escape").
            WithControlsExcluding("Mouse").
            WithControlsExcluding("<Keyboard>/ctrl").
            WithControlsExcluding("<Keyboard>/alt").
            WithControlsExcluding("<Keyboard>/shift").
            WithControlsHavingToMatchPath("<Keyboard>").
            OnComplete(x => ProcessRebindOperation(x, index)).
            Start();
        
        
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
                if (!capturedComposites.Contains(SHIFT_PATH)) capturedComposites.Add(SHIFT_PATH);
            }

            if (Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift))
            {
                if (!capturedComposites.Contains(ALT_PATH)) capturedComposites.Add(ALT_PATH);
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

    private void ProcessRebindOperation(InputActionRebindingExtensions.RebindingOperation operation, int actionIndex)
    {
        if (actionIndex >= actionIndeces.Count) actionIndex = actionIndeces.Count - 1;

        if (actionIndex >= 0)
        {
            // remove existing action to prep for new action
            assignedAction.ChangeBinding(actionIndeces[actionIndex]).Erase();
        }
        
        switch (capturedComposites.Count)
        {
            case >= 2:
                assignedAction.AddCompositeBinding("ButtonWithTwoModifiers").
                    With("Button", operation.selectedControl.path).
                    With("Modifier1", capturedComposites[0]).
                    With("Modifier2", capturedComposites[1]);
                break;
            case >= 1:
                assignedAction.AddCompositeBinding("ButtonWithOneModifier").
                    With("Button", operation.selectedControl.path).
                    With("Modifier", capturedComposites[0]);
                break;
            default:
                assignedAction.AddBinding(operation.selectedControl);
                break;
        }
        
        operation.Dispose();
        assignedAction.Enable();
        capturedComposites.Clear();
    }
    
    public void Initialize(InputAction assignedInput)
    {
        label.text = assignedInput.name;
        var bindings = assignedInput.bindings;

        foreach (var binding in bindings)
        {
            print(binding);
        }
        
        assignedInput.ChangeBinding(0).Erase();

        foreach (var binding in assignedInput.bindings)
        {
            print(binding);
        }
        

        //primaryKeybindLabelText.text = bindings.Count > 0 ? bindings[0].ToDisplayString() : "--";
        //secondaryKeybindLabelText.text = bindings.Count > 1 ? bindings[1].ToDisplayString() : "--";
    }

    private List<int> DetectBindings(InputAction action)
    {
        var actionIndeces = new List<int>();
        for (int i = 0; i < action.bindings.Count; i++)
        {
            var identifier = action.bindings[i];

            if (identifier.path == "OneModifier")
            {
                i += 2; // modifier + control
            }

            if (identifier.path == "TwoModifiers")
            {
                i += 3; // modifier + modifier + control
            }
            
            actionIndeces.Add(i);
        }

        return actionIndeces;
    }
}