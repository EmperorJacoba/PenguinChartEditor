using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class NoteColors : ScriptableObject
{
    [SerializeField] List<ColorPairing> noteColorMaterials = new();
    public Material GetNoteMaterial(int lane, bool tap)
    {
        if (noteColorMaterials.Count > lane)
        {
            return tap ? noteColorMaterials[lane].tapMat : noteColorMaterials[lane].normalMat;
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

    [SerializeField] Material normalBorderColor;
    [SerializeField] Material tapBorderColor;

    public Material GetHeadColor(bool tap)
    {
        if (tap) return tapBorderColor;
        return normalBorderColor;
    }
}

[System.Serializable]
internal struct ColorPairing
{
    public Material normalMat;
    public Material tapMat;
}