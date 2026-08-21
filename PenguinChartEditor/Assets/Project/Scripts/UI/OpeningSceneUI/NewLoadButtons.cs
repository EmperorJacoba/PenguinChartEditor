using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class NewLoadButtons : MonoBehaviour
{
    [SerializeField] private Button newButton;
    [SerializeField] private Button loadButton;

    private void Awake()
    {
        newButton.onClick.AddListener(OnNew);
        loadButton.onClick.AddListener(OnLoad);
    }

    private void OnNew()
    {
        if (Chart.NewFile())
        {
            SceneManager.LoadScene("ContainerSceneV2");
        }
    }

    private void OnLoad() => Chart.LoadFile();
}