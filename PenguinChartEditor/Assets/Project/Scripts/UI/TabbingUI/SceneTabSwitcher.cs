using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTabSwitcher : BasicPenguinTab<SceneTabSwitcher>
{
    [SerializeField] private string controlledScene;
    
    protected override void OnSwitchOff()
    {
        SceneManager.UnloadSceneAsync(controlledScene);
    }

    protected override void OnSwitchOn()
    {
        SceneManager.LoadScene(controlledScene, LoadSceneMode.Additive);
    }
}