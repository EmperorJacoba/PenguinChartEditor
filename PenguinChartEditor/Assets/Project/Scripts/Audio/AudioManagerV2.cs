using System;
using System.IO;
using ManagedBass;
using UnityEngine;

public class AudioManagerV2 : MonoBehaviour
{
    public void Initialize()
    {
        if (Bass.Init(-1, Flags: DeviceInitFlags.Default))
        {
            string path = $"{Application.dataPath}/Plugins/Bass/";

#if (UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN)
            path += "Bass_win";
#endif
#if (UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX)
            path += "Bass_macOS";
#endif
#if (UNITY_EDITOR_LINUX || UNITY_STANDALONE_LINUX)
            path += "Bass_linux/x86_64";
#endif
            
            foreach (var file in Directory.EnumerateFiles(path))
            {
                if (Bass.PluginLoad(file) == 0)
                {
                    Debug.LogWarning($"Plugin Load error. Bass Error: {Bass.LastError}");
                }
            }
        }
    }
}