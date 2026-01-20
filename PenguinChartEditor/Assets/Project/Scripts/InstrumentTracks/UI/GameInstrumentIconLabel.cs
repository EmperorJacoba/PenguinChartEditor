using UnityEngine;
using TMPro;

/// <summary>
/// The label displaying the instrument & difficulty corresponding to a track in multi-track (starpower) mode.
/// </summary>
[RequireComponent(typeof(MeshRenderer))]
public class GameInstrumentIconLabel : MonoBehaviour
{
    [SerializeField] public GameInstrument parentGameInstrument;
    [SerializeField] private InstrumentIcons iconMatcher;
    private MeshRenderer iconMesh;
    [SerializeField] private TMP_Text difficultyText;

    private void Awake()
    {
        iconMesh = GetComponent<MeshRenderer>();

        iconMesh.material = iconMatcher.GetInstrumentIcon(parentGameInstrument.instrumentID);
        difficultyText.text = InstrumentMetadata.GetDifficultyAbbreviation(parentGameInstrument.instrumentID);
    }
}