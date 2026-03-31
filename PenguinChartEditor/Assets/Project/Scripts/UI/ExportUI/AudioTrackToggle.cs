using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AudioTrackToggle : MonoBehaviour
{
    [HideInInspector] public Toggle toggle;
    [SerializeField] private TMP_Text textComponent;
    public StemType audioStemType;
    
    private string FormattedStem => MiscTools.Capitalize(audioStemType.ToString().Replace("_", " "));
    
    private void Awake()
    {
        toggle = GetComponent<Toggle>();
        toggle.isOn = true;
    }

    public void Initialize(StemType asStem)
    {
        audioStemType = asStem;
        textComponent.text = FormattedStem;
    }
}