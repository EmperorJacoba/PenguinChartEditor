using System;
using UnityEngine;

public class Waveform3D : Waveform
{
    [SerializeField] Transform highway;
    // GenerateWaveformPoints
    // SetWaveformVisibility

    protected override void Awake()
    {
        base.Awake();
        wf2D = false;
        Chart.currentTab = Chart.TabType.Chart;
    }
    float ShrinkFactor3D => ShrinkFactor * 5;
    float Amplitude3D => Amplitude * 5;

    /// <summary>
    /// Generate an array of line renderer positions based on waveform audio.
    /// </summary>
    /// <returns>Vector3 array of line renderer positions</returns>
    protected override void GenerateWaveformPoints()
    {
        float[] waveformData;
        if (WaveformData.ContainsKey(CurrentWaveform) && isVisible)
        {
            waveformData = WaveformData[CurrentWaveform].volumeData;
        }
        else
        {
            // this is to generate waveform data even if there is either
            // a) no data available (no audio loaded)
            // or b) a call when CurrentWaveform = 0 (none) occurs.
            // this lets the for loop below execute because it can't without SOMETHING in waveformData.
            // the if statement in that loop is always true when an empty float[] exists in waveformData,
            // so it accurately represents no data (even though the "waveform" is actually behind the track)
            waveformData = new float[0];
        }

        var sampleCount = samplesPerBoundary;
        var startSampleIndex = CurrentWaveformDataPosition - strikeSamplePoint;

        // each line renderer point is a sample
        lineRendererMain.positionCount = sampleCount;
        lineRendererMirror.positionCount = sampleCount;

        Vector3[] lineRendererPositions = new Vector3[lineRendererMain.positionCount];
        float zPos = 0;

        for (int lineRendererIndex = 0; lineRendererIndex < lineRendererPositions.Length; lineRendererIndex++)
        {
            zPos = lineRendererIndex * ShrinkFactor3D;
            var waveformIndex = startSampleIndex + lineRendererIndex;

            if (waveformIndex < 0 || waveformIndex >= waveformData.Length)
            {
                lineRendererPositions[lineRendererIndex] = new Vector3(0, 0.001f, zPos);
                continue;
            }
            lineRendererPositions[lineRendererIndex] = new(waveformData[waveformIndex] * Amplitude3D, 0.01f, zPos);
        }

        lineRendererMain.SetPositions(lineRendererPositions);

        // mirror all x positions of every point
        lineRendererPositions = Array.ConvertAll(lineRendererPositions, pos => new Vector3(-pos.x, pos.y, pos.z));

        lineRendererMirror.SetPositions(lineRendererPositions);

        UpdateWaveformData();
    }


    protected override int samplesPerBoundary => (int)Mathf.Round(highway.localScale.z / (ShrinkFactor3D));
    protected override int strikeSamplePoint => (int)Math.Ceiling(samplesPerBoundary * Strikeline3D.instance.GetStrikelineProportion());

    bool isVisible = true;
    public override void SetWaveformVisibility(bool isVisible)
    {
        this.isVisible = isVisible;
    }
}