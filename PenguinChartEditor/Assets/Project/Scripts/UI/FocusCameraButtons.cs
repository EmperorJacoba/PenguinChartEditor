using UnityEngine;
using UnityEngine.UI;

public class FocusCameraButtons : MonoBehaviour
{
    [SerializeField] Button rightButton;
    [SerializeField] Button leftButton;
    [SerializeField] public Camera mainCamera;

    private void Awake()
    {
        leftButton.onClick.AddListener(MoveCameraLeft);
        rightButton.onClick.AddListener(MoveCameraRight);
    }

    void MoveCameraLeft()
    {
        if (mainCamera.transform.position.x <= InstrumentSpawningManager.instance.OutOfBoundsPosLeft)
        {
            return;
        }

        mainCamera.transform.position += (Vector3.left * 10);
    }

    void MoveCameraRight()
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
