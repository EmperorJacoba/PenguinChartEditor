using UnityEngine;
using UnityEngine.SceneManagement;

public class TabManager : MonoBehaviour
{
    [SerializeField] private GameObject tabDisplayWindow;

    public void HandleTabClick(string scenePressed)
    {
        // Check if the scene is loaded
        if (SceneManager.GetSceneByName(scenePressed).isLoaded) return;
        // else load the passed in scene
        SceneManager.LoadScene(scenePressed, LoadSceneMode.Additive);
    }
}
