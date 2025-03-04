using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class Waypoint : MonoBehaviour
{
    public bool staticWaypoint = false;
    private GameObject textObject;
    [HideInInspector]
    public Transform grabTransform { get; private set; }
    private ProximityButton deleteButton;
    [HideInInspector]
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
        textObject = transform.Find("TextContainer").gameObject;
        tmp = textObject.GetComponentInChildren<TextMeshPro>();
        if (!staticWaypoint) {
            grabTransform = transform.Find("Grabbable");
            transformer = GetComponentInChildren<GrabFreeTransformerTracking>();
            deleteButton = GetComponentInChildren<ProximityButton>();
        }
        initialised = true;
    }

    // Update is called once per frame
    void Update()
    {
        // Update transform
        if (!staticWaypoint) {
            transform.position = grabTransform.position;
            grabTransform.localPosition = Vector3.zero;
        }

        // Look at camera
        Vector3 headPos = GameObject.FindWithTag("MainCamera").transform.position;
        textObject.transform.LookAt(headPos);

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

    public void SetWaypointTransform(Vector3 position, Quaternion rotation)
    {
        if (!initialised) Initialise();
        transform.position = position;
        if (!staticWaypoint) grabTransform.rotation = rotation;
    }

    public void ResetButtonState() { deleteButton.ResetState(); }
}
