using UnityEngine.UI;
using UnityEngine;
using TMPro;

public class UIManager : MonoBehaviour
{
    [SerializeField] Button PlayButton;
    [SerializeField] Button PauseButton;
    [SerializeField] Button RWButton;
    [SerializeField] Button FFWButton;
    [SerializeField] Button StopButton;

    [SerializeField] TextMeshProUGUI SongTimestampLabel;
    [SerializeField] TextMeshProUGUI SongLengthLabel;

    [SerializeField] Slider HyperspeedSlider;
    [SerializeField] TMP_InputField HyperspeedInput;

    [SerializeField] Slider AmplitudeSlider;
    [SerializeField] TMP_InputField AmplitudeInput;

    [SerializeField] Slider PlaybackSpeedSlider;
    [SerializeField] TMP_InputField PlaybackSpeedInput;

    [SerializeField] TMP_InputField DivisionInput;
    [SerializeField] Button IncreaseDivisionButton;
    [SerializeField] Button DecreaseDivisionButton;
}
