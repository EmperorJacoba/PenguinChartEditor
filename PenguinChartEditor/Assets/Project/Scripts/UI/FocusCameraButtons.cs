using UnityEngine;
using UnityEngine.UI;

public class FocusCameraButtons : MonoBehaviour
{
    [SerializeField] Button rightButton;
    [SerializeField] Button leftButton;
    [SerializeField] Camera mainCamera;

    private void Awake()
    {
        leftButton.onClick.AddListener(MoveCameraLeft);
        rightButton.onClick.AddListener(MoveCameraRight);
    }

    void MoveCameraLeft()
    {
        mainCamera.transform.position += (Vector3.left * 10);
    }

    void MoveCameraRight()
    {
        mainCamera.transform.position += (Vector3.right * 10);
    }
}
