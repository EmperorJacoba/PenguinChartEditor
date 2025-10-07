using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Pooler<T> : MonoBehaviour where T : MonoBehaviour
{
    [SerializeField] protected GameObject objectPrefab;
    [SerializeField] protected GameObject canvas;

    protected List<T> eventObjects = new();

    protected void CreateNew()
    {
        GameObject tmp = Instantiate(objectPrefab, canvas.transform);
        eventObjects.Add(tmp.GetComponent<T>());
    }

    public T GetObject(int index)
    {
        while (eventObjects.Count <= index)
        {
            CreateNew();
        }
        T @object = eventObjects[index];
        return @object;
    }

    public abstract void DeactivateUnused(int lastIndex);

    public IEnumerator DestructionTimer(T @object)
    {
        yield return new WaitForSeconds(5.0f);

        eventObjects.Remove(@object);

        Destroy(@object.gameObject);
    }
}