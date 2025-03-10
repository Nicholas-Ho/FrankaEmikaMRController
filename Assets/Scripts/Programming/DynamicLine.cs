using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class DynamicLine : MonoBehaviour
{
    public GameObject insertButtonPrefab;
    private List<Transform> refTransforms = new();
    private List<GameObject> insertButtons = new();
    private LineRenderer lineRenderer;
    private Transform headTransform;
    private bool initialised = false;

    // Start is called before the first frame update
    void Start()
    {
        if (!initialised) Initialise();
        SetPositions();
    }

    void Initialise()
    {
        lineRenderer = gameObject.GetComponent<LineRenderer>();
        initialised = true;
        headTransform = GameObject.FindWithTag("MainCamera").transform;
    }

    // Update is called once per frame
    void Update()
    {
        SetPositions();
    }

    private void SetPositions()
    {
        for (int i=0; i<refTransforms.Count; i++) {
            lineRenderer.SetPosition(i, refTransforms[i].position);
        }
        for (int i=0; i<insertButtons.Count; i++) {
            insertButtons[i].transform.position =
                (refTransforms[i].position + refTransforms[i+1].position) / 2;
            insertButtons[i].transform.LookAt(headTransform.position);
        }
    }

    public void AddReferenceTransform(Transform t, UnityAction<BaseEventData> action)
    {
        if (!initialised) Initialise();

        refTransforms.Add(t);
        lineRenderer.positionCount = refTransforms.Count;

        if (refTransforms.Count <= 1) return ;
        GameObject insertButton = Instantiate(insertButtonPrefab);
        insertButtons.Add(insertButton);
        insertButton.GetComponent<ProximityButton>().callback.AddListener(action);
    }

    public void PopLastReferenceTransform()
    {
        if (refTransforms.Count == 0) return ;

        // Remove transform and update line
        refTransforms.RemoveAt(refTransforms.Count-1);
        lineRenderer.positionCount = refTransforms.Count;

        // Remove and destroy insert button (if any)
        if (insertButtons.Count == 0) return ;
        GameObject lastInsertButton = insertButtons[^1];
        insertButtons.RemoveAt(insertButtons.Count-1);
        Destroy(lastInsertButton);
    }

    public void ResetButtonState(int index)
    {
        if (index < 0 || index >= insertButtons.Count) {
            Debug.LogWarning("Index out-of-range while resetting insert button.");
            return ;
        }
        insertButtons[index].GetComponent<ProximityButton>().ResetState();
    }

    public Vector3 GetMidpoint(int index) { return insertButtons[index].transform.position; }
}
