using System;
using System.Collections.Generic;
using UnityEngine;

public class SelectionManager : MonoBehaviour
{
    static InputMap inputMap;
    public static SortedDictionary<int, HashSet<IEventData>> clipboard = new();
    public static SortedDictionary<int, HashSet<IEventData>> selection = new();

    void Awake()
    {
        inputMap = new();
        inputMap.Enable();
        inputMap.Charting.Copy.performed += x => CopySelection();
    }

    public void CopySelection()
    {
        var copyAction = new Copy();
        copyAction.Execute();
    }
}