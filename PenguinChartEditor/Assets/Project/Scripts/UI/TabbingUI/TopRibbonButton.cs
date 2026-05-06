using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class TopRibbonButton : MonoBehaviour
{
    [SerializeField] private GameObject controlledPanel;
    private Button button;

    private Vector3 originalCoordinates;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(EnableContent);
    
        controlledPanel.SetActive(true);
        originalCoordinates = controlledPanel.transform.position;
            
        controlledPanel.transform.position = Vector3.left * 100000;
    }

    
    private void LateUpdate()
    {
        if (Input.GetMouseButtonDown(0) && controlledPanel.transform.position.x > 0)
        {
            controlledPanel.transform.position = Vector3.left * 100000;
        }
    }

    public void DisableContent()
    {
        controlledPanel.transform.position = Vector3.left * 100000;
    }
    
    private void EnableContent()
    {
        controlledPanel.transform.position = originalCoordinates;
    }
}