using System;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;

namespace Penguin.Dialogs
{
    public class LoadingDialog : MonoBehaviour
    {
        public enum OperationType
        {
            @new,
            load,
            export
        }
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text descriptionText;

        private Task activeTask;
        private Action funcToRunWhenComplete;
        private OperationType loadedOperation;
        
        public void Initialize(
            string title, 
            Task task,
            Action onSuccessfulCompletion, 
            OperationType operationType, 
            float width = 1660, float height = 860
            )
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
                    descriptionText.text = Chart.operationUpdateString;
                    break;
                case TaskStatus.RanToCompletion:
                    print("Completed");
                    gameObject.SetActive(false);
                    funcToRunWhenComplete();
                    break;
                case TaskStatus.Canceled:
                case TaskStatus.Faulted:
                    gameObject.SetActive(false);
                    print($"Exception occured during file \"{loadedOperation}\" operation. Exception: \n\t{activeTask.Exception}");
                    SpawnErrorDialog();
                    break;
            }
        }

        private void SpawnErrorDialog()
        {
            var dialog = DialogManager.SpawnDialog<ErrorNotificationDialog>();
            dialog.Initialize($"There was an error during the \"{loadedOperation}\" operation. Please check the log file for more details.");
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