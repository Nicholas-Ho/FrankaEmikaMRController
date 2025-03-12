using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class Waypoint : MonoBehaviour
{
    private Transform textTransform;
    private Transform deleteButtonTransform;
    private Transform headTransform;
    private Transform centreTransform;
    [HideInInspector]
    private ProximityButton deleteButton;
    public GrabFreeTransformerTracking transformer { get; private set; }
    private TextMeshPro tmp;
    private string text = "";
    private int index = -1;
    private bool initialised = false;

    void Start()
    {
        if (!initialised) Initialise();
    }

    void Initialise()
    {
        centreTransform = transform.Find("WaypointCentre");
        
        tmp = transform.GetComponentInChildren<TextMeshPro>();
        textTransform = tmp.transform.parent;

        transformer = GetComponentInChildren<GrabFreeTransformerTracking>();

        deleteButton = GetComponentInChildren<ProximityButton>();
        deleteButtonTransform = deleteButton.transform;

        headTransform = GameObject.FindWithTag("MainCamera").transform;

        initialised = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (!initialised) Initialise();

        // Reset centre transform rotation
        centreTransform.rotation = Quaternion.identity;

        // Look at camera
        textTransform.transform.LookAt(headTransform.position);
        deleteButtonTransform.LookAt(headTransform.position);

        // Set text
        if (tmp.text != text) tmp.SetText(text);
    }

    public void SetIndex(int i)
    {
        if (!initialised) Initialise();
        index = i;
        if (i == 0) {
            text = "Start";
            return ;
        }
        text = i.ToString();
    }
    
    public int GetIndex() { return index; }

    public void SetButtonCallback(UnityAction<BaseEventData> action)
    {
        if (!initialised) Initialise();
        deleteButton.callback.AddListener(action);
    }

    public void SetMoveCallback(UnityAction<BaseEventData> action)
    {
        if (!initialised) Initialise();
        transformer = GetComponentInChildren<GrabFreeTransformerTracking>();
        transformer.AddCallbackListener(action);
    }

    public TransformData GetWaypointTransform()
    {
        return new TransformData(transform.position, transform.rotation);
    }

    public void SetWaypointTransform(Vector3 position, Quaternion rotation)
    {
        transform.SetPositionAndRotation(position, rotation);
    }

    public void ResetButtonState() { deleteButton.ResetState(); }
}
