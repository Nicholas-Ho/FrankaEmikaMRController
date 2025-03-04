using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Analytics;
using UnityEngine.EventSystems;

public class PieMenu : MonoBehaviour
{
    // Active pie menus for closing
    public static List<PieMenu> activePieMenus = new();

    // Variable list of options
    public List<PieMenuOptionInfo> pieMenuOptions = new List<PieMenuOptionInfo>();

    // Is this the primary pie menu?
    public bool primary = false;

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

    // Opacity ratio when not in focus
    public float outOfFocusOpacityRation = 0.5f;

    // Hand GameObject
    public GameObject handObject;

    // List of option GameObjects
    private List<GameObject> optionObjects = new();

    // Reference to hand
    private OVRHand hand;

    // Reference to hand skeleton. For extracting index finger location
    private OVRSkeleton handSkeleton;

    // Reference to cancel button GameObject
    private GameObject cancelButton;

    // Sprite and text renderers in children
    SpriteRenderer[] spriteRenderers = {};
    TextMeshPro[] tmps = {};

    // Visibility of object
    private bool visible = false;

    // Is this pie menu in focus?
    private bool focus = false;

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

        // Get hand reference
        hand = handObject.GetComponent<OVRHand>();
        handSkeleton = handObject.GetComponent<OVRSkeleton>();
    }

    // Update is called once per frame
    void Update()
    {
        if (!hand.IsActive()) return ;
        if (!hand.IsTracked) return ;
        bool pinching = hand.GetFingerIsPinching(OVRHand.HandFinger.Index);
        if (!visible) {
            // Only activate on pinching if primary
            if (pinching && primary) BringIntoFocus();
        } else {
            // If not in focus, wait for pie menu in focus to deactivate
            if (!focus) return ;

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

                // If hover to open menu, open child menu
                if (pieMenuOptions[currOption].hoverOpenMenu) {
                    OpenChildPieMenu(currOption);
                    return ;
                }
            }

            if (!pinching) {
                CloseAll();
                // Select option
                if (currOption != -1) optionObjects[currOption].GetComponent<PieMenuOption>().ExecuteCallback();
            }
        }
    }

    public void BringIntoFocus()
    {
        Vector3 rightIndexPos = HandUtilities.GetIndexTipTransform(handSkeleton).position;

        if (primary) {
            // Set position to hand
            Vector3 headPos = GameObject.FindWithTag("MainCamera").transform.position;
            transform.position = rightIndexPos;
            transform.LookAt(headPos);
        } else {
            Vector3 forward = activePieMenus[^1].transform.forward;
            transform.position = Vector3.ProjectOnPlane(rightIndexPos, forward)
                + forward * Vector3.Dot(activePieMenus[^1].transform.position, forward)
                + forward * 0.03f;  // Offset distance from parent
            transform.forward = forward;
        }

        // If there are other active pie menus, ensure that they overlay the parents
        spriteRenderers = GetComponentsInChildren<SpriteRenderer>(true);
        for (int i=0; i<spriteRenderers.Count(); i++)
            spriteRenderers[i].sortingOrder += 2 * activePieMenus.Count;
        tmps = GetComponentsInChildren<TextMeshPro>(true);
        for (int i=0; i<tmps.Count(); i++)
            tmps[i].sortingOrder += 2 * activePieMenus.Count;
    
        SetVisibility(true);
        focus = true;
        activePieMenus.Add(this);
    }

    private void OpenChildPieMenu(int currOption)
    {
        // Visuals
        if (spriteRenderers.Count() == 0) spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        for (int i=0; i<spriteRenderers.Count(); i++)
            spriteRenderers[i].color = new Color(
                spriteRenderers[i].color.r,
                spriteRenderers[i].color.g,
                spriteRenderers[i].color.b,
                spriteRenderers[i].color.a * outOfFocusOpacityRation
            );
        if (tmps.Count() == 0) tmps = GetComponentsInChildren<TextMeshPro>();
        for (int i=0; i<tmps.Count(); i++)
            tmps[i].color = new Color(
                tmps[i].color.r,
                tmps[i].color.g,
                tmps[i].color.b,
                tmps[i].color.a * outOfFocusOpacityRation
            );

        focus = false;
        optionObjects[currOption].GetComponent<PieMenuOption>().ExecuteCallback();
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

    public void Close()
    {
        if (spriteRenderers.Count() == 0) spriteRenderers = GetComponentsInChildren<SpriteRenderer>();
        if (tmps.Count() == 0) tmps = GetComponentsInChildren<TextMeshPro>();

        // Restore order layer
        for (int i=0; i<spriteRenderers.Count(); i++)
            spriteRenderers[i].sortingOrder -= 2 * activePieMenus.Count;
        for (int i=0; i<tmps.Count(); i++)
            tmps[i].sortingOrder -= 2 * activePieMenus.Count;
        
        // Restore visuals if out of focus
        if (!focus) {
            for (int i=0; i<spriteRenderers.Count(); i++)
                spriteRenderers[i].color = new Color(
                    spriteRenderers[i].color.r,
                    spriteRenderers[i].color.g,
                    spriteRenderers[i].color.b,
                    spriteRenderers[i].color.a / outOfFocusOpacityRation
                );
            for (int i=0; i<tmps.Count(); i++)
                tmps[i].color = new Color(
                    tmps[i].color.r,
                    tmps[i].color.g,
                    tmps[i].color.b,
                    tmps[i].color.a / outOfFocusOpacityRation
                );
        }

        focus = false;
        SetVisibility(false);
    }

    public void CloseAll()
    {
        for (int i=activePieMenus.Count-1; i>=0; i--) {
            PieMenu curr = activePieMenus[i];
            activePieMenus.RemoveAt(i);
            curr.Close();
        }
    }
}


[System.Serializable]
public struct PieMenuOptionInfo
{
    public string name;
    public EventTrigger.TriggerEvent callback;
    public bool hoverOpenMenu;
}