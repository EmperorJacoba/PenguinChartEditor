using System.Collections.Generic;
using System.Linq;

public class SustainData<T> where T : IEventData
{
    public bool sustainInProgress = false;
    public int lastMouseTick;
    public int firstMouseTick;
    public Dictionary<int, HashSet<int>> sustainingTicks;

    public SustainData(Dictionary<int, HashSet<int>> selection, int mouseTick)
    {
        lastMouseTick = mouseTick;
        firstMouseTick = mouseTick;

        sustainingTicks = selection;

        sustainInProgress = true;
    }

    // use only in Lane<T> class/end of user sustain --
    // this is supposed to signal for the sustain function
    // that this object must be properly initialized with the new loop's variables
    public SustainData()
    {
        sustainInProgress = false;
    }
}