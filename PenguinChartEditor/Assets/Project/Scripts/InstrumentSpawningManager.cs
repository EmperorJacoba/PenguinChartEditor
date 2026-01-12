using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class InstrumentSpawningManager : MonoBehaviour
{
    public static InstrumentSpawningManager instance;
    [SerializeField] GameObject GameInstrumentPackage;
    LinkedList<GameInstrument> activeGameInstruments = new();

    public GameInstrument leftmostTrack => activeGameInstruments.First.Value;
    public GameInstrument rightmostTrack => activeGameInstruments.Last.Value;

    [SerializeField] InstrumentAddButton leftButton;
    [SerializeField] InstrumentAddButton rightButton;

    Vector3 BUTTON_Z_POSITION = (Vector3.forward * 25);
    Vector3 rightSpawningOffset = Vector3.right * 20;
    Vector3 leftSpawningOffset = Vector3.left * 20;

    public float OutOfBoundsPosLeft => leftmostTrack.transform.position.x + leftSpawningOffset.x;
    public float OutOfBoundsPosRight => rightmostTrack.transform.position.x + rightSpawningOffset.x;


    private void Awake()
    {
        instance = this;
    }
    private void OnDestroy()
    {
        instance = null;
    }

    public void SpawnInstrumentOnRight(HeaderType instrumentType)
    {
        Vector3 newInstrumentPos;
        if (activeGameInstruments.Count == 0)
        {
            newInstrumentPos = Vector3.zero;
            leftButton.gameObject.SetActive(true);
            leftButton.transform.position = (Vector3.left * 20) + BUTTON_Z_POSITION;
        }
        else
        {
            newInstrumentPos = activeGameInstruments.Last.Value.transform.position + rightSpawningOffset;
        }

        var activeInstrument = CreateNewInstrument(instrumentType);
        activeInstrument.transform.position = newInstrumentPos;
        activeGameInstruments.AddLast(activeInstrument);

        rightButton.transform.position += rightSpawningOffset;

        Chart.InPlaceRefresh();
    }

    public void SpawnInstrumentOnLeft(HeaderType instrumentType)
    {
        var activeInstrument = CreateNewInstrument(instrumentType);
        activeInstrument.transform.position = activeGameInstruments.First.Value.transform.position + leftSpawningOffset;
        activeGameInstruments.AddFirst(activeInstrument);

        leftButton.transform.position += leftSpawningOffset;

        Chart.InPlaceRefresh();
    }

    public void RemoveInstrument(HeaderType instrumentType)
    {
        var targetInstrument = activeGameInstruments.Where(x => x.instrumentID == instrumentType).ToList()[0];
        Destroy(targetInstrument);
        activeGameInstruments.Remove(targetInstrument);
    }

    private GameInstrument CreateNewInstrument(HeaderType instrumentType)
    {
        GameInstrumentPackage.SetActive(false);
        var spawnedInstrument = Instantiate(GameInstrumentPackage);
        var gameInstrument = spawnedInstrument.GetComponent<GameInstrument>();
        gameInstrument.instrumentID = instrumentType;
        spawnedInstrument.SetActive(true);
        return gameInstrument;
    }
}
