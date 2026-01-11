using UnityEngine;
using TMPro;

[RequireComponent(typeof(MeshRenderer))]
public class GameInstrumentIconLabel : MonoBehaviour
{
    [SerializeField] GameInstrument parentGameInstrument;
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