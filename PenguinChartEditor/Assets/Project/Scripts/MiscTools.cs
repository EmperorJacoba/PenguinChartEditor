public static class MiscTools
{
    public static string Capitalize(string name)
    {
        return char.ToUpper(name[0]) + name.Substring(1); 
    }
}
