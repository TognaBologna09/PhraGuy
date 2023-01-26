using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimBody : MonoBehaviour
{
    // take the avg position of leg targets + offset
    // to position body rotation, normal

    public Transform spineTarget;

    public Transform limbi, limbii, limbiii, limbiv;
    public Vector3 offset;

    private Vector3 average;
    private Vector3 averageNormal;
   
    void AvgPosition()
    {
        average = 0.25f * limbi.position + 0.25f * limbii.position + 0.25f * limbiii.position + 0.25f * limbiv.position;
        averageNormal = 0.25f * (limbi.forward + limbii.forward + limbiii.forward + limbiv.forward);
    }

    private void FixedUpdate()
    {
        AvgPosition();
        spineTarget.position = average + offset;
        spineTarget.up = averageNormal;
    }
}

