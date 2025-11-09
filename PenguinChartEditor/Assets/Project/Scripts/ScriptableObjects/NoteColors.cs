using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class NoteColors : ScriptableObject
{
    [SerializeField] List<Material> noteColorMaterials = new();
    public Material GetNoteMaterial(int lane)
    {
        if (noteColorMaterials.Count > lane)
        {
            return noteColorMaterials[lane];
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
}