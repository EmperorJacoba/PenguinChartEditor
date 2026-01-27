using System.Collections.Generic;
using UnityEngine;

public class InstrumentSpawningManager : MonoBehaviour
{
    public static InstrumentSpawningManager instance;
    [SerializeField] private GameObject GameInstrumentPackage;
    private LinkedList<GameInstrument> activeGameInstruments = new();

    public GameInstrument leftmostTrack => activeGameInstruments.First.Value;
    public GameInstrument rightmostTrack => activeGameInstruments.Last.Value;

    [SerializeField] private FocusCameraButtons cameraController;
    [SerializeField] private InstrumentAddButton leftButton;
    [SerializeField] private InstrumentAddButton rightButton;

    private Vector3 BUTTON_Z_POSITION = (Vector3.forward * 25);
    private Vector3 rightSpawningOffset = Vector3.right * 20;
    private Vector3 leftSpawningOffset = Vector3.left * 20;

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

    public LinkedList<int> GetCurrentInstrumentOrdering()
    {
        LinkedList<int> outputLinkedList = new();
        LinkedListNode<GameInstrument> activeNode = activeGameInstruments.First;

        while (activeNode != null)
        {
            outputLinkedList.AddLast((int)activeNode.Value.instrumentID);
            activeNode = activeNode.Next;
        }

        return outputLinkedList;
    }

    public List<int> GetActiveInstrumentIDs()
    {
        var outputList = new List<int>(activeGameInstruments.Count);
        foreach (var instrument in activeGameInstruments)
        {
            outputList.Add((int)instrument.instrumentID);
        }
        return outputList;
    }

    public void SpawnInstrumentOnRight(HeaderType instrumentID)
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

        var activeInstrument = CreateNewInstrument(instrumentID);
        activeInstrument.transform.position = newInstrumentPos;
        activeGameInstruments.AddLast(activeInstrument);

        rightButton.transform.position += rightSpawningOffset;

        Chart.InPlaceRefresh();
    }

    public void SpawnInstrumentOnLeft(HeaderType instrumentID)
    {
        var activeInstrument = CreateNewInstrument(instrumentID);
        activeInstrument.transform.position = activeGameInstruments.First.Value.transform.position + leftSpawningOffset;
        activeGameInstruments.AddFirst(activeInstrument);

        leftButton.transform.position += leftSpawningOffset;

        Chart.InPlaceRefresh();
    }

    public void SwapInstrumentWithLeft(GameInstrument instrument)
    {
        var movingNode = activeGameInstruments.Find(instrument);

        if (movingNode == null)
        {
            Debug.LogWarning("Tried to find nonexistent GameInstrument object from spawning manager.");
            return;
        }

        var leftNode = movingNode.Previous;
        if (leftNode == null)
        {
            return;
        }

        var leftInstrument = leftNode.Value;

        instrument.transform.position += leftSpawningOffset;
        leftInstrument.transform.position += rightSpawningOffset;

        movingNode.Value = leftNode.Value;
        leftNode.Value = instrument;
    }

    public void SwapInstrumentWithRight(GameInstrument instrument)
    {
        var movingNode = activeGameInstruments.Find(instrument);

        if (movingNode == null)
        {
            Debug.LogWarning("Tried to find nonexistent GameInstrument object from spawning manager.");
            return;
        }

        var rightNode = movingNode.Next;
        if (rightNode == null)
        {
            return;
        }

        var rightInstrument = rightNode.Value;

        instrument.transform.position += rightSpawningOffset;
        rightInstrument.transform.position += leftSpawningOffset;

        movingNode.Value = rightNode.Value;
        rightNode.Value = instrument;
    }

    public void RemoveInstrument(GameInstrument instrument)
    {
        var removingNode = activeGameInstruments.Find(instrument);
        if (removingNode == null)
        {
            Debug.LogWarning("Tried to remove nonexistent GameInstrument object from spawning manager.");
            return;
        }

        int index = 0;
        LinkedListNode<GameInstrument> node = activeGameInstruments.First;
        while (removingNode != node)
        {
            if (node.Next != null)
            {
                node = node.Next;
                index++;
            }
            else break;
        }

        if (node.Previous != null)
        {
            node = node.Previous;
        }

        activeGameInstruments.Remove(removingNode);

        if (activeGameInstruments.Count == 0)
        {
            leftButton.transform.position = rightButton.transform.position = Vector3.zero + BUTTON_Z_POSITION;
            leftButton.gameObject.SetActive(false);

            cameraController.ResetCameraPosition();
        }
        else
        {
            if ((index + 1) / (float)(activeGameInstruments.Count + 1) > 0.5)
            {
                node = node.Next;
                while (node != null)
                {
                    node.Value.transform.position += leftSpawningOffset;
                    node = node.Next;
                }
                rightButton.transform.position += leftSpawningOffset;
            }
            else
            {
                var startIndex = 0;
                node = activeGameInstruments.First;
                while (startIndex < index)
                {
                    node.Value.transform.position += rightSpawningOffset;
                    node = node.Next;
                    startIndex++;
                }
                leftButton.transform.position += rightSpawningOffset;
            }
        }

        Chart.StarpowerInstrument.ClearLaneSelection(instrument.instrumentID);
        Destroy(instrument.gameObject);
        Chart.InPlaceRefresh();
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
