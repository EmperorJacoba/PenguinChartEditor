using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Tooltip system modeled after this video: https://youtu.be/HXFoUGw7eKk
// Thanks Game Dev Guide! (even though this technically isn't a game)
public class Tooltip : MonoBehaviour
{
    public static Tooltip tooltip;

    public TextMeshProUGUI contentText;
    public LayoutElement layoutElement;
    public int wrapLimit = 20;
    [SerializeField] RectTransform rectTransform;

    void Awake()
    {
        tooltip = this;
        tooltip.gameObject.SetActive(false);
    }

    public void SetText(string content)
    {
        if (string.IsNullOrEmpty(content)) contentText.gameObject.SetActive(false);
        else
        {
            contentText.gameObject.SetActive(true);
            contentText.text = content;
        }

        var contentLength = contentText.text.Length;

        layoutElement.enabled = (contentLength > wrapLimit) ? true : false;
    }

    void Update()
    {
        Vector2 mousePosition = Input.mousePosition;
        transform.position = mousePosition;

        rectTransform.pivot = GetDesiredPivot();
    }

    Vector2 GetDesiredPivot()
    {
        int pivotX = 0;
        int pivotY = 0;

        if (transform.position.x + rectTransform.rect.width > Screen.width)
        {
            pivotX = 1;
        }

        if (transform.position.y + rectTransform.rect.height > Screen.height)
        {
            pivotY = 1;
        }

        return new Vector3(pivotX, pivotY);
    }

    public static void NewTooltip(string text)
    {
        tooltip.gameObject.SetActive(true);
        tooltip.SetText(text);
    }

    public static void Deactivate() => tooltip.gameObject.SetActive(false);
}
