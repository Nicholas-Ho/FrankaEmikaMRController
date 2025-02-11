using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.Geometry;
using UrdfPositioning;
using System;

public class EndTarget : MonoBehaviour
{
    ROSConnection ros;

    public string subTopic = "/vmc_controller/robot_current_pose";
    public string pubTopic = "/vmc_controller/equilibrium_pose";

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
        ros.RegisterPublisher<PoseStampedMsg>(pubTopic);
        initialised = true;
    }
}
