using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class MoveData<T> where T : IEventData
{
    public bool moveInProgress = false;
    public int lastMouseTick;
    public int firstMouseTick;
    public int selectionOriginTick;
    public int lastTempGhostPasteStartTick;
    public int lastTempGhostPasteEndTick
    {
        get
        {
            return lastTempGhostPasteStartTick + MovingGhostSet.Keys.Max();
        }
    }
    public Move<T> currentMoveAction;
    public SortedDictionary<int, T> MovingGhostSet = new();
}