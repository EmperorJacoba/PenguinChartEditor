using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

// Attach this to the component it should spawn them under. E.g "Content" in a scroll view.
public class KeybinderManager : MonoBehaviour
{
    [SerializeField] private GameObject KeybindDisplayPrefab;
    [SerializeField] private GameObject actionHeadingTextPrefab;

    /// <summary>
    /// Store all included action maps that the Keybind menu should not allow rebinding of.
    /// </summary>
    private static readonly string[] excludedActionMaps =
    {
        "StandardStaticEvents", // contains basic internal listeners for things like mouse clicks and drags
        "UI",                   // default action map - used to define unity button presses and stuff (volatile)
        "Player"                // default action map
    };
    
    private void Awake()
    {
        var spawnableInputActions = Chart.instance.inputMap.Where(x => !excludedActionMaps.Contains(x.actionMap.name));

        InputActionMap activeMap = null;
        foreach (var ia in spawnableInputActions)
        {
            if (ia.actionMap != activeMap)
            {
                activeMap = ia.actionMap;
                var tmpText = Instantiate(actionHeadingTextPrefab, transform).GetComponent<TMP_Text>();
                tmpText.text = MiscTools.UnpackCamelCase(activeMap.name);
            }
            var item = Instantiate(KeybindDisplayPrefab, transform).GetComponent<KeybindEditor>();
            
            item.Initialize(ia);
        }
    }        
}