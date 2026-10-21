using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Penguin.Dialogs
{
    public class ErrorNotificationDialog : MonoBehaviour
    {
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private Button okButton;
        [SerializeField] private Button logButton;
        private Action runOnYesAction;

        private void Awake()
        {
            okButton.onClick.AddListener(Okay);
            logButton.onClick.AddListener(ViewLog);
        }

        public void Initialize(string title, float width = 830, float height = 430)
        {
            gameObject.SetActive(true);
            
            descriptionText.text = title;
            var rt = GetComponent<RectTransform>();
            
            rt.sizeDelta = new Vector2(width, height);
            rt.anchoredPosition = Vector3.zero;
        }

        private void Okay()
        {
            gameObject.SetActive(false);
        }

        private void ViewLog()
        {
            // Shouldn't be an issue since all Penguin platforms support logs but ya neva know
            if (Application.consoleLogPath == "") return;
            Application.OpenURL(Application.consoleLogPath);
            print(Application.consoleLogPath);
            Okay();
        }
    }
}