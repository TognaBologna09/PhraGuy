using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class AnimFoot : MonoBehaviour
{
    [Header("Player")]
    [Tooltip("A reference to the Player Transform with collider.")]
    public Transform bodyRoot;
    public Rigidbody playerRigidbody;
    public AnimFoot otherFoot;

    [Space(10)]
    [Header("Step Qualities")]
    [Tooltip("Spacing of the feet in local X to the right, so left-limbs are negative.")]
    public float footSpacing;
    [Tooltip("Spacing of the feet in local Z forward, so back-limbs are negative.")]
    public float footHandSpacing;
    public LayerMask stepLayer;
    public float legHeight;
    public float stepSpeed;
    private float charSpeed, stepDistance, stepHeight;


    [Tooltip("Multiplies by the charSpeed to determine StepHeight variable.")]
    [Range(0, 0.25f)]
    public float stepHeightModifier;
    public float stepHeightBias;
    [Tooltip("Multiplies by the charSpeed to determine StepDist variable.")]
    [Range(0, 0.1f)]
    public float stepDistanceModifier;
    public float stepDistanceBias;

    [Space(10)]
    [Header("Grounded Settings")]
    [Tooltip("A float for the height offset from the ground to tune the grounded case")]
    public float groundedOffset;
    public float groundedRadius;
    public LayerMask groundLayer;
    public bool isGrounded;

    [Header("TEMPORARY_REMOVE_ME_LATER")]
    // vector3 calls
    private Vector3 distanceVector;
    private float distance, time;
    public Vector3  initialPosition, currentPosition, oldPosition, newPosition;
    public Vector3 initialNormal, currentNormal, oldNormal, newNormal;
    
    [Header("Debug!")]
    public bool isDebugMode;
    public bool isStationary;
    private Vector3 debugVector;
    private Vector3 infoVector;

    private void Start()
    {
        initialPosition = transform.localPosition;
        oldNormal = initialNormal = transform.up;
        oldPosition = transform.position;
        
    }
    public Vector3 rotationEuler;

    private void FixedUpdate()
    {

        GroundedCheck();

        charSpeed = playerRigidbody.velocity.magnitude;

        stepDistance = stepDistanceBias + stepDistanceModifier * charSpeed;    // 0.2f is a decent value for bipeds
        stepHeight = stepHeightBias + stepHeightModifier * charSpeed;    // 0.025f is a decent value for bipeds

        infoVector = new Vector3(charSpeed, stepDistance, stepHeight);

        // To most easily read function variables:
        //
        // Transform to current location
        // old -=> current  
        //          currentFarEnough? -=> new : old
        //                          

        // Set Transform and vectors
        transform.position = oldPosition = currentPosition;
        transform.up = oldNormal = currentNormal;
   
        // Now search for a new point
        Ray ray = new Ray(bodyRoot.position + (bodyRoot.right * footSpacing)+(bodyRoot.forward * footHandSpacing), Vector3.down);
        if (Physics.Raycast(ray, out RaycastHit info, legHeight, stepLayer))
        {   
            debugVector = info.point;
            if (isDebugMode)
            {
                Debug.DrawRay(bodyRoot.position + (bodyRoot.right * footSpacing) + (bodyRoot.forward * footHandSpacing), Vector3.down * legHeight, Color.magenta);
                Debug.DrawRay(oldPosition, (info.point - oldPosition).normalized * distance, Color.red);
            }

            if (Vector3.Distance(oldPosition, info.point) > stepDistance && time >= 1 && !otherFoot.IsMoving()) 
            {
                time = 0;
                int direction = bodyRoot.InverseTransformPoint(info.point).z > bodyRoot.InverseTransformPoint(newPosition).z ? 1 : -1;
                newPosition = info.point + (bodyRoot.forward * stepDistance * direction);
                newNormal = info.normal;
            }
        }

        if (time < 1)
        {
            Vector3 tempPosition = Vector3.Lerp(oldPosition, newPosition, time);
            tempPosition.y += Mathf.Sin(time * Mathf.PI) * stepHeight;

            currentPosition = tempPosition;
            currentNormal = Vector3.Lerp(oldNormal, newNormal, time);
            time += Time.deltaTime * stepSpeed;

        }
        else
        {
            currentPosition = oldPosition;
            currentNormal = oldNormal;
        }

        if (isDebugMode)
        {
            Debug.DrawRay(bodyRoot.position + (bodyRoot.right * footSpacing) + (bodyRoot.forward * footHandSpacing), Vector3.forward * stepDistance, Color.yellow);
            Debug.Log("Next line is <speed, stepDistance, stepHeight>");
            Debug.Log(infoVector);
        }

        transform.position = currentPosition;
        transform.up = currentNormal + rotationEuler;
    }

    public bool IsMoving()
    {
        return time < 1;
    }

    public bool GroundedCheck()
    {
        Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - groundedOffset,
                transform.position.z);
        isGrounded = Physics.CheckSphere(spherePosition, groundedRadius, groundLayer,
            QueryTriggerInteraction.Ignore);
        return isGrounded;
    }

    // visualization gizmos
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(transform.position, 0.2f * groundedRadius);

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(newPosition, 0.25f * groundedRadius);

        Gizmos.color = Color.magenta;
        Gizmos.DrawSphere(debugVector, 0.15f * groundedRadius);

        if (!isGrounded)
        {
            Gizmos.color = Color.red;
        }
        else
        {
            Gizmos.color = Color.green;
        }

        Gizmos.DrawSphere(new Vector3(bodyRoot.position.x, bodyRoot.position.y - groundedOffset, bodyRoot.position.z), 0.01f*groundedRadius);

    }
}
