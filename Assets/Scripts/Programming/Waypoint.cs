using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class Waypoint : MonoBehaviour
{
    private GameObject textObject;
    private string text;

    void Start()
    {
        textObject = transform.Find("Text").gameObject;
    }

    // Update is called once per frame
    void Update()
    {
        // Look at camera
        Vector3 headPos = GameObject.FindWithTag("MainCamera").transform.position;
        textObject.transform.LookAt(headPos);

        // Update text
        gameObject.GetComponentInChildren<TextMeshPro>().SetText(text);
    }

    public void SetIndex(int i)
    {
        if (i == 0) {
            text = "Start";
            return ;
        }
        text = i.ToString();
    }
}
