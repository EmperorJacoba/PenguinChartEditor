using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class InstrumentSpawningManager : MonoBehaviour
{
    [SerializeField] GameObject GameInstrumentPackage;
    LinkedList<GameInstrument> activeGameInstruments;

    public void SpawnInstrumentOnRight(HeaderType instrumentType)
    {
        var activeInstrument = CreateNewInstrument(instrumentType);
        activeInstrument.transform.position += Vector3.right * 20;
        activeGameInstruments.AddLast(activeInstrument);
    }

    public void SpawnInstrumentOnLeft(HeaderType instrumentType)
    {
        var activeInstrument = CreateNewInstrument(instrumentType);
        activeInstrument.transform.position += Vector3.left * 20;
        activeGameInstruments.AddFirst(activeInstrument);
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
        return gameInstrument;
    }
}
