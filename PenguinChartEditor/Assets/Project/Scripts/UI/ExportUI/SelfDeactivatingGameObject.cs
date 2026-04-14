using System;
using UnityEngine;

public class SelfDeactivatingGameObject : MonoBehaviour
{
    // Unity refuses to run the Start() functions properly if you try to quickly enable them and disable them upon a scene start.
    // Start() needs to run in a lot of objects in the ExportScene to avoid a bunch of NullReferenceExceptions in the case
    // that the user does not access certain tabs. So, the hidden tab content has to be active pre-start and then 
    // quickly disabled once playing. 
    private void LateUpdate()
    {
        // This will happen after all the Start() functions are called, because it's *Late*Update(). Once it's disabled,
        // it's not running again. 
        gameObject.SetActive(false);
        Destroy(this);
    }
}