using System.Collections;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;

namespace Penguin.Dialogs
{
    public class VersionUpdateDialog : MonoBehaviour, IPointerDownHandler
    {
        [SerializeField] private TMP_Text versionText;
        private static readonly string latestReleasePageURL = "https://github.com/EmperorJacoba/PenguinChartEditor/releases/latest";

        private void Awake()
        {
            StartCoroutine(CheckForVersionMismatch());
        }

        IEnumerator CheckForVersionMismatch()
        {
            var request = UnityWebRequest.Get(latestReleasePageURL);
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                UnityEngine.Debug.LogError($"Could not get releases page. Error:\n\t{request.error}");
            }
            else
            {
                var serverVersion = Path.GetFileName(request.uri.ToString());
                var applicationVersion = "v" + Application.version;

                if (serverVersion != applicationVersion)
                {
                    versionText.text =
                        $"[NOTICE]\nThere is a new version of Penguin Chart Editor available on GitHub.\n" +
                        $"Current version: {applicationVersion}\n" +
                        $"Latest version: {serverVersion}\n" +
                        $"Click here to go to the release page.";
                }
                else
                {
                    gameObject.SetActive(false);
                }
            }
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            System.Diagnostics.Process.Start(latestReleasePageURL);
        }
    }
}