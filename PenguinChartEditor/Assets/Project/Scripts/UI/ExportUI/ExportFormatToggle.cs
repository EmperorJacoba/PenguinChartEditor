using UnityEngine;
using UnityEngine.Serialization;

public class ExportFormatToggle : MonoBehaviour
{
    [FormerlySerializedAs("format")] [SerializeField] public ChartFormat format;
}