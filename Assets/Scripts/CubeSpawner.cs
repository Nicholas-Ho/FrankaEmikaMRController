using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CapsuleSpawner : MonoBehaviour
{
    public GameObject prefab;
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (OVRInput.GetDown(OVRInput.RawButton.A)) {
            Instantiate(
                prefab,
                OVRInput.GetLocalControllerPosition(OVRInput.Controller.RTouch),
                OVRInput.GetLocalControllerRotation(OVRInput.Controller.RTouch)
            );
        }
    }
}
