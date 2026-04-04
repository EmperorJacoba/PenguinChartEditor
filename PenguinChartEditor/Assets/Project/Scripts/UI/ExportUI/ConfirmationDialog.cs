using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

    public void Initialize(string title, Action positiveResultFunc)
    {
        descriptionText.text = title;
        runOnYesAction = positiveResultFunc;
    }

    private void Yes()
    {
        runOnYesAction();
        Destroy(gameObject);
    }

    private void No()
    {
        Destroy(gameObject);
    }
}