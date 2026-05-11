using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SettingsIcon : MonoBehaviour
{
    private Button button;
    private static bool settingsLoaded;

    private void Awake()
    {
        button = GetComponent<Button>();
        button.onClick.AddListener(OnLoadSettings);
    }

    private void OnLoadSettings()
    {
        if (settingsLoaded) return;
        
        SceneTabSwitcher.SwitchOffActiveTab();

        SceneManager.LoadScene("SettingsScene", LoadSceneMode.Additive);
        settingsLoaded = true;
    }

    public static void UnloadSettingsScene()
    {
        if (!settingsLoaded) return;
        
        SceneManager.UnloadSceneAsync("SettingsScene");
        settingsLoaded = false;
        return;
    }
}