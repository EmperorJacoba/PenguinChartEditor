using UnityEngine;
using UnityEngine.UIElements;

[System.Serializable]
public struct FiveFretAnatomy
{
    [SerializeField] public bool isPreviewer;

    [SerializeField] public NoteColors colorPalette;

    [SerializeField] public GameObject noteModel;

    [SerializeField] public Transform sustainTail;

    [SerializeField] public MeshRenderer sustainColorMesh;
    [SerializeField] public MeshRenderer noteColorMesh;
    [SerializeField] public MeshRenderer headBorderMesh;
    [SerializeField] public MeshRenderer noteBaseMesh;

    [SerializeField] public GameObject hopoTopper;
    [SerializeField] public GameObject strumTopper;
    [SerializeField] public GameObject tapTopper;

    public void ChangeColor(FiveFretInstrument.LaneOrientation newLane, bool isTap)
    {
        if (colorPalette == null) return;

        noteColorMesh.material = isPreviewer ? colorPalette.GetPreviewerMat(false) : colorPalette.GetNoteMaterial((int)newLane, isTap);
        sustainColorMesh.material = isPreviewer ? colorPalette.GetPreviewerMat(false) : colorPalette.GetNoteMaterial((int)newLane, isTap);
    }

    public void ChangeHopo(bool status)
    {
        strumTopper.SetActive(!status);
        hopoTopper.SetActive(status);
    }

    public void ChangeTap(FiveFretInstrument.LaneOrientation lane, bool status)
    {
        noteColorMesh.material = isPreviewer ? colorPalette.GetPreviewerMat(status) : colorPalette.GetNoteMaterial((int)lane, status);

        // this script is also on opens
        // opens do not have head borders and thus borders will be null
        if (headBorderMesh != null)
        {
            headBorderMesh.material = colorPalette.GetHeadColor(status);
        }
        strumTopper.SetActive(!status);
        tapTopper.SetActive(status);
    }

    public void ChangeDefault(bool status)
    {
        noteBaseMesh.material = colorPalette.GetBaseColor(status);
    }

    public void SetVisibility(bool visible)
    {
        if (noteModel.activeInHierarchy != visible) noteModel.SetActive(visible);
    }

    public void UpdateSustainLength(int tick, int sustainLength, float noteLocalTransformPosition)
    {
        var sustainEndPointTicks = tick + sustainLength;

        var endProportion = Waveform.GetWaveformRatio(sustainEndPointTicks);
        var trackPosition = endProportion * Chart.instance.SceneDetails.HighwayLength;

        var noteProportion = Waveform.GetWaveformRatio(tick);
        var notePosition = noteProportion * Chart.instance.SceneDetails.HighwayLength;

        var localScaleZ = (float)(trackPosition - notePosition);

        // stop it from appearing past the end of the highway
        if (localScaleZ + noteLocalTransformPosition > Chart.instance.SceneDetails.HighwayLength)
            localScaleZ = Chart.instance.SceneDetails.HighwayLength - noteLocalTransformPosition;

        if (localScaleZ < 0) localScaleZ = 0; // box collider negative size issues??

        sustainTail.localScale = new Vector3(sustainTail.localScale.x, sustainTail.localScale.y, localScaleZ);
    }
}