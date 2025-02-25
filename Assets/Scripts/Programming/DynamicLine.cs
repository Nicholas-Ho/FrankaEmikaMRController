using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class DynamicLine : MonoBehaviour
{
    [HideInInspector]
    public Transform ref1, ref2;
    private Transform insertButtonTransform;
    private ProximityButton insertButtonComponent;
    private LineRenderer lineRenderer;

    // Start is called before the first frame update
    void Start()
    {
        lineRenderer = gameObject.GetComponent<LineRenderer>();
        insertButtonTransform = transform.Find("InsertButton");
        insertButtonComponent = insertButtonTransform.GetComponent<ProximityButton>();
        SetPositions();
    }

    // Update is called once per frame
    void Update()
    {
        SetPositions();
    }

    private void SetPositions()
    {
        lineRenderer.SetPosition(0, ref1.position);
        lineRenderer.SetPosition(1, ref2.position);
        insertButtonTransform.position = (ref1.position + ref2.position) / 2;
    }

    public void SetButtonCallback(UnityAction<BaseEventData> action)
    {
        insertButtonTransform = transform.Find("InsertButton");
        insertButtonComponent = insertButtonTransform.GetComponent<ProximityButton>();
        insertButtonComponent.callback.AddListener(action);
    }

    public void ResetButtonState() { insertButtonComponent.ResetState(); }

    public Vector3 GetMidpoint() { return insertButtonTransform.position; }
}
