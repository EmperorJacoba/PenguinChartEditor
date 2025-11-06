using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IPoolable
{
    // needed for DeactivateUnused()
    bool Visible { get; set; }
    int Tick { get; }
    Coroutine destructionCoroutine { get; set; }
}

// Adapted from Unity's intro to object pooling
// https://learn.unity.com/tutorial/introduction-to-object-pooling

public abstract class Pooler<T> : MonoBehaviour where T : MonoBehaviour, IPoolable
{
    [SerializeField] protected GameObject objectPrefab;

    [Tooltip("The object that the pooled objects will be children of. Use the canvas with all other events on it for 2D applications (TempoMap)")]
    [SerializeField] protected GameObject parentObject;

    protected List<T> eventObjects = new();

    protected void CreateNew()
    {
        GameObject tmp = Instantiate(objectPrefab, parentObject.transform);
        eventObjects.Add(tmp.GetComponent<T>());
    }

    /// <summary>
    /// Get a specified pooled object from the collection of existing pooled objects.
    /// </summary>
    /// <param name="index">The target object number.</param>
    /// <returns>The requested object.</returns>
    public virtual T GetObject(int index)
    {
        while (eventObjects.Count <= index)
        {
            CreateNew();
        }
        T @object = eventObjects[index];

        if (@object.destructionCoroutine != null)
            StopCoroutine(@object.destructionCoroutine);

        @object.Visible = true;

        // initialize in lane!
        return @object;
    }

    /// <summary>
    /// Deactivate all active pooled objects past a specified index number.
    /// <para>Called after generating objects. Pass in the last generated object number to deactivate all unused objects past that object.</para>
    /// </summary>
    /// <param name="lastIndex">The object index to start deactivating from.</param>
    public void DeactivateUnused(int lastIndex)
    {
        for (int i = lastIndex; i < eventObjects.Count; i++)
        {
            eventObjects[i].Visible = false;
            eventObjects[i].destructionCoroutine = StartCoroutine(DestructionTimer(eventObjects[i]));
        }
    }

    /// <summary>
    /// Waits for five seconds, and then destroys the pooled object.
    /// <para> Used to avoid letting idle pooled objects take up resources
    /// in the background after a large hyperspeed change. </para>
    /// </summary>
    /// <param name="beatline">The target beatline to destroy after five seconds.</param>
    /// <returns></returns>
    IEnumerator DestructionTimer(T @object)
    {
        yield return new WaitForSeconds(5.0f);

        if (@object != null && !@object.Visible)
        {
            eventObjects.Remove(@object);

            if (@object != null) Destroy(@object.gameObject);
        }
    }
}