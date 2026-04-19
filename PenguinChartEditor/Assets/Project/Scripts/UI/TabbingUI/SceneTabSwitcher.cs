using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTabSwitcher : BasicPenguinTab<SceneTabSwitcher>
{
    [SerializeField] private string controlledScene;
    public static string forceLoadedScene = null;
    
    protected override void OnSwitchOff()
    {
        SceneManager.UnloadSceneAsync(forceLoadedScene ?? controlledScene);
        forceLoadedScene = null;
    }

    protected override void OnSwitchOn()
    {
        SceneManager.LoadScene(controlledScene, LoadSceneMode.Additive);
    }
}