using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class WaypointVisualisation : MonoBehaviour
{
    public GameObject staticWaypointPrefab;
    public GameObject linePrefab;
    private List<GameObject> waypointObjects = new List<GameObject>();

    // Start is called before the first frame update
    void Start()
    {
        List<TransformData> waypoints = WalkthroughManager.waypointTransformData;
        for (int i=0; i<waypoints.Count; i++) {
            GameObject waypointObject = Instantiate(staticWaypointPrefab);
            waypointObject.GetComponent<Waypoint>().SetWaypointTransform(
                waypoints[i].position, waypoints[i].rotation
            );
            waypointObject.GetComponent<Waypoint>().SetIndex(i);
            waypointObjects.Add(waypointObject);
            waypointObject.transform.SetParent(transform);
        }

        for (int i=0; i<waypoints.Count-1; i++) {
            GameObject line = Instantiate(linePrefab);
            line.transform.SetParent(transform);
            LineRenderer lineRenderer = line.GetComponent<LineRenderer>();
            lineRenderer.SetPosition(0, waypoints[i].position);
            lineRenderer.SetPosition(1, waypoints[i+1].position);
        }
    }
}
