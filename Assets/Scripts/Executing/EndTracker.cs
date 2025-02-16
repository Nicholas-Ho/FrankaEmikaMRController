using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Geometry;
using UrdfPositioning;
using System;

public class EndTracker : MonoBehaviour
{
    ROSConnection ros;

    public string subTopic = "/robot_current_pose";

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
        ros.Subscribe<PoseStampedMsg>(subTopic, SubscribeCallback);
        initialised = true;
    }

    void SubscribeCallback(PoseStampedMsg msg)
    {
        Vector3 position = new Vector3(
            (float)-msg.pose.position.y,  // Note: Swapped around x and y
            (float)msg.pose.position.z,
            (float)msg.pose.position.x);
        transform.position = UrdfPositioner.TransformFromRobotSpace(position);
    }
}
