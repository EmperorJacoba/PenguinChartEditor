using System;
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
    
    [SerializeField] private Button startButton;
    [SerializeField] private TMP_Text buttonText;
    [SerializeField] private TMP_Text statusText;

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
            StopMinigame();
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

    private void StopMinigame()
    {
        buttonText.text = "Start";

        deltas.Sort();
        
        Chart.settings.Calibration = (int)Mathf.Round(deltas[deltas.Count / 2] * 1000);
        statusText.text = $"Calculated calibration (median value): {Chart.settings.Calibration}ms";
        print(Chart.settings.Calibration);
        
        game = null;
        minigameActive = false;
        playerStarted = false;
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
            yield return new WaitForSeconds(0.5f);
        }
        StopMinigame();
    }
}