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
        var sustainTrackLength = Waveform.GetWaveformRatio(tick, sustainLength) * Highway3D.highwayLength;
        if (sustainTrackLength < 0) sustainTrackLength = 0;

        sustainTail.localScale = new Vector3(sustainTail.localScale.x, sustainTail.localScale.y, (float)sustainTrackLength);
    }

    public void SetSustainZero()
    {
        sustainTail.localScale = new Vector3(sustainTail.localScale.x, sustainTail.localScale.y, 0);
    }
}