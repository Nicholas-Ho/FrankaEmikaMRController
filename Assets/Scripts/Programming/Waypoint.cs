using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Waypoint : MonoBehaviour
{
    public bool staticWaypoint = false;
    private GameObject textObject;
    private Transform grabTransform;
    private TextMeshPro tmp;
    private string text = "";
    private int index = -1;

    void Start()
    {
        textObject = transform.Find("TextContainer").gameObject;
        tmp = textObject.GetComponentInChildren<TextMeshPro>();
        if (!staticWaypoint) grabTransform = transform.Find("GrabbableSphere");
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
        index = i;
        if (i == 0) {
            text = "Start";
            return ;
        }
        text = i.ToString();
    }

    public int GetIndex() { return index; }
}
