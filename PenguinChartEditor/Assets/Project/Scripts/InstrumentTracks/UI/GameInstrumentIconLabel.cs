using UnityEngine;
using TMPro;

/// <summary>
/// The label displaying the instrument & difficulty corresponding to a track in multi-track (starpower) mode.
/// </summary>
[RequireComponent(typeof(MeshRenderer))]
public class GameInstrumentIconLabel : MonoBehaviour
{
    [SerializeField] public GameInstrument parentGameInstrument;
    [SerializeField] InstrumentIcons iconMatcher;
    MeshRenderer iconMesh;
    [SerializeField] TMP_Text difficultyText;

    private void Awake()
    {
        iconMesh = GetComponent<MeshRenderer>();

        iconMesh.material = iconMatcher.GetInstrumentIcon(parentGameInstrument.instrumentID);
        difficultyText.text = InstrumentMetadata.GetDifficultyAbbreviation(parentGameInstrument.instrumentID);
    }
}