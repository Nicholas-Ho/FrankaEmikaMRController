using System.Buffers.Text;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
public class ExecutionManager : MonoBehaviour
{

    public GameObject endTracker;
    public GameObject endTarget;
    public GameObject spring;
    public GameObject pieMenu;  // For setting text
    public float speed = 0.1f; // per second
    public float waitDistanceThreshold = 0.2f;
    public float resumeDistanceThreshold = 0.1f;
    public float waypointReachedThreshold = 0.05f;
    private List<TransformData> waypoints;
    private int activeWaypoint = 0;
    private bool waiting = false;
    private bool paused = false;
    private bool finished = false;

    // Start is called before the first frame update
    void Start()
    {
        waypoints = WalkthroughManager.waypointTransformData;
        activeWaypoint = 0;
    }

    // Update is called once per frame
    void Update()
    {
        if (endTarget.GetComponent<EndTarget>().IsReady() && !finished && !paused) {
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

            if (Vector3.Distance(endTracker.transform.position, waypoints[activeWaypoint].position)
                < waypointReachedThreshold) {
                    // If waypoint reached, move to next waypoint
                    activeWaypoint++;

                    // If all waypoints have been traversed, finish
                    if (activeWaypoint == waypoints.Count) {
                        // endTarget.SetActive(false);
                        // endTracker.SetActive(false);
                        // spring.SetActive(false);
                        finished = true;
                        return ;
                    }
            }

            Vector3 direction = (waypoints[activeWaypoint].position - endTarget.transform.position).normalized;
            Vector3 displacement = speed * Time.deltaTime * direction;
            endTarget.transform.position += displacement;
        }
    }

    public void ToggleExecution()
    {
        paused = !paused;
        TextMeshPro[] tmps = pieMenu.GetComponentsInChildren<TextMeshPro>();
        foreach (TextMeshPro tmp in tmps) {
            if (paused) {
                tmp.SetText(tmp.text.Replace("Pause", "Resume"));
            } else {
                tmp.SetText(tmp.text.Replace("Resume", "Pause"));
            }
        }
    }

    public void CancelExecution() => finished = true;

    public void ReturnToProgramming()
    {
        // End execution and return to programming
        CancelExecution();
        SceneManager.LoadScene(1, LoadSceneMode.Single);
    }
}
