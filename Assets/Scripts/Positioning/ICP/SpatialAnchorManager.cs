using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;



public class SpatialAnchorManager : MonoBehaviour
{
    public OVRSpatialAnchor anchorPrefab;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    // Create and save spatial anchor
    public void CreateAndSaveAnchor(TransformData transformData, string key)
    {
        OVRSpatialAnchor anchor = Instantiate(anchorPrefab, transformData.position, transformData.rotation);
        StartCoroutine(SaveAnchor(anchor, key));
    }

    private IEnumerator SaveAnchor(OVRSpatialAnchor anchor, string key)
    {
        // Wait for anchor to be created and localised
        yield return new WaitUntil(() => !anchor.Created && !anchor.Localized);

        // Save to player preferences
        anchor.SaveAnchorAsync().ContinueWith(
            (success, anchor) =>
            {
                if (success)
                {
                    PlayerPrefs.SetString(key, anchor.Uuid.ToString());
                    PlayerPrefs.Save();
                    Debug.Log("Anchor saved.");
                }
                else
                {
                    Debug.LogWarning("Could not save anchor.");
                }
            },
        anchor);
    }

    public async Task LoadAnchor(string key, Action<bool, OVRSpatialAnchor.UnboundAnchor> onLocaliseCallback, Action onLoadFailureCallback)
    {
        if (!PlayerPrefs.HasKey(key))
        {
            onLoadFailureCallback();
            return;
        }

        List<OVRSpatialAnchor.UnboundAnchor> unboundAnchors = new();
        string anchorUuidValue = PlayerPrefs.GetString(key);
        List<Guid> anchorUuids = new() { new(anchorUuidValue) };
        var result = await OVRSpatialAnchor.LoadUnboundAnchorsAsync(anchorUuids, unboundAnchors);

        if (result.Success)
        {
            // Only expecting one result
            OVRSpatialAnchor.UnboundAnchor anchor = result.Value[0];
            anchor.LocalizeAsync().ContinueWith(onLocaliseCallback, anchor);
        }
        else
        {
            onLoadFailureCallback();
        }
    }
}
