using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class KeybindEditor : MonoBehaviour
{
    public const string ONE_MODIFIER = "OneModifier";
    public const string TWO_MODIFIERS = "TwoModifiers";
    private const int KEYBIND_REBIND_FRAME_BUFFER = 10;
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
        Chart.instance.inputMap.Disable();

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
            OnComplete(CompleteRebindingOperation).
            OnCancel(CompleteRebindingOperation).
            Start();

        buttonText.text = "...";
    }
        
    private void UpdateKeybindButtonDisplayText()
    {
        actionIndeces = DetectBindings(assignedAction);
        
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
                if (!capturedComposites.Contains(LEFT_COMMAND_PATH)) capturedComposites.Add(LEFT_COMMAND_PATH);
            }

            if (Input.GetKeyDown(KeyCode.RightCommand))
            {
                if (!capturedComposites.Contains(RIGHT_COMMAND_PATH)) capturedComposites.Add(RIGHT_COMMAND_PATH);
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
                    With("Modifier1", capturedComposites[0]).
                    With("Modifier2", capturedComposites[1]).
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
        
        actionIndeces = DetectBindings(assignedAction);
        UpdateKeybindButtonDisplayText();
    }

    private void CompleteRebindingOperation(InputActionRebindingExtensions.RebindingOperation operation)
    {
        operation.Dispose();
        captureCompositeActions = false;
        capturedComposites.Clear();
        
        StartCoroutine(EnableAfterKeysReleased());
    }
    
    private static IEnumerator EnableAfterKeysReleased()
    {
        // Wait an arbitrary number of frames (10) to reenable inputs. After a rebind operation the input will trigger
        // because Unity didn't think to stop that for some reason. Womp
        for (int i = 0; i < KEYBIND_REBIND_FRAME_BUFFER; i++) yield return null;

        Chart.instance.inputMap.Enable();
    }
    
    public void Initialize(InputAction assignedAction)
    {
        this.assignedAction = assignedAction;        
        
        label.text = MiscTools.UnpackCamelCase(assignedAction.name);
        
        UpdateKeybindButtonDisplayText();
        
        gameObject.SetActive(true);
    }

    private List<int> DetectBindings(InputAction action)
    {
        var foundActionIndeces = new List<int>();
        for (int i = 0; i < action.bindings.Count; i++)
        {
            var identifier = action.bindings[i];
            foundActionIndeces.Add(i);

            switch (identifier.path)
            {
                case ONE_MODIFIER:
                    i += 2; // modifier + control
                    break;
                case TWO_MODIFIERS:
                    i += 3; // modifier + modifier + control
                    break;
            }
        }

        return foundActionIndeces;
    }
}