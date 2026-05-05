using System;
using UnityEngine;

public class ContainerSceneLoader : MonoBehaviour
{
    private void Start()
    {
        SceneTabSwitcher.LoadScene("SongSetupSceneV2");
        Destroy(gameObject);
    }
}