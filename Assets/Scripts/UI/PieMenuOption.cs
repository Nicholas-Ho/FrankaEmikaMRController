using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class PieMenuOption : MonoBehaviour
{
    [HideInInspector]
    private EventTrigger.TriggerEvent callback;

    public void SetCallback(EventTrigger.TriggerEvent _callback)
    {
        callback = _callback;
    }

    public void ExecuteCallback()
    {
        BaseEventData eventData = new BaseEventData(EventSystem.current);
        callback.Invoke(eventData);
    }
}
