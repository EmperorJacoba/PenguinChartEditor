using System;
using UnityEngine;
using UnityEngine.UI;

public class Spectrogram : MonoBehaviour
{
    [SerializeField] int sliceWidth;
    [SerializeField] int sliceHeight;
    [SerializeField] int FFTStartTime;

    private void Update()
    {
        Image spectrogramImage = GetComponent<Image>();
        Texture2D outputTexture = new Texture2D(sliceWidth, sliceHeight);

        Rect rect = new Rect(0, 0, sliceWidth, sliceHeight);
        Vector2 pivot = Vector2.one * 0.5f;
        spectrogramImage.sprite = Sprite.Create(outputTexture, rect, pivot);

        float[] FFTData = AudioManager.GetSpectrogramData(FFTStartTime);
        for (int i = 0; i < FFTData.Length; i++)
        {
            Debug.Log(FFTData[i]);
        }
        
    }
}
