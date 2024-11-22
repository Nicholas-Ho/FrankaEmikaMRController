using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using Unity.Robotics.ROSTCPConnector;
using RosMessageTypes.FrankaExampleControllers;

public class DigitalTwinManager : MonoBehaviour
{
    ROSConnection ros;

    public string topic = "/cartesian_impedance_example_controller/joint_positions";
    public int numberOfJoints = 7;
    
    private float[] positions;
    private bool initialised = false;
    private JointPositionState jointSetter;

    // Start is called before the first frame update
    void Start()
    {
        Initialise();
    }

    // Update is called once per frame
    void Update()
    {
        if (!initialised) Initialise();
        jointSetter.SetJointPositions(positions);
    }

    void Initialise()
    {
        // Find JointPositionState component
        jointSetter = gameObject.GetComponent<JointPositionState>();

        // Initialise positions array
        positions = new float[numberOfJoints];

        // Initialise ROS
        ros = ROSConnection.GetOrCreateInstance();
        ros.Subscribe<JointPositionsMsg>(topic, SubscribeCallback);

        initialised = true;
    }

    public void SubscribeCallback(JointPositionsMsg msg)
    {
        for (int i=0; i<numberOfJoints; i++) {
            positions[i] = RadiansToDegrees(msg.positions[i]);
        }
    }

    public float DegreesToRadians(float degrees)
    {
        return (float)Math.PI * degrees / 180;
    }

    public float RadiansToDegrees(double radians)
    {
        return (float)(180 * radians / Math.PI);
    }
}
