using System;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace Penguin.Dialogs
{
    public class LoadingDialog : MonoBehaviour
    {
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text descriptionText;

        private Task activeTask;
        private Action funcToRunWhenComplete;
        
        public void Initialize(string title, Task task, Action onSuccessfulCompletion, float width = 1660, float height = 860)
        {
            gameObject.SetActive(true);

            activeTask = task;
            funcToRunWhenComplete = onSuccessfulCompletion;
            
            titleText.text = title;
            descriptionText.text = "Operation started.";
            
            var rt = GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(width, height);
            rt.anchoredPosition = Vector3.zero;
        }

        private void Update()
        {
            switch (activeTask.Status)
            {
                case TaskStatus.Running:
                    print("Running");
                    break;
                case TaskStatus.RanToCompletion:
                    print("Completed");
                    gameObject.SetActive(false);
                    funcToRunWhenComplete();
                    break;
                case TaskStatus.Canceled:
                case TaskStatus.Faulted:
                    print("Failed.");
                    print(activeTask.Exception);
                    break;
            }
        }

        public void UpdateLoadingState(string flavorText)
        {
            descriptionText.text = flavorText;
        }

        public void Despawn()
        {
            gameObject.SetActive(false);
        }
    }
}