using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VerticalLayoutAutoSpacing : MonoBehaviour
{
    [SerializeField] VerticalLayoutGroup layoutGroup;

    // Update is called once per frame
    void Update()
    {
        if(layoutGroup.transform.childCount > 5) {
            layoutGroup.spacing = -100;
            layoutGroup.childForceExpandHeight = true;
        }
        else {
            layoutGroup.spacing = -10;
            layoutGroup.childForceExpandHeight = false;
        }
    }
}
