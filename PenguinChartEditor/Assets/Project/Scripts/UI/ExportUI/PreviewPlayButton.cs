using System.Collections;
using UnityEngine;
using Button = UnityEngine.UI.Button;

public class PreviewPlayButton : MonoBehaviour
{
    [SerializeField] private PlayPauseIconReference iconRef;
    private Button attachedButton;
    private float previewClipLength = 15.0f;

    private void Awake()
    {
        attachedButton = GetComponent<Button>();
        attachedButton.onClick.AddListener(PlayFromPreviewStart);
    }

    private Coroutine activeCoroutine;
    private void PlayFromPreviewStart()
    {
        if (activeCoroutine is null)
        {
            AudioManager.ForcePlayFromPosition(Chart.Metadata.PreviewStartTime);
            activeCoroutine = StartCoroutine(StartPreviewTimer());
            attachedButton.image.sprite = iconRef.pauseIcon;
        }
        else
        {
            WrapButtonAction();
        }
    }

    private void WrapButtonAction()
    {
        AudioManager.ForceStop();
        activeCoroutine = null;
        attachedButton.image.sprite = iconRef.playIcon;
    }

    IEnumerator StartPreviewTimer()
    {
        yield return new WaitForSeconds(previewClipLength);
        WrapButtonAction();
    }
}