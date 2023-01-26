using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RagJointAnim : MonoBehaviour
{
    public Transform target;
    private ConfigurableJoint configj;
    Quaternion targetInitialRotation;

    // Start is called before the first frame update
    void Start()
    {
        configj = this.GetComponent<ConfigurableJoint>();
        targetInitialRotation = transform.localRotation;
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        copyRotation();
    }
    private void copyRotation()
    {
        configj.targetRotation = Quaternion.Inverse(target.localRotation) * targetInitialRotation;
    }
}
