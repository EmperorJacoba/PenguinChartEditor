using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Penguin.Dialogs
{
    public class ConfirmationDialog : MonoBehaviour
    {
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private Button yesButton;
        [SerializeField] private Button noButton;
        private Action runOnYesAction;

        private void Awake()
        {
            yesButton.onClick.AddListener(Yes);
            noButton.onClick.AddListener(No);
        }

        public void Initialize(string title, Action positiveResultFunc, float width = 415, float height = 215)
        {
            gameObject.SetActive(true);
            
            descriptionText.text = title;
            runOnYesAction = positiveResultFunc;
            var rt = GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(width, height);
            rt.anchoredPosition = Vector3.zero;
        }

        private void Yes()
        {
            runOnYesAction();
            gameObject.SetActive(false);
        }

        private void No()
        {
            gameObject.SetActive(false);
        }
    }
}