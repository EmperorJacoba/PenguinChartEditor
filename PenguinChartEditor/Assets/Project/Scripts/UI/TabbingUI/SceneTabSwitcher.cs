using System;
using System.Linq;
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
        SettingsIcon.UnloadSettingsScene();
        SceneManager.LoadScene(controlledScene, LoadSceneMode.Additive);
    }

    public static void FullRefreshLoadedTab()
    {
        if (loadedTab is not SceneTabSwitcher loadedTabTyped) return;
        loadedTabTyped.SwitchOff();
        loadedTabTyped.SwitchOn();
    }

    public static void LoadScene(string sceneName)
    {
        var tabsFound = tabs.Where(x =>
        {
            var candidate = (SceneTabSwitcher)x;
            return candidate.controlledScene == sceneName;
        }
            );

        if (!tabsFound.Any())
            throw new NullReferenceException($"No tab controlling scene of name {sceneName}. Cannot force load scene.");
        
        tabsFound.First().SwitchOn();
    }
}