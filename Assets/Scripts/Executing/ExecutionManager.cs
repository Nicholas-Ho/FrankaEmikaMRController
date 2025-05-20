using System.Linq;
using TMPro;
using UnityEngine;
public class ExecutionManager : MonoBehaviour
{
    public WalkthroughManager walkthroughManager;
    public SafetyFields safetyFieldsManager;
    public GameObject endTracker;
    public GameObject endTarget;
    public GameObject spring;
    public GameObject pieMenu;  // For setting text
    public float speed = 0.1f; // per second
    public float waitDistanceThreshold = 0.2f;
    public float resumeDistanceThreshold = 0.1f;
    public float waypointReachedThreshold = 0.05f;
    private int activeIndex = 0;
    private bool waiting = false;
    private bool paused = false;

    private EndTarget endTargetComponent;
    private TextMeshPro[] tmps = {};

    // Start is called before the first frame update
    void Start()
    {
        activeIndex = 0;
        paused = true;
        endTargetComponent = endTarget.GetComponent<EndTarget>();
        endTargetComponent.SetTargetActive(false);
        spring.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (endTargetComponent.IsReady() && !paused) {
            if (waiting) {
                // If waiting and within start distance, resume
                if (Vector3.Distance(endTarget.transform.position, endTracker.transform.position)
                    < resumeDistanceThreshold) {
                        waiting = false;
                }
                return ;
            }

            // Currently executing
            if (Vector3.Distance(endTarget.transform.position, endTracker.transform.position)
                > waitDistanceThreshold) {
                    // If not waiting and distance between target and tracker exceeds threshold, wait
                    waiting = true;
                    return ;
            }

            // Check for unexpected finish (e.g. deletion of waypoints)
            if (activeIndex >= walkthroughManager.waypoints.Count) {
                CancelExecution();
                return ;
            }

            TransformData activeTransformData = walkthroughManager.waypoints[activeIndex].GetWaypointTransform();
            if (Vector3.Distance(endTracker.transform.position, activeTransformData.position)
                < waypointReachedThreshold) {
                    // If waypoint reached, move to next waypoint
                    activeIndex++;

                    // If all waypoints have been traversed, finish
                    if (activeIndex >= walkthroughManager.waypoints.Count) {
                        CancelExecution();
                        return ;
                    }
            }

            // Prevents oscillation about active waypoint if the target reaches without the tracker
            // Make it a bit less forgiving than for the tracker
            Vector3 difference = activeTransformData.position - endTarget.transform.position;
            if (difference.magnitude < waypointReachedThreshold * 0.5) {
                endTarget.transform.SetPositionAndRotation(
                    activeTransformData.position, activeTransformData.rotation);
                return ;
            }

            // Position
            Vector3 direction = difference.normalized;
            Vector3 displacement = speed * Time.deltaTime * direction;
            endTarget.transform.position += displacement;

            // Rotation
            float distFraction = speed * Time.deltaTime / difference.magnitude;
            endTarget.transform.rotation = Quaternion.Slerp(
                endTarget.transform.rotation,
                activeTransformData.rotation,
                distFraction);
        }
    }

    private void ActivateExecutionMode()
    {
        endTargetComponent.SetTargetActive(true);
        spring.SetActive(true);
        safetyFieldsManager.SetFieldsActive(true);
    }

    private void DeactivateExecutionMode()
    {
        endTargetComponent.SetTargetActive(false);
        spring.SetActive(false);
        safetyFieldsManager.SetFieldsActive(false);
    }

    public void ToggleExecution()
    {
        paused = !paused;
        if (paused) {
            // Paused
            DeactivateExecutionMode();
        } else {
            // Resumed
            ActivateExecutionMode();
        }

        // Setting text
        if (tmps.Count() == 0) tmps = pieMenu.GetComponentsInChildren<TextMeshPro>(true);
        foreach (TextMeshPro tmp in tmps) {
            if (paused) {
                // Paused
                tmp.SetText(tmp.text.Replace("Pause", "Resume"));
            } else {
                // Resumed
                tmp.SetText(tmp.text.Replace("Start", "Pause"));
                tmp.SetText(tmp.text.Replace("Resume", "Pause"));
            }
        }
    }

    public void CancelExecution() {
        paused = true;
        activeIndex = 0;

        // Return to walkthrough
        DeactivateExecutionMode();

        // Reset text
        if (tmps.Count() == 0) tmps = pieMenu.GetComponentsInChildren<TextMeshPro>();
        foreach (TextMeshPro tmp in tmps) {
            tmp.SetText(tmp.text.Replace("Pause", "Start"));
            tmp.SetText(tmp.text.Replace("Resume", "Start"));
        }
    }
}
