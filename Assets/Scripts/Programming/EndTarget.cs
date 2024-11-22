using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Geometry;
using UrdfPositioning;
using System;

public class EndTarget : MonoBehaviour
{
    ROSConnection ros;

    public string subTopic = "/cartesian_impedance_example_controller/robot_current_pose";
    public string pubTopic = "/cartesian_impedance_example_controller/equilibrium_pose";

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
            PoseStampedMsg msg = new PoseStampedMsg();
            Vector3 robotOriginTransform = UrdfPositioner.TransformToRobotSpace(transform.position);
            msg.pose.position.x = robotOriginTransform.z;
            msg.pose.position.y = -robotOriginTransform.x;
            msg.pose.position.z = robotOriginTransform.y;
            msg.pose.orientation = orientation;  // Not dealing with orientation for now
            msg.header.frame_id = "panda_link0";

            TimeSpan t = DateTime.UtcNow - new DateTime(1970, 1, 1);
            msg.header.stamp.sec = (uint)t.TotalSeconds;

            ros.Publish(pubTopic, msg);
        }
    }

    void Initialise()
    {
        // Initialise ROS
        ros = ROSConnection.GetOrCreateInstance();
        ros.Subscribe<PoseStampedMsg>(subTopic, SubscribeCallback);
        ros.RegisterPublisher<PoseStampedMsg>(pubTopic);
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
}
