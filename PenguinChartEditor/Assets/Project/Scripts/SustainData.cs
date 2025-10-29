using System.Collections.Generic;

public class SustainData<T>
{
    public bool sustainInProgress = false;
    public int lastMouseTick;
    public int firstMouseTick;
    public Sustain<FiveFretNoteData> sustainEventAction;
    public SortedDictionary<int, FiveFretNoteData> sustainingTicks = new();
    public bool sustainsReset = false;
}