using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
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
        transform.SetPositionAndRotation(
            UrdfPositioner.VectorFromRobotSpace(msg.pose.position.From<FLU>()),
            UrdfPositioner.RotateFromRobotSpace(msg.pose.orientation.From<FLU>()));
    }

    // public void OnDestroy()
    // {
    //     ros.Unsubscribe(subTopic);
    // }
}
