using System;
using System.IO;
using System.Net;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Penguin.Dialogs
{
    public class VersionUpdateDialog : MonoBehaviour, IPointerDownHandler
    {
        [SerializeField] private TMP_Text versionText;
        private static readonly string latestReleasePageURL = "https://github.com/EmperorJacoba/PenguinChartEditor/releases/latest";
        
        private void Awake()
        {
            try
            {
                if (IsLatestVersionMismatch(out var version))
                {
                    versionText.text =
                        $"[NOTICE]\nThere is a new version of Penguin Chart Editor available on GitHub.\n" +
                        $"Current version: v{Application.version}\n" +
                        $"Latest version: {version}\n" +
                        $"Click here to go to the release page.";
                }
                else
                {
                    gameObject.SetActive(false);
                }
            }
            catch (Exception e)
            {
                print($"Error when trying to fetch latest version. Failed, likely due to network error.\n\tSpecifics: {e}");
                gameObject.SetActive(false);
            }
        }
        
        private bool IsLatestVersionMismatch(out string newVersion)
        {
            var currentVersion = "v" + Application.version;
            
            // guide to not fuck this up:
            // 1. upload releases to github formatted as v[application version] (standard)
            // 2. if releases change website, then upload second-to-last version on github, then change the logic here to check
            // for the new source
            // yes this is very unstable but this is the simplest way to check it and the fallout is minimal if it goes wrong
            var expectedPageURL = "https://github.com/EmperorJacoba/PenguinChartEditor/releases/tag/" + currentVersion;
        
            var webRequest = WebRequest.Create(latestReleasePageURL);
            HttpWebResponse httpResponse = (HttpWebResponse)webRequest.GetResponse();
            newVersion = Path.GetFileName(httpResponse.ResponseUri.ToString());
            
            return currentVersion != newVersion;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            System.Diagnostics.Process.Start(latestReleasePageURL);
        }
    }
}