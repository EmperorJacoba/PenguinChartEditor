using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TabSceneSpawningManager : MonoBehaviour
{
    private bool tabbingRibbonActive = false;
    // Should be the ribbon border. 105 pixels tall as of writing this
    [SerializeField] private RectTransform widestRibbonElement;
    public static TabSceneSpawningManager instance;

    public static bool IsTabbingActive() => instance is not null && instance.tabbingRibbonActive;
    
    public static Vector2 RealUISize
    {
        get
        {
            return IsTabbingActive()
                ? new Vector2(Screen.width, Screen.height - instance.widestRibbonElement.rect.height)
                : new Vector2(Screen.width, Screen.height);
        }
    }
    public static Vector2 RealUICenter
    {
        get
        {
            return IsTabbingActive()
                ? new Vector2(Screen.width / 2.0f, (Screen.height - instance.widestRibbonElement.rect.height) / 2.0f)
                : new Vector2(Screen.width / 2.0f, Screen.height / 2.0f);
        }
    }

    public static float YCenterDelta
    {
        get
        {
            return (Screen.height / 2.0f) - (RealUICenter.y);
        }
    }

    // Use this to adjust the Y position of canvas elements to be centered properly in their window.
    public float HeightOffset => widestRibbonElement.rect.height;
    
    private void Awake()
    {
        tabbingRibbonActive = true;
        instance = this;
    }

    private void OnDisable()
    {
        instance = null;
    }

    private void Start()
    {
    }
}