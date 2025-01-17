using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class TempoTrack : MonoBehaviour
{
    [SerializeField] PluginBassManager pluginBassManager;
    



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Texture2D waveformDisplayOutput = pluginBassManager.GetWaveform();
        Rect rect = new(Vector2.zero, new Vector2(pluginBassManager.width, pluginBassManager.height));
        SpriteRenderer spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
        spriteRenderer.sprite = Sprite.Create(waveformDisplayOutput, rect, Vector2.zero);

        var renderer = GetComponent<Renderer>();
        renderer.material.mainTexture = waveformDisplayOutput;
    }
}
