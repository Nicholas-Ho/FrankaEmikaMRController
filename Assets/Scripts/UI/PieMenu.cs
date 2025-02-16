using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.EventSystems;

public enum HandSide
{
    Left,
    Right
}

public class PieMenu : MonoBehaviour
{
    // Variable list of options
    public List<PieMenuOptionInfo> pieMenuOptions = new List<PieMenuOptionInfo>();

    // Right or left hand?
    public HandSide handSide = HandSide.Left;

    // Line divider
    public GameObject lineDividerPrefab;

    // TextMeshPro GameObject for option text
    public GameObject optionTextPrefab;

    // Offset of text from centre
    public float optionOffset;

    // Radius to cancel menu
    public float cancelRadius;

    // Option hover colour
    public Color optionColour = new Color();

    // Cancel hover colour
    public Color cancelColour = new Color();

    // List of option GameObjects
    private List<GameObject> optionObjects = new List<GameObject>();

    // Reference to hand
    private OVRHand hand;

    // Reference to hand skeleton. For extracting index finger location
    private OVRSkeleton handSkeleton;

    // Reference to cancel button GameObject
    private GameObject cancelButton;

    // Visibility of object
    private bool visible = false;

    // Start is called before the first frame update
    void Start()
    {
        // Get reference to cancel option
        foreach (Transform child in transform) {
            if (child.gameObject.name == "Cancel Button") {
                cancelButton = child.gameObject;
            }
        }

        // Build menu
        Quaternion quaternion = new Quaternion();
        float degIncrement = 360 / (2 * pieMenuOptions.Count());
        for (int i=0; i<(2*pieMenuOptions.Count()); i++) {
            Vector3 position = new Vector3(0, optionOffset, 0);
            position = quaternion * position;
            if (i % 2 == 0) {
                // Render option
                GameObject option = Instantiate(optionTextPrefab, position, Quaternion.identity);
                option.transform.SetParent(transform, false);
                
                option.GetComponentInChildren<TextMeshPro>().SetText(pieMenuOptions[i/2].name);
                option.GetComponent<PieMenuOption>().SetCallback(pieMenuOptions[i/2].callback);
                optionObjects.Add(option);
            } else {
                // Render line
                GameObject line = Instantiate(lineDividerPrefab, position, quaternion);
                line.transform.SetParent(transform, false);
            }
            quaternion.eulerAngles = new Vector3(0, 0, quaternion.eulerAngles.z + degIncrement);
        }

        // Hidden
        SetVisibility(false);

        // Get hand reference. Name used is default from building block
        string handSideString = handSide == HandSide.Left ? "left" : "right";
        GameObject rightHandObject = GameObject.Find(
            String.Format("[BuildingBlock] Hand Tracking {0}", handSideString));
        hand = rightHandObject.GetComponent<OVRHand>();
        handSkeleton = rightHandObject.GetComponent<OVRSkeleton>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!hand.IsTracked) return ;
        bool pinching = hand.GetFingerIsPinching(OVRHand.HandFinger.Index);
        if (!visible) {
            if (pinching) {
                // Set position to hand
                Vector3 headPos = GameObject.FindWithTag("MainCamera").transform.position;
                Vector3 rightIndexPos = HandUtilities.GetIndexTipTransform(handSkeleton).position;
                transform.position = rightIndexPos;
                transform.LookAt(headPos);

                SetVisibility(true);
            }
        } else {
            // Reset all option colours
            cancelButton.GetComponent<SpriteRenderer>().color = Color.white;
            foreach (GameObject option in optionObjects) {
                option.GetComponentInChildren<TextMeshPro>().color = Color.white;
            }

            // Update hover selection
            int currOption = GetHoverOption();

            // Recolour hover
            if (currOption == -1) {
                // Cancel option
                cancelButton.GetComponent<SpriteRenderer>().color = cancelColour;
            } else {
                // Option colours
                optionObjects[currOption].GetComponentInChildren<TextMeshPro>().color = optionColour;
            }

            if (!pinching) {
                // Select option
                if (currOption != -1) optionObjects[currOption].GetComponent<PieMenuOption>().ExecuteCallback();
                SetVisibility(false);
            }
        }
    }

    private int GetHoverOption()
    {
        Vector3 rightIndexPos = HandUtilities.GetIndexTipTransform(handSkeleton).position;
        Vector3 relativePos = HandUtilities.GetRelativePlaneProjected(rightIndexPos, transform.forward, transform.position);
        
        // Check for cancel
        if (relativePos.magnitude <= cancelRadius) return -1;

        // Check nearest option
        int nearest = -1;
        float leastDist = float.PositiveInfinity;
        float distance;
        for (int i=0; i<optionObjects.Count; i++) {
            distance = Vector3.Distance(rightIndexPos, optionObjects[i].transform.position);
            if (distance < leastDist) {
                nearest = i;
                leastDist = distance;
            }
        }
        return nearest;
    }

    private void SetVisibility(bool _visible)
    {
        visible = _visible;
        foreach (Transform child in transform) {
            child.gameObject.SetActive(_visible);
        }
    }
}


[System.Serializable]
public struct PieMenuOptionInfo
{
    public string name;
    public EventTrigger.TriggerEvent callback;
}