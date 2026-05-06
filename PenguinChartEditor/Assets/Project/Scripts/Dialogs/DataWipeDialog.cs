using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Penguin.Dialogs
{
    public class DataWipeDialog : MonoBehaviour
    {
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private Button yesButton;
        [SerializeField] private Button noButton;
        [SerializeField] private Button cancelButton;
        private Func<bool> runOnYesAction;
        private Func<bool> runOnNoAction;

        private void Awake()
        {
            yesButton.onClick.AddListener(Yes);
            noButton.onClick.AddListener(No);
            cancelButton.onClick.AddListener(Cancel);
        }

        public void Initialize(
            string title, 
            Func<bool> positiveResultFunc, 
            Func<bool> negativeResultFunc,
            float width = 650, 
            float height = 215
        )
        {
            gameObject.SetActive(true);
            
            descriptionText.text = title;
            runOnYesAction = positiveResultFunc;
            runOnNoAction = negativeResultFunc;
            var rt = GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(width, height);
            rt.position = new Vector3(Screen.width / 2.0f, Screen.height / 2.0f);
        }

        private void Yes()
        {
            runOnYesAction();
            gameObject.SetActive(false);
        }

        private void No()
        {
            runOnNoAction();
            gameObject.SetActive(false);
        }

        private void Cancel()
        {
            gameObject.SetActive(false);
        }
    }
}