using System.Linq;
using UnityEngine;

// Attach this to the component it should spawn them under. E.g "Content" in a scroll view.
public class KeybinderManager : MonoBehaviour
{
    [SerializeField] private GameObject KeybindDisplayPrefab;

    /// <summary>
    /// Store all included action maps that the Keybind menu should not allow rebinding of.
    /// </summary>
    private static readonly string[] excludedActionMaps =
    {
        "StandardStaticEvents", // contains basic internal listeners for things like mouse clicks and drags
        "UI",                   // default action map - used to define unity button presses and stuff (volatile)
        "Player"                // default action map - 
    };
    
    private void Awake()
    {
        InputMap inputMap = new InputMap();

        var spawnableInputActions = inputMap.Where(x => !excludedActionMaps.Contains(x.actionMap.name));

        foreach (var ia in spawnableInputActions)
        {
            var item = Instantiate(KeybindDisplayPrefab, transform).GetComponent<KeybindEditor>();
            
            item.Initialize(ia);
        }
    }        
}