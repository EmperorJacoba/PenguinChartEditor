using System.Collections.Generic;
using System.IO;
using SFB;
using UnityEngine;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using UnityEngine.EventSystems;
using Color = UnityEngine.Color;
using UnityImage = UnityEngine.UI.Image;

public class SongImageProcessor : MonoBehaviour, IPointerDownHandler
{
    enum ImageType
    {
        album,
        background
    }

    [SerializeField] private ImageType imageToProcess;
    [SerializeField] private List<GameObject> disableObjectsOnImageLoad; 
    private UnityImage imageComponent;

    private int width => imageToProcess == ImageType.album ? 512 : 1920;
    private int height => imageToProcess == ImageType.album ? 512 : 1080;

    private void Awake()
    {
        imageComponent = GetComponent<UnityImage>();
        LoadImageFromDisk();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        var pathCandidates = 
            StandaloneFileBrowser.OpenFilePanel
            (
                $"Open {imageToProcess} image.", 
                Chart.FolderPath, 
                new[]
                {
                    new ExtensionFilter(
                        "Images", 
                        // all images supported by ImageSharp
                        "bmp", "gif", "jpeg", "jpg", "pbm", "png", "tiff", "tga", "webp", "qoi")
                }, 
                false
            );

        if (pathCandidates.Length < 1) return;
        var imagePath = pathCandidates[0];

        using var image = Image.Load(imagePath);
        
        image.Mutate(x => x.Resize(width, height));
        
        if (!Directory.Exists(UserSettings.MetadataImagePaths))
        {
            Directory.CreateDirectory(UserSettings.MetadataImagePaths);
        }
        var savedImagePath = $"{UserSettings.MetadataImagePaths}/{System.Guid.NewGuid().ToString()}.jpg";
        
        image.SaveAsJpeg(savedImagePath);
        if (imageToProcess == ImageType.album)
        {
            Chart.Metadata.CoverPath = savedImagePath;
        }
        else
        {
            Chart.Metadata.BackgroundPath = savedImagePath;
        }
        
        LoadImageFromDisk();
    }

    private void LoadImageFromDisk()
    {
        var targetPath = imageToProcess == ImageType.album ? Chart.Metadata.CoverPath : Chart.Metadata.BackgroundPath;
        if (!File.Exists(targetPath)) return;
        
        var rawFileData = File.ReadAllBytes(targetPath);
        var texture = new Texture2D(width, height);
            
        if (!texture.LoadImage(rawFileData)) return;
            
        var sprite = Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0.5f, 0.5f));
        imageComponent.sprite = sprite;
        
        disableObjectsOnImageLoad.ForEach(x => x.SetActive(false));
        imageComponent.color = Color.white;
    }
}