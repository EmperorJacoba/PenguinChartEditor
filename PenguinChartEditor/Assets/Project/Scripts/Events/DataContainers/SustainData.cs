using System.Collections.Generic;
using System.Linq;

public class SustainData<T> where T : IEventData
{
    public bool sustainInProgress;
    public int lastMouseTick;
    public int firstMouseTick;
    public Sustain<T> sustainEventAction;
    public SortedDictionary<int, T> sustainingTicks = new();

    public SustainData(LaneSet<T> eventSet, SelectionSet<T> selection, int mouseTick)
    {
        sustainEventAction = new(eventSet);
        sustainInProgress = true;
        lastMouseTick = mouseTick;
        firstMouseTick = mouseTick;

        sustainingTicks.Clear();
        sustainingTicks = new(selection);

        selection.Clear();
        sustainEventAction.CaptureOriginalSustain(sustainingTicks.Keys.ToList());
    }

    // use only in Lane<T> class/end of user sustain --
    // this is supposed to signal for the sustain function
    // that this object must be properly initialized with the new loop's variables
    public SustainData()
    {
        sustainInProgress = false;
    }
}