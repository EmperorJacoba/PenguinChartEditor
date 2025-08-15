using UnityEngine;
using System.Collections.Generic;

public class MoveData<T> where T : IEventData
{
    public bool moveInProgress = false;
    public int lastMouseTick;
    public int firstMouseTick;
    public int selectionOriginTick;
    public int lastTempGhostPasteStartTick;
    public int lastTempGhostPasteEndTick;
    public Move<T> currentMoveAction;
    public Delete<T> lastDeleteAction;
    public SortedDictionary<int, T> MovingGhostSet = new();
}