using System;
using System.Collections;
using TMPro;
using UnityEngine;

public class RightHeaderText : MonoBehaviour
{
    [SerializeField] private TMP_Text version;
    [SerializeField] private TMP_Text saved;

    public static RightHeaderText instance;

    private void Awake()
    {
        instance = this;
        version.text = $"{Application.version}";
        saved.gameObject.SetActive(false);
    }

    public void ShowSaved()
    {
        saved.gameObject.SetActive(true);
        saved.text = "Saved.";
        StartCoroutine(DisableTextAfterBuffer());
    }

    IEnumerator DisableTextAfterBuffer()
    {
        yield return new WaitForSeconds(1.5f);
        saved.gameObject.SetActive(false);
    }

    public void ShowError()
    {
        saved.gameObject.SetActive(true);
        saved.text = "Error when saving file. Please check the log file.";
    }
}