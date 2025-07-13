using System.Collections.Generic;
using UnityEngine;

public class SelectionManager : MonoBehaviour
{
    public static SortedDictionary<int, HashSet<IEventData>> clipboard = new();
    public static SortedDictionary<int, HashSet<IEventData>> selection = new();

}