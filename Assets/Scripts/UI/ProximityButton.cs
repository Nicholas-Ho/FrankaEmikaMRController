using UnityEngine.UI;
using UnityEngine;
using UnityEngine.EventSystems;
using Oculus.Interaction.Input;
using System;

enum ProximityButtonState {
    Normal,
    Hover,
    Pressed,
    Disabled
}

public class ProximityButton : MonoBehaviour
{
    public float hoverDistance = 0.1f;
    public float pressDistance = 0.05f;
    public float leaveDistance = 0.1f;
    public float buttonBounds = 0.01f;
    public float loadTime = 1.5f;  // seconds

    public Color defaultColour;
    public Color hoverColour;
    public Color pressColour;

    public SpriteRenderer buttonIcon;
    public Image radialLoad;

    public EventTrigger.TriggerEvent callback;

    private float progress = 0;

    // Reference to hand skeletons. For extracting index finger location
    private OVRSkeleton leftHandSkeleton, rightHandSkeleton;
    private OVRHand leftHand, rightHand;
    private ProximityButtonState state = ProximityButtonState.Normal;

    // Start is called before the first frame update
    void Start()
    {
        // Get skeleton references. Names used are default from building block
        GameObject rightHandObject = GameObject.Find("[BuildingBlock] Hand Tracking right");
        GameObject leftHandObject = GameObject.Find("[BuildingBlock] Hand Tracking left");
        leftHandSkeleton = rightHandObject.GetComponent<OVRSkeleton>();
        rightHandSkeleton = leftHandObject.GetComponent<OVRSkeleton>();
        leftHand = rightHandObject.GetComponent<OVRHand>();
        rightHand = leftHandObject.GetComponent<OVRHand>();
    }

    // Update is called once per frame
    void Update()
    {
        if (state == ProximityButtonState.Disabled) return ;

        // Determine relevant index finger transform
        Transform activeIndexTransform;
        if (!leftHand.IsTracked && !rightHand.IsTracked) {
            SetNormalState();
            return ;
        }
        if (leftHand.IsTracked && !rightHand.IsTracked) {
            activeIndexTransform = HandUtilities.GetIndexTipTransform(leftHandSkeleton);
        } else if (rightHand.IsTracked && !leftHand.IsTracked) {
            activeIndexTransform = HandUtilities.GetIndexTipTransform(rightHandSkeleton);
        } else {
            activeIndexTransform = GetActiveIndexTransform();
        }

        // If not within projected planar bounds, not pressing
        if (HandUtilities.GetRelativePlaneProjected(activeIndexTransform.position,
                                               transform.forward,
                                               transform.position).magnitude > buttonBounds) {
            SetNormalState();
            return ;
        }

        // Check proximity
        float projectedDist = Math.Abs(Vector3.Dot(activeIndexTransform.position - transform.position,
                                          transform.forward));
        if (projectedDist < pressDistance ||
            (state == ProximityButtonState.Pressed && projectedDist < leaveDistance)) {
                SetPressedState();
        } else if (projectedDist < hoverDistance) {
            SetHoverState();
        } else {
            SetNormalState();
        }

        if (progress >= 1) {
            state = ProximityButtonState.Disabled;
            BaseEventData eventData = new BaseEventData(EventSystem.current);
            callback.Invoke(eventData);
            progress = 0;
        }
    }

    private void SetNormalState()
    {
        if (state == ProximityButtonState.Normal) return ;
        progress = 0;
        buttonIcon.color = defaultColour;
        state = ProximityButtonState.Normal;
    }

    private void SetHoverState()
    {
        if (state == ProximityButtonState.Hover) return ;
        progress = 0;
        buttonIcon.color = hoverColour;
        state = ProximityButtonState.Hover;
    }

    private void SetPressedState()
    {
        progress += Time.deltaTime * (1 / loadTime);
        if (state == ProximityButtonState.Pressed) return ;
        buttonIcon.color = pressColour;
        state = ProximityButtonState.Pressed;
    }

    private Transform GetActiveIndexTransform()
    {
        Transform leftIndexTransform = HandUtilities.GetIndexTipTransform(leftHandSkeleton);
        Transform rightIndexTransform = HandUtilities.GetIndexTipTransform(rightHandSkeleton);
        float leftDist = (leftIndexTransform.position - transform.position).magnitude;
        float rightDist = (rightIndexTransform.position - transform.position).magnitude;

        return leftDist < rightDist ? leftIndexTransform : rightIndexTransform;
    }

    public float GetProgress() { return progress; }

    public void ResetState() { SetNormalState(); }
}
