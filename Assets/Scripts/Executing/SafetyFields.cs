using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RosMessageTypes.FrankaExampleControllers;
using Unity.Robotics.ROSTCPConnector;
using Unity.Robotics.ROSTCPConnector.ROSGeometry;
using UrdfPositioning;

public class SafetyFields : MonoBehaviour
{
    ROSConnection ros;
    public string pubTopic = "/safety_fields";

    public GameObject leftHandObject;
    public GameObject rightHandObject;
    public Transform endTracker;

    public float bodyFieldRadius = 0.5f;
    public float handFieldRadius = 0.1f;
    public float bodyFieldStrengthTarget = 200;
    public float handFieldStrengthTarget = 300;
    public float parameterRampTime = 0.1f;

    private Transform headTransform;
    private OVRHand leftHand;
    private OVRHand rightHand;
    private OVRSkeleton leftHandSkeleton;
    private OVRSkeleton rightHandSkeleton;

    private Vector3 bodyFieldCentre = new();
    private Vector3 leftHandFieldCentre = new();
    private Vector3 rightHandFieldCentre = new();
    private float bodyFieldStrength = 0;
    private float handFieldStrength = 0;
    private float bodyStrengthRamp;
    private float handStrengthRamp;

    private bool initialised = false;
    private bool activeFields = false;

    // Start is called before the first frame update
    void Start()
    {
        Initialise();

        headTransform = GameObject.FindWithTag("MainCamera").transform;
        leftHand = leftHandObject.GetComponent<OVRHand>();
        leftHandSkeleton = leftHandObject.GetComponent<OVRSkeleton>();
        rightHand = rightHandObject.GetComponent<OVRHand>();
        rightHandSkeleton = rightHandObject.GetComponent<OVRSkeleton>();

        bodyFieldCentre = new Vector3(
            headTransform.position.x,
            Math.Min(headTransform.position.y, endTracker.position.y),
            headTransform.position.z
        );

        bodyStrengthRamp = bodyFieldStrengthTarget / parameterRampTime;
        handStrengthRamp = handFieldStrengthTarget / parameterRampTime;
    }

    // Update is called once per frame
    void Update()
    {
        if (!initialised) Initialise();

        // Update positions
        bodyFieldCentre = new Vector3(
            headTransform.position.x,
            Math.Min(headTransform.position.y, endTracker.position.y),
            headTransform.position.z
        );

        if (leftHand.IsActive() && leftHand.IsTracked) 
            leftHandFieldCentre = HandUtilities.GetHandTransform(leftHandSkeleton).position;
        if (rightHand.IsActive() && rightHand.IsTracked) 
            rightHandFieldCentre = HandUtilities.GetHandTransform(rightHandSkeleton).position;

        // While switching, ramp transitions
        if (activeFields) {
            bodyFieldStrength = Math.Min(
                bodyFieldStrength + Time.deltaTime * bodyStrengthRamp,
                bodyFieldStrengthTarget);
            handFieldStrength = Math.Min(
                handFieldStrength + Time.deltaTime * handStrengthRamp,
                handFieldStrengthTarget);
        } else {
            bodyFieldStrength = Math.Max(
                bodyFieldStrength - Time.deltaTime * bodyStrengthRamp,
                0);
            handFieldStrength = Math.Max(
                handFieldStrength - Time.deltaTime * handStrengthRamp,
                0);
        }

        // Publish
        SafetyRepulsiveFieldsMsg msg = new();
        msg.body.centre = URDFPointCloudManager.VectorToRobotSpace(bodyFieldCentre).To<FLU>();
        msg.body.active_radius = bodyFieldRadius;
        msg.body.strength = bodyFieldStrength;
        msg.left_hand.centre = URDFPointCloudManager.VectorToRobotSpace(leftHandFieldCentre).To<FLU>();
        msg.left_hand.active_radius = handFieldRadius;
        msg.left_hand.strength = handFieldStrength;
        msg.right_hand.centre = URDFPointCloudManager.VectorToRobotSpace(rightHandFieldCentre).To<FLU>();
        msg.right_hand.active_radius = handFieldRadius;
        msg.right_hand.strength = handFieldStrength;

        ros.Publish(pubTopic, msg);
    }

    void Initialise()
    {
        // Initialise ROS
        ros = ROSConnection.GetOrCreateInstance();
        ros.RegisterPublisher<SafetyRepulsiveFieldsMsg>(pubTopic);
        initialised = true;
    }

    public void SetFieldsActive(bool activate) => activeFields = activate;
}
