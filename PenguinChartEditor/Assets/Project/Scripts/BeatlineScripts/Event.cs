using UnityEngine;

public abstract class Event : MonoBehaviour
{
    public int Tick { get; set; }
    
    public bool Selected
    {
        get
        {
            return _selected;
        }
        set
        {
            SelectionOverlay.SetActive(value);
            _selected = value;
        }
    }
    bool _selected = false;

    [field: SerializeField] public GameObject SelectionOverlay { get; set; }
    public bool Visible
    {
        get
        {
            return gameObject.activeInHierarchy;
        }
        set
        {
            gameObject.SetActive(value);
        }
    }
}