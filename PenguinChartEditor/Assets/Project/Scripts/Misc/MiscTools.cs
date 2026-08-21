using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine.EventSystems;
using UnityEngine;

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

    // Adapted from https://stackoverflow.com/a/9283563/31816967
    // Makes PascalCase/camelCase input names space separated
    public static string UnpackCamelCase(string camelCasedString)
    {
        return Regex.Replace(
            camelCasedString, 
            @"(?<=[a-z])([A-Z])|(?<!\A)([A-Z])(?=[a-z])", " $1$2"
            );
    }

    public static string CleanFileName(string name)
    {
        return name.
            Replace("/", "").
            Replace("\\", "").
            Replace("\"", "").
            Replace("<", "").
            Replace(">", "").
            Replace(":", "").
            Replace("|", "").
            Replace("?", "").
            Replace("*", "");
    }
}
