using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Geometry;
using RosMessageTypes.Std;
using UrdfPositioning;
using System;

public class EndTarget : MonoBehaviour
{
    ROSConnection ros;

    public string subTopic = "/robot_current_pose";
    public string pubTopic = "/target_data";
    public float springStiffness = 100;
    public float damperStrength = 5;

    QuaternionMsg orientation;
    private bool initialised = false;
    private bool initialPositionSet = false;

    // Start is called before the first frame update
    void Start()
    {
        Initialise();
    }

    // Update is called once per frame
    void Update()
    {
        if (!initialised) Initialise();
        if (initialised && initialPositionSet) {
            Vector3 robotOriginTransform = UrdfPositioner.TransformToRobotSpace(transform.position);
            Float64MultiArrayMsg msg = new Float64MultiArrayMsg();
            msg.data = new double[]{
                robotOriginTransform.z,
                -robotOriginTransform.x,
                robotOriginTransform.y,
                springStiffness,
                damperStrength
            };

            ros.Publish(pubTopic, msg);
        }
    }

    void Initialise()
    {
        // Initialise ROS
        ros = ROSConnection.GetOrCreateInstance();
        ros.Subscribe<PoseStampedMsg>(subTopic, SubscribeCallback);
        ros.RegisterPublisher<Float64MultiArrayMsg>(pubTopic);
        initialised = true;
    }

    void SubscribeCallback(PoseStampedMsg msg)
    {
        if (!initialPositionSet) {
            Vector3 position = new Vector3(
                (float)-msg.pose.position.y,  // Note: Swapped around x and y
                (float)msg.pose.position.z,
                (float)msg.pose.position.x);
            transform.position = UrdfPositioner.TransformFromRobotSpace(position);
            orientation = msg.pose.orientation;
            initialPositionSet = true;
        }
    }

    // public void OnDestroy()
    // {
    //     ros.Unsubscribe(subTopic);
    // }

    public bool IsReady()
    {
        return initialPositionSet;
    }
}
