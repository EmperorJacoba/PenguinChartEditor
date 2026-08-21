using UnityEngine;
using UnityEngine.Serialization;

public class AudioFormatToggle : MonoBehaviour
{
    [FormerlySerializedAs("format")] [SerializeField] public AudioFormat format;
}