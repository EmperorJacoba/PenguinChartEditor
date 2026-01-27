using UnityEngine;
using UnityEngine.UI;

public class FocusCameraButtons : MonoBehaviour
{
    [SerializeField] private Button rightButton;
    [SerializeField] private Button leftButton;
    [SerializeField] public Camera mainCamera;

    private void Awake()
    {
        leftButton.onClick.AddListener(MoveCameraLeft);
        rightButton.onClick.AddListener(MoveCameraRight);
    }

    private void MoveCameraLeft()
    {
        if (mainCamera.transform.position.x <= InstrumentSpawningManager.instance.OutOfBoundsPosLeft)
        {
            return;
        }

        mainCamera.transform.position += (Vector3.left * 10);
    }

    private void MoveCameraRight()
    {
        if (mainCamera.transform.position.x >= InstrumentSpawningManager.instance.OutOfBoundsPosRight)
        {
            return;
        }

        mainCamera.transform.position += (Vector3.right * 10);
    }

    public void ResetCameraPosition()
    {
        mainCamera.transform.position = new Vector3(0, mainCamera.transform.position.y, mainCamera.transform.position.z);
    }
}
