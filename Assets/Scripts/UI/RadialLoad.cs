using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RadialLoad : MonoBehaviour
{
    public ProximityButton proximityButton;
    private Image loadSprite;

    // Start is called before the first frame update
    void Start()
    {
        loadSprite = gameObject.GetComponent<Image>();
    }

    // Update is called once per frame
    void Update()
    {
        loadSprite.fillAmount = proximityButton.GetProgress();
    }
}
