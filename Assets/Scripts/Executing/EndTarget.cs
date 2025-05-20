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
    public float cartesianStiffnessTarget = 200;
    public float rotationalStiffnessTarget = 60;
    public float parameterRampTime = 0.1f;  // in seconds
    public Color tint;
    public float tintBlend = 0.3f;

    private float cartesianStiffness = 0;
    private float rotationalStiffness = 0;
    private float cartesianStiffnessRamp;
    private float rotationalStiffnessRamp;

    private bool initialised = false;
    private bool initialPositionSet = false;

    private bool activeTarget = false;

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

        cartesianStiffnessRamp = cartesianStiffnessTarget / parameterRampTime;
        rotationalStiffnessRamp = rotationalStiffnessTarget / parameterRampTime;
    }

    // Update is called once per frame
    void Update()
    {
        if (!initialised) Initialise();
        if (initialised && initialPositionSet) {
            TargetPoseMsg msg = new();
            msg.pose.position = URDFPointCloudManager.VectorToRobotSpace(transform.position).To<FLU>();
            msg.pose.orientation = URDFPointCloudManager.RotateToRobotSpace(transform.rotation).To<FLU>();

            // While switching, ramp transitions
            if (activeTarget) {
                cartesianStiffness = Math.Min(
                    cartesianStiffness + Time.deltaTime * cartesianStiffnessRamp,
                    cartesianStiffnessTarget);
                rotationalStiffness = Math.Min(
                    rotationalStiffness + Time.deltaTime * rotationalStiffnessRamp,
                    rotationalStiffnessTarget);
            } else {
                cartesianStiffness = Math.Max(
                    cartesianStiffness - Time.deltaTime * cartesianStiffnessRamp,
                    0);
                rotationalStiffness = Math.Max(
                    rotationalStiffness - Time.deltaTime * rotationalStiffnessRamp,
                    0);
            }

            msg.cartesian_stiffness = cartesianStiffness;
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
        if (!initialPositionSet || !activeTarget) {
            transform.SetPositionAndRotation(
                URDFPointCloudManager.VectorFromRobotSpace(msg.pose.position.From<FLU>()),
                URDFPointCloudManager.RotateFromRobotSpace(msg.pose.orientation.From<FLU>()));
            initialPositionSet = true;
        }
    }

    public void SetTargetActive(bool activate)
    {
        activeTarget = activate;
        foreach (Transform child in transform) {
            child.gameObject.SetActive(activate);
        }
    }

    public bool IsReady() => initialPositionSet;
}
