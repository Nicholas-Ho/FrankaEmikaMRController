using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Std;
using UrdfPositioning;
using System;
using System.Collections.Generic;
using System.Linq;

public class RobotCartesianTracker : MonoBehaviour
{
    ROSConnection ros;

    public string subTopic = "/joint_cartesian_positions";
    public GameObject visualisationPrefab;
    public bool visualise = true;

    [HideInInspector]
    public List<Vector3> linkPositions = new();

    private List<GameObject> visualisationObjects = new();
    private bool initialised = false;

    // Start is called before the first frame update
    void Start()
    {
        Initialise();
    }

    // Update is called once per frame
    void Update()
    {
        if (!initialised) Initialise();
    }

    void Initialise()
    {
        // Initialise ROS
        ros = ROSConnection.GetOrCreateInstance();
        ros.Subscribe<Float64MultiArrayMsg>(subTopic, SubscribeCallback);
        initialised = true;
    }

    void SubscribeCallback(Float64MultiArrayMsg msg)
    {
        if (linkPositions.Count == 0) {
            while (linkPositions.Count < (msg.data.Count() / 3)) {
                linkPositions.Add(Vector3.zero);
                if (visualise) {
                    GameObject visObj = Instantiate(visualisationPrefab);
                    visObj.transform.SetParent(transform);
                    visualisationObjects.Add(visObj);
                }
            }
        }
        for (int i=0; i<(msg.data.Count() / 3); i++) {
            Vector3 position = new(
                (float)-msg.data[i*3+1],  // -y
                (float)msg.data[i*3+2],   // z
                (float)msg.data[i*3]);    // x
            linkPositions[i] = UrdfPositioner.TransformFromRobotSpace(position);
            if (visualise) visualisationObjects[i].transform.position = linkPositions[i];
        }
    }

    // public void OnDestroy()
    // {
    //     ros.Unsubscribe(subTopic);
    // }
}
