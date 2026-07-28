using TMPro;
using UnityEngine;

namespace Penguin.Dialogs
{
    public class LoadingDialog : MonoBehaviour
    {
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text descriptionText;
        
        public void Initialize(string title, float width = 1660, float height = 860)
        {
            gameObject.SetActive(true);
            
            titleText.text = title;
            descriptionText.text = "Operation started.";
            var rt = GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(width, height);
            rt.anchoredPosition = Vector3.zero;
        }

        public void UpdateLoadingState(string flavorText)
        {
            descriptionText.text = flavorText;
        }
    }
}