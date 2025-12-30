using UnityEngine;

public interface IReadOnlyEvent
{
    int Tick { get; }
    bool Visible { get; set; }

    ILaneData GetLaneData();
    IInstrument ParentInstrument { get; }
}
public abstract class ReadOnlyEvent<T> : MonoBehaviour where T : IEventData
{
    public int Tick
    {
        get
        {
            return _tick;
        }
    }
    protected int _tick;

    public bool Visible
    {
        get
        {
            return gameObject.activeInHierarchy;
        }
        set
        {
            if (Visible != value) gameObject.SetActive(value);
        }
    }

    public abstract LaneSet<T> LaneData { get; }
    public ILaneData GetLaneData() => LaneData;

    public T RepresentedData => _representedData;
    private T _representedData;
}