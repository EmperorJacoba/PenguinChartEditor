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
        playerTimestamps.Clear();
        gameTimestamps.Clear();
        game = StartCoroutine(Countdown());
    }

    private void StopMinigame()
    {
        buttonText.text = "Start";
        statusText.gameObject.SetActive(false);

        List<float> deltas = new List<float>();
        for (int i = 0; i < playerTimestamps.Count && i < gameTimestamps.Count; i++)
        {
            deltas.Add(gameTimestamps[i] - playerTimestamps[i]);
        }

        Chart.settings.Calibration = (int)Mathf.Round(deltas.Average() * 1000);
        print($"{deltas.Average()}");
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

    private List<float> playerTimestamps = new();
    private List<float> gameTimestamps = new List<float>();

    private bool playerStarted = false;

    private void Update()
    {
        if (minigameActive)
        {
            if (Keyboard.current.anyKey.wasPressedThisFrame)
            {
                playerStarted = true;
                playerTimestamps.Add(Time.realtimeSinceStartup);
                if (gameTimestamps.Count > 0)
                {
                    statusText.text = $"Delta: {(gameTimestamps.Last() - playerTimestamps.Last())*1000}ms";
                }
            }
        }
    }

    IEnumerator Game()
    {
        for (int i = 0; i < NUMBER_OF_PASSES; i++)
        {
            AudioManager.PlayClapSound();
            if (playerStarted)
            {
                gameTimestamps.Add(Time.realtimeSinceStartup);
            }
            yield return new WaitForSeconds(0.5f);
        }
        StopMinigame();
    }
}