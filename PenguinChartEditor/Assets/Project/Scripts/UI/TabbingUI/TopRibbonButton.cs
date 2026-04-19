using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class TopRibbonButton : MonoBehaviour
{
    [SerializeField] private GameObject controlledPanel;
    private Button button;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(EnableContent);
    }

    private void Update()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame && controlledPanel.activeInHierarchy)
        {
            controlledPanel.SetActive(false);
        }
    }

    private void EnableContent()
    {
        controlledPanel.SetActive(true);
    }
}