using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Penguin.Dialogs
{
    public class ErrorNotificationDialog : MonoBehaviour
    {
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private Button okButton;
        private Action runOnYesAction;

        private void Awake()
        {
            okButton.onClick.AddListener(Okay);
        }

        public void Initialize(string title, Action positiveResultFunc, float width = 830, float height = 430)
        {
            gameObject.SetActive(true);
            
            descriptionText.text = title;
            var rt = GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(width, height);
            rt.position = new Vector3(Screen.width / 2.0f, Screen.height / 2.0f);
        }

        private void Okay()
        {
            gameObject.SetActive(false);
        }
    }
}