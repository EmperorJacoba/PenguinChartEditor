using UnityEngine;
using SixLabors.ImageSharp;
using UnityEngine.EventSystems;

public class SongImageProcessor : MonoBehaviour, IPointerDownHandler
{
    enum ImageType
    {
        album,
        background
    }

    [SerializeField] private ImageType imageToProcess;

    public void OnPointerDown(PointerEventData eventData)
    {
        throw new System.NotImplementedException();
    }
}