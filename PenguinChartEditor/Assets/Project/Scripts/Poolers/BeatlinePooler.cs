
public class BeatlinePooler : Pooler<Beatline>
{
    /// <summary>
    /// Static reference to the pooler object.
    /// </summary>
    public static BeatlinePooler instance;

    void Awake()
    {
        instance = this;
    }
}
