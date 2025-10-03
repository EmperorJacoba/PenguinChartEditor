public static class MiscTools
{
    public static string Capitalize(string name)
    {
        return char.ToUpper(name[0]) + name.Substring(1);
    }

    public static string Decapitalize(string name)
    {
        return char.ToLower(name[0]) + name.Substring(1);
    }
}
