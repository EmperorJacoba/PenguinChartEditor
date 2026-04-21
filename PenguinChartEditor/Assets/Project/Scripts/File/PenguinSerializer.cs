using System.Collections.Generic;
using System.Linq;

public static class PenguinSerializer
{
    public static List<string> SerializeBasicDictionary<TKey, TValue>(
        string identifier,
        Dictionary<TKey, TValue> dictionary, 
        int level
        )
    {
        var mainIndent = string.Concat(Enumerable.Repeat("\t", level + 1));
        var curlyIndent = string.Concat(Enumerable.Repeat("\t", level));

        List<string> output = new List<string>();
        
        output.Add($"{curlyIndent}[{identifier}]");
        output.Add($"{curlyIndent}" + "{");

        output.AddRange(dictionary.Select(element => $"{mainIndent}{element.Key} = {element.Value}"));
        
        output.Add($"{curlyIndent}" + "}");

        return output;
    }
    
}