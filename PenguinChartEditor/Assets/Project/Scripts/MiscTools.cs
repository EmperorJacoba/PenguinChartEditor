public static class MiscTools
{
    public static string Capitalize(string name)
    {
        return char.ToUpper(name[0]) + name.Substring(1); 
    }

    public static int IncreaseByHalfDivision(int tick)
    {
        return (int)(ChartMetadata.ChartResolution / SongTimelineManager.CalculateDivision(tick) / 2);
    }
}
