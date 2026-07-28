using System;
using System.Collections.Generic;
using UnityEngine;

namespace Penguin.Dialogs
{
    public class DialogManager : MonoBehaviour
    {
        public static DialogManager instance;

        [SerializeField] private ConfirmationDialog confirmationDialog;
        [SerializeField] private DataWipeDialog dataWipeDialog;
        [SerializeField] private ErrorNotificationDialog errorNotificationDialog;
        [SerializeField] private LoadingDialog loadingDialog;
        
        public static T SpawnDialog<T>()
        {
            UnityEngine.Debug.Assert(instance is not null, "No dialog manager to pull dialogs from");
            
            if (typeof(T) == typeof(ErrorNotificationDialog)) return (T)(object)instance.errorNotificationDialog;
            if (typeof(T) == typeof(ConfirmationDialog)) return (T)(object)instance.confirmationDialog;
            if (typeof(T) == typeof(DataWipeDialog)) return (T)(object)instance.dataWipeDialog;
            if (typeof(T) == typeof(LoadingDialog)) return (T)(object)(instance.loadingDialog);
            
            throw new ArgumentOutOfRangeException($"No support for creating dialog of type {typeof(T)}");
        }

        private void Awake()
        {
            instance = this;
        }
    }
}