using UnityEngine;

[System.Serializable]
public struct StarpowerAnatomy
{
    [SerializeField] public bool isPreviewer;

    [SerializeField] public Transform sustainTail;
    [SerializeField] public Transform trackOverlay;

    [SerializeField] public MeshRenderer colorMesh;
    [SerializeField] public MeshRenderer colorSphereTopperMesh;

    [SerializeField] private Material previewerColor;
    [SerializeField] private Material normalColor;
    [SerializeField] private Material fillColor;

    public void UpdateSustainLength(int tick, int sustainLength)
    {
        var sustainTrackLength = Waveform.GetWaveformRatio(tick, sustainLength) * Highway3D.highwayLength;
        if (sustainTrackLength < 0) sustainTrackLength = 0;

        sustainTail.localScale = new(sustainTail.localScale.x, sustainTail.localScale.y, (float)sustainTrackLength);
        trackOverlay.localScale = new(trackOverlay.localScale.x, trackOverlay.localScale.y, (float)sustainTrackLength);
    }

    public void ChangeColorToPreviewer()
    {
        colorMesh.material = colorSphereTopperMesh.material = previewerColor;
    }
}