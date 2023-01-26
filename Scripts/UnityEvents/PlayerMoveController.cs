using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Interactions;

public class PlayerMoveController : MonoBehaviour
{

    private Rigidbody rb;

    // player
    [Space(10)]
    [Header("Player Movement Settings")]

    [Tooltip("Default move speed of the character")]
    public float moveSpeed;
    public float sprintSpeed;
    public float maxGroundSpeed;
    public float maxAirSpeed;
    public float walkForce;
    public float rotationSpeed;
    [Tooltip("Modifies the strength of move-inputs received in the air")]
    public float _airMoveDrag = 0.25f;
    private Vector3 modifiedGravity = Vector3.zero;

    [Header("Player Move States")]
    public bool isGrounded = false;
    public bool isGrappling = false;
    public bool isJumping = false;
    public bool upperObstruction = false;
    public bool lowerObstruction = false;

    [Header("Player Jump Settings")]
    public float jumpForce;
    public float maxJumpHoldTime;
    public float jumpHeightModifier;
    private Vector3 jumpDirection;
    private float extraJumpForce;
    private bool m_jumpCharging = false;
    private float timeOfLastJump;
    private float jumpVisualizationMagnitude;

    [Space(10)]
    [Header("Grounded Settings")]
    public float groundedOffset = -0.14f;
    [Tooltip("The radius of the grounded sphere check. A little extra helps")]
    public float groundedRadius = 0.82f;
    public Transform smoothBumpRayUpper, smoothBumpRayLower;
    public float stepSmooth;
    [Tooltip("Multiplied by the gravity value when grounded to keep you grounded")]
    public float gravityModifier = 1.8f;
    public float maxSlopeAngle, slopeRayLength;
    public float lowerObstructionLength, upperObstructionLength;
    private float lastTimeOnSlope, slopeAngle;
    [Tooltip("What layers the character uses as ground")]
    public LayerMask groundLayers;
    private Vector3 groundedMove = new Vector3(0f, 0f, 0f);
    
    [Space(10)]
    [Header("Mouse Cursor Settings")]
    public LayerMask aimColliderLayerMask;
    public bool cursorLocked = true;
    public bool cursorInputForLook = true;
    private Vector3 mouseWorldPosition = Vector3.zero;
    private Ray aimRay;

    [Header("Debug!")]
    public bool isDebugMode = false;

    // audio
    public AudioSource jumpAudio1, jumpAudio2;

    // inputs variables
    private Vector2 m_Move;
    private Vector2 m_Look;

    // components
    private SpringJoint joint;
    private Animator animator;
    // camera
    private GameObject mainCamera;
    

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        mainCamera = GameObject.FindGameObjectWithTag("MainCamera");
        animator = GetComponent<Animator>();

