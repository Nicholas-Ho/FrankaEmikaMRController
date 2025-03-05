using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using RosMessageTypes.Geometry;
using RosMessageTypes.FrankaExampleControllers;
using UrdfPositioning;
using System;

public class EndTarget : MonoBehaviour
{
    ROSConnection ros;

    public string subTopic = "/robot_current_pose";
    public string pubTopic = "/target_data";
    public float springStiffness = 100;
    public float rotationalStiffness = 30;
    public Color tint;
    public float tintBlend = 0.3f;

    private bool initialised = false;
    private bool initialPositionSet = false;

    // Start is called before the first frame update
    void Start()
    {
        Initialise();

        // Apply colour tint
        foreach (Renderer renderer in GetComponentsInChildren<Renderer>()) {
            renderer.material.color = new Color(
                renderer.material.color.r * (1-tintBlend) + tint.r * tintBlend,
                renderer.material.color.g * (1-tintBlend) + tint.g * tintBlend,
                renderer.material.color.b * (1-tintBlend) + tint.b * tintBlend,
                renderer.material.color.a * (1-tintBlend) + tint.a * tintBlend
            );
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (!initialised) Initialise();
        if (initialised && initialPositionSet) {
            TargetPoseMsg msg = new();
            msg.pose.position = UrdfPositioner.VectorToRobotSpace(transform.position).To<FLU>();
            msg.pose.orientation = UrdfPositioner.RotateToRobotSpace(transform.rotation).To<FLU>();
            msg.cartesian_stiffness = springStiffness;
            msg.rotational_stiffness = rotationalStiffness;

            ros.Publish(pubTopic, msg);
        }
    }

    void Initialise()
    {
        // Initialise ROS
        ros = ROSConnection.GetOrCreateInstance();
        ros.Subscribe<PoseStampedMsg>(subTopic, SubscribeCallback);
        ros.RegisterPublisher<TargetPoseMsg>(pubTopic);
        initialised = true;
    }

    void SubscribeCallback(PoseStampedMsg msg)
    {
        if (!initialPositionSet) {
            transform.SetPositionAndRotation(
                UrdfPositioner.VectorFromRobotSpace(msg.pose.position.From<FLU>()),
                UrdfPositioner.RotateFromRobotSpace(msg.pose.orientation.From<FLU>()));
            initialPositionSet = true;
        }
    }

    // public void OnDestroy()
    // {
    //     ros.Unsubscribe(subTopic);
    // }

    public bool IsReady() => initialPositionSet;
    public void Reset() => initialPositionSet = false;
}
