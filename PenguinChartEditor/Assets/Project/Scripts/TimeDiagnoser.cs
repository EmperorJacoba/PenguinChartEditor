#if UNITY_EDITOR

using System.Text;
using UnityEngine;

namespace Penguin.Debug
{
    public class TimeDiagnoser
    {
        string diagnosticName;
        StringBuilder timeString;
        float lastRecord;
        float firstRecord;

        public TimeDiagnoser(string name)
        {
            diagnosticName = name;
            lastRecord = Time.realtimeSinceStartup * 1000;
            firstRecord = lastRecord;
            timeString = new();
        }

        public string Report()
        {
            return $"{diagnosticName}:\n" + timeString.ToString() + $"\nTotal time: {(Time.realtimeSinceStartup * 1000) - firstRecord}";
        }

        public void RecordTime(string id)
        {
            timeString.AppendLine($"{id}: {(Time.realtimeSinceStartup * 1000) - lastRecord}ms");
            lastRecord = Time.realtimeSinceStartup * 1000;
        }
    }
}

#endif