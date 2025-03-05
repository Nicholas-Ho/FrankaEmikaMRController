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

    public Color tint;
    public float tintBlend = 0.3f;

    private bool initialised = false;
    public bool connected { get; private set; } = false;

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
        connected = true;
    }

    // public void OnDestroy()
    // {
    //     ros.Unsubscribe(subTopic);
    // }
}
