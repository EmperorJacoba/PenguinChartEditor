#if UNITY_EDITOR

using System.Text;
using UnityEngine;

namespace Penguin.Debug
{
    public class TimeDiagnoser
    {
        private string diagnosticName;
        private StringBuilder timeString;
        private float lastRecord;
        private float firstRecord;

        public TimeDiagnoser(string name)
        {
            diagnosticName = name;
            lastRecord = Time.realtimeSinceStartup;
            firstRecord = lastRecord;
            timeString = new();
        }

        public void Report()
        {
            MonoBehaviour.print($"{diagnosticName}:\n" + timeString.ToString() + $"\nTotal time: {(Time.realtimeSinceStartup - firstRecord) * 1000}ms");
        }

        public void RecordTime(string id)
        {
            timeString.AppendLine($"{id}: {(Time.realtimeSinceStartup - lastRecord) * 1000}ms");
            lastRecord = Time.realtimeSinceStartup;
        }
    }
}

#endif