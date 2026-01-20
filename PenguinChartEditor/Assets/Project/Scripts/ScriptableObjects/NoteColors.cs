using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class NoteColors : ScriptableObject
{
    [SerializeField] private List<ColorPairing> noteColorMaterials = new();
    public Material GetNoteMaterial(int lane, bool isTap, bool isStarpower)
    {
        if (noteColorMaterials.Count > lane)
        {
            if (isTap)
            {
                return isStarpower ? overdriveTapColor : noteColorMaterials[lane].tapMat;
            }
            else
            {
                return isStarpower ? overdriveColor : noteColorMaterials[lane].normalMat;
            }
        }
        else
        {
            Debug.LogWarning(
                $@"Note color list is not set up correctly for this charting mode. 
                Please update this lane positioning script to have the correct number of lanes for this mode. 
                Lane requested: {lane}. Lane positions in {name} = {noteColorMaterials.Count}.
                ");
            return null;
        }
    }

    [SerializeField] private Material normalBorderColor;
    [SerializeField] private Material tapBorderColor;
    [SerializeField] private Material previewerColor;
    [SerializeField] private Material previewerTapColor;
    [SerializeField] private Material defaultBaseColor;
    [SerializeField] private Material nonDefaultBaseColor;

    [SerializeField] private Material overdriveColor;
    [SerializeField] private Material overdriveTapColor;

    // This is used exclusively for open notes, because their color and hopo identifying parts are the same.
    public Material starpowerHopoColor;
    public Material normalHopoColor;

    public Material GetHeadColor(bool tap)
    {
        if (tap) return tapBorderColor;
        return normalBorderColor;
    }

    public Material GetPreviewerMat(bool tap)
    {
        return tap ? previewerTapColor : previewerColor;
    }

    public Material GetBaseColor(bool @default)
    {
        return @default ? defaultBaseColor : nonDefaultBaseColor;
    }
}

[System.Serializable]
internal struct ColorPairing
{
    public Material normalMat;
    public Material tapMat;
}