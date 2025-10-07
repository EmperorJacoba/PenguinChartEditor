using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BPMPooler : MonoBehaviour
{
    [SerializeField] GameObject bpmPrefab;
    [SerializeField] GameObject canvas;

    public static BPMPooler instance;

    List<BPM> labels = new();

    void Awake()
    {
        instance = this;
    }

    void CreateNew()
    {
        GameObject tmp = Instantiate(bpmPrefab, canvas.transform);
        labels.Add(tmp.GetComponent<BPM>());

        labels[^1].Visible = false;
    }

    public BPM GetBPM(int index)
    {
        while (labels.Count <= index)
        {
            CreateNew();
        }
        BPM bpm = labels[index];
        bpm.Visible = true;
        return bpm;
    }

    public void DeactivateUnused(int lastIndex)
    {
        // Since beatlines are accessed and displayed sequentially, disable all
        // beatlines from the last beatline accessed until hitting an already inactive beatline.
        while (true)
        {
            try
            {
                if (labels[lastIndex].Visible)
                {
                    labels[lastIndex].Visible = false;
                }
                else break;
            }
            catch
            {
                break;
            }
            lastIndex++;
        }
    }

    public IEnumerator DestructionTimer(BPM bpm)
    {
        yield return new WaitForSeconds(5.0f);

        instance.labels.Remove(bpm);

        Destroy(bpm.gameObject);
    }
}