using System.IO;
using UnityEngine;

public class ChartWriter : MonoBehaviour
{
    public static void WriteDotChartFile()
    {

    }

    public static void WriteDotIniFile()
    {
        StreamWriter iniWriter = new(ChartMetadata.IniPath);
        iniWriter.WriteLine("[Song]");
    }
}