        if (isDebugMode)
        {
            float extraJumpForce;
            Vector3 jumpDirection;
        }
    }

    // input calls
    public void OnMove(InputAction.CallbackContext context)
    {
        m_Move = context.ReadValue<Vector2>();
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        switch (context.phase)
        {
            case InputActionPhase.Performed:

                if (isDebugMode) Debug.Log("Jump Action Performed");

                isJumping = true;

                if (isGrounded || isGrappling)
                {
                    if (context.interaction is SlowTapInteraction)
                    {
                        // store context duration as a float (units of seconds)
                        float t = (float)(context.duration);

                        // if this time is too long, constrain jump height so /= infinite height
                        if (t > maxJumpHoldTime)
                        {
                            Jump(jumpForce, t);
                            jumpAudio2.Play();

                            if (isDebugMode)
                            {
                                Debug.Log(t);
                                Debug.Log(maxJumpHoldTime);
                                Debug.Log("a > b : Too Long!");
                            }

                        }
                        else
                        {
                            if (isDebugMode) Debug.Log(t);

                            Jump(jumpForce, t);

                            jumpAudio2.Play();
                        }
                        if (isGrappling)
                        {
                            joint.maxDistance = 0.0f;
                            Destroy(joint);
                        }
                    }
                    else
                    {
                        if (isGrappling)
                        {
                            joint.maxDistance = 0.0f;
                            Jump(jumpForce, 0);
                
                            jumpAudio1.Play();
                            Destroy(joint);
                            
                        }
                        else
                        {
                            Jump(jumpForce, 0);
                
                            jumpAudio1.Play();
                        }
                        
                    }

                    m_jumpCharging = false;
                    isJumping = false;
                }
                else
                {

                }
                m_jumpCharging = false;
                isJumping = false;
                break;

            case InputActionPhase.Started:

                if (isDebugMode) Debug.Log("Jump Action Started");

                timeOfLastJump = Time.time;

                if (context.interaction is SlowTapInteraction)
                {
                    m_jumpCharging = true;
                    isJumping = true;

                    if (isDebugMode) Debug.Log("Charging UP!");
                    
                }
                break;

            case InputActionPhase.Canceled:
                
                m_jumpCharging = false;
                isJumping = false;

                if (isDebugMode) Debug.Log("Button Canceled");

                break;
        }
    }

    private void FixedUpdate()
    {
        GroundedCheck();
        GrappleCheck();
        SmoothMovementAtBumps();
        Move(m_Move);
        

        if (isDebugMode)
        {
            Debug.DrawRay(transform.position, transform.forward * (3f + jumpVisualizationMagnitude));
        }
        
    }

    private Vector3 rotatedDirection;

    // movement methods
    private void Move(Vector2 direction)
    {
        // targetSpeed reference for when 'sprint is added'? sprintSpeed : moveSpeed;
        float targetSpeed = moveSpeed;

        // if no input is recorded, targetSpeed is adjusted
        if (direction == Vector2.zero) targetSpeed = 0.0f;

        // Useful variables
        float currentHorizontalSpeed = new Vector3(rb.velocity.x, 0.0f, rb.velocity.z).magnitude;

        // read input from Move InputAction into a Vector3
        Vector3 newDirection = new Vector3(direction.x, 0, direction.y).normalized;
        rotatedDirection = mainCamera.transform.localRotation * newDirection; 
        rotatedDirection = new Vector3( rotatedDirection.x, 0f, rotatedDirection.z).normalized;

        
       
        if (!isGrounded)
        {
            ModifiedGravity(-1);

            transform.forward = Vector3.Slerp(transform.forward, rotatedDirection, Time.deltaTime * rotationSpeed);
            rotatedDirection *= _airMoveDrag;
        }

        if (OnFloor())
        {
            ModifiedGravity(-1);

            transform.forward = Vector3.Slerp(transform.forward, rotatedDirection, Time.deltaTime * rotationSpeed);
            rb.AddForce(walkForce * rotatedDirection);
        }

        // Check if the player is grounded
        if (currentHorizontalSpeed > targetSpeed)
        {
            if (isGrounded && OnFloor())
            {
                Vector3 clamped = Vector3.ClampMagnitude(rb.velocity, targetSpeed);
                rb.velocity = clamped;
            }
            else if (!isGrounded)
            {
                // Midair case for speed clamping
                Vector3 airClamped = Vector3.ClampMagnitude(new Vector3(rb.velocity.x, 0f, rb.velocity.z), maxAirSpeed);

            }
        }
    }

    private void Jump(float jumpForceParameter, float timePart)
    {
        // ON JUMP: aka they touched the spacebar
        // i.) check if hold interaction or tap w/ if float > 0 
        //  iia tap interaction.) jump in target direction minimum force
        //  iib hold interaction.) collect context.time as a float
        //  iiib) multiply jump force Extra by cos(Mathf.pi*jumptime) to add charge-up and down with amplitude 

        // negate modified gravity accel
        groundedMove.y = -2.5f * Physics.gravity.y * gravityModifier;
        rb.AddForce(groundedMove, ForceMode.Impulse);

        if (isDebugMode) Debug.Log("Jump Action!");

        Vector3 jumpDirection = transform.forward;

        if (timePart > 0)
        {
            float extraJumpForce = jumpForceParameter * Mathf.Cos(Mathf.PI * timePart);
            rb.AddForce(3.1f * jumpForce * jumpDirection + 1.2f * extraJumpForce * jumpDirection + jumpHeightModifier * jumpForce * transform.up);
        }
        else
        {
            rb.AddForce(jumpForce * jumpDirection + jumpHeightModifier * jumpForce * transform.up);
        }
        extraJumpForce = 0f;
    }

    private void ModifiedGravity(int direction)
    {
        modifiedGravity.y = direction * 9.81f * gravityModifier;
        rb.AddForce(modifiedGravity, ForceMode.Acceleration);
    }

    private void SmoothMovementAtBumps()
    {
        // Shoot out two raycasts: one is at the toe-level and the other around the knees. 
        // If the first returns, and not the second then the obstruction isn't too tall
        // and we can transition the player to the height
        
        // forward smoothing
        if (Physics.Raycast(smoothBumpRayLower.position, transform.forward, out RaycastHit infoLower, lowerObstructionLength, groundLayers))
        {
            lowerObstruction = true;
            if (isDebugMode)
            {
                Debug.Log("There is an obstruction at the foot level");
            }
            if (!Physics.Raycast(smoothBumpRayUpper.position, transform.forward, out RaycastHit infoUpper, upperObstructionLength, groundLayers))
            {
                upperObstruction = false;
                rb.MovePosition(transform.position + new Vector3(0f, stepSmooth, 0f));
            }
            else
            {
                upperObstruction = true;
                if (isDebugMode)
                {
                    Debug.Log("Jeez this obstruction is tall, we can't move past this");
                }
            }
        }
        else
        {
            lowerObstruction = false;
        }
        // yaw 56* right smoothing
        if (Physics.Raycast(smoothBumpRayLower.position, transform.TransformDirection(1.5f,0,1), out RaycastHit infoLowerR, lowerObstructionLength, groundLayers))
        {
            lowerObstruction = true;
            if (isDebugMode)
            {
                Debug.Log("There is an obstruction at the foot level");
            }
            if (!Physics.Raycast(smoothBumpRayUpper.position, transform.TransformDirection(1.5f, 0, 1), out RaycastHit infoUpperR, upperObstructionLength, groundLayers))
            {
                upperObstruction = false;
                rb.MovePosition(transform.position + new Vector3(0f, stepSmooth, 0f));
            }
            else
            {
                upperObstruction = true;
                if (isDebugMode)
                {
                    Debug.Log("Jeez this obstruction is tall, we can't move past this");
                }
            }
        }
        else
        {
            lowerObstruction = false;
        }
        // yaw 56* left smoothing
        if (Physics.Raycast(smoothBumpRayLower.position, transform.TransformDirection(-1.5f, 0, 1), out RaycastHit infoLowerL, lowerObstructionLength, groundLayers))
        {
            lowerObstruction = true;
            if (isDebugMode)
            {
                Debug.Log("There is an obstruction at the foot level");
            }
            if (!Physics.Raycast(smoothBumpRayUpper.position, transform.TransformDirection(-1.5f, 0, 1), out RaycastHit infoUpperL, upperObstructionLength, groundLayers))
            {
                upperObstruction = false;
                rb.MovePosition(transform.position + new Vector3(0f, stepSmooth, 0f));
            }
            else
            {
                upperObstruction = true;
                if (isDebugMode)
                {
                    Debug.Log("Jeez this obstruction is tall, we can't move past this");
                }
            }
        }
        else
        {
            lowerObstruction = false;
        }
    }
    
    

    // grounded methods
    private float SlopeAngle()
    {
        float dist = slopeRayLength + groundedOffset * transform.localScale.y * 0.5f;
        bool hit = Physics.Raycast(rb.position, Vector3.down, out RaycastHit info, dist);
        if (hit)
        {
            return Vector3.Angle(Vector3.up, info.normal);
        }
        return -1;
    }
    private bool OnFloor()
    {
        return SlopeAngle() <= maxSlopeAngle;
    }
    public bool GroundedCheck()
    {
        Vector3 spherePosition = new Vector3(transform.position.x, transform.position.y - groundedOffset,
                transform.position.z);
        isGrounded = Physics.CheckSphere(spherePosition, groundedRadius, groundLayers,
            QueryTriggerInteraction.Ignore);
        return isGrounded;
    }

    // movement state check
    private void GrappleCheck()
    {
        joint = GetComponent<SpringJoint>();
        if (GetComponent<SpringJoint>())
        {
            isGrappling = true;
        }
        else
        {
            isGrappling = false;
        }
    }
    // animations to ragdoll method
    private void AnimatorUpdater()
    {
        if (!isGrounded)
        {
            animator.enabled = false;
        }
        else
        {
            animator.enabled = true;
        }
    }

    // cursor state methods
    private void OnApplicationFocus(bool hasFocus)
    {
        SetCursorState(cursorLocked);
    }

    private void SetCursorState(bool newState)
    {
        Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = true;
    }

    // Debug visualization gizmos
    private void OnDrawGizmosSelected()
    {
        Color transparentGreen = new Color(0.0f, 1.0f, 0.0f, 0.35f);
        Color transparentRed = new Color(1.0f, 0.0f, 0.0f, 0.35f);

        if (isGrounded) Gizmos.color = transparentGreen;
        else Gizmos.color = transparentRed;

        // when selected, draw a gizmo in the position of, and matching radius of, the grounded collider
        Gizmos.DrawSphere(
            new Vector3(transform.position.x, transform.position.y - groundedOffset, transform.position.z),
            groundedRadius);
    }
}
