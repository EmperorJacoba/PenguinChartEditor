using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class CalibrationGame : MonoBehaviour
{
    private const int NUMBER_OF_PASSES = 30;
    private const float TIME_BETWEEN_CLICKS = 0.5f;
    
    [SerializeField] private Button startButton;
    [SerializeField] private TMP_Text buttonText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private SettingsInputFieldHandler calibrationInput; 

    public bool minigameActive = false;

    private Coroutine game;

    private void Awake()
    {
        startButton.onClick.AddListener(StartButtonCallback);
        statusText.gameObject.SetActive(false);
    }

    private void StartButtonCallback()
    {
        if (game != null)
        {
            CancelMinigame();
        }
        else StartMinigame();
    }
    
    private void StartMinigame()
    {
        if (game != null)
        {
            StopCoroutine(game);
        }
        
        buttonText.text = "Stop";
        statusText.gameObject.SetActive(true);
        game = StartCoroutine(Countdown());
    }

    private void CancelMinigame()
    {
        StopCoroutine(game);
    }

    private void StopMinigame()
    {
        buttonText.text = "Start";
        
        Chart.settings.Calibration = (int)Mathf.Round(deltas.Average() * 1000);
        statusText.text = $"Calculated calibration (average): {Chart.settings.Calibration}ms";
        
        game = null;
        minigameActive = false;
        playerStarted = false;
        calibrationInput.ForceUpdate();
    }

    IEnumerator Countdown()
    {
        statusText.text = "3";
        yield return new WaitForSeconds(0.5f);
        statusText.text = "2";
        yield return new WaitForSeconds(0.5f);
        statusText.text = "1";
        yield return new WaitForSeconds(0.5f);
        
        minigameActive = true;
        statusText.text = $"Delta: n/a";
        
        game = StartCoroutine(Game());
    }

    List<float> deltas = new List<float>();
    
    private bool playerStarted = false;

    private void Update()
    {
        if (!minigameActive) return;
        if (!Keyboard.current.anyKey.wasPressedThisFrame) return;
        
        playerStarted = true;
        var p = Time.realtimeSinceStartup;
        var delta = p - lastGameTimestamp;

        if (delta > TIME_BETWEEN_CLICKS / 2)
        {
            delta = (lastGameTimestamp - p) + TIME_BETWEEN_CLICKS;
        }
        
        statusText.text = $"Delta: {delta*1000}ms";
        deltas.Add(delta);
    }

    private float lastGameTimestamp;

    IEnumerator Game()
    {
        for (int i = 0; i < NUMBER_OF_PASSES; i++)
        {
            AudioManager.PlayClapSound();
            lastGameTimestamp = Time.realtimeSinceStartup;
            yield return new WaitForSeconds(TIME_BETWEEN_CLICKS);
        }
        StopMinigame();
    }
}