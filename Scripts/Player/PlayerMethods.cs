using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Interactions;
using FishNet.Connection;
using FishNet.Object;
using Cinemachine;

public sealed class PlayerMethods : NetworkBehaviour
{
    public static PlayerMethods Instance { get; private set; }

    // player prefab
    public GameObject playerObject;

    // components
    private Rigidbody rb;
    private SpringJoint joint;
    private LineRenderer lr;

    // UI
    public GameObject viewParent;
 
    // inputs
    public ControlMap controlMap;
    private InputAction move;
    private InputAction shoot;
    private InputAction jump;
    private InputAction sprint;
    private InputAction pause;

    // variables from inputs
    Vector2 moveDirection;
    Vector3 moveRotatedDirection;

    [Space(8)]
    // move settings
    [Header("Movement Settings")]
    public float walkForce = 14f;
    public float moveSpeed = 1.8f;
    public float sprintSpeed = 3.1f;
    public float maxAirSpeed = 6.9f;
    public float rotationSpeed = 3f;
    public float airMoveDrag = 0.25f;
    [Space(4)]
    [Header("Smooth Movement Settings")]
    public float stepSmoothVelocity = 0.1f;
    public Transform smoothBumpRayUpper, smoothBumpRayLower;
    public float upperObstructionLength, lowerObstructionLength;
    private Vector3 modifiedGravity = Vector3.zero;

    [Space(8)]
    [Header("Grounded Settings")]
    public float groundedOffset = -0.14f;
    public float groundedRadius = 0.82f;
    public float gravityModifier = 1.8f;
    public float maxSlopeAngle = 45f;
    public float slopeRayLength = 2f;
    private float lastTimeOnSlope, slopeAngle;
    public LayerMask groundLayers;
    private Vector3 groundedMove = new Vector3(0f, 0f, 0f);
    
    [Space(8)]
    [Header("Grounded Settings")]
    public float buoyantBias, buoyantDelta;
    public float swimForce;
    public LayerMask waterLayers;


    [Space(8)]
    [Header("Player Jump Settings")]
    public float jumpForce;
    public float maxJumpHoldTime;
    public float jumpHeightModifier;
    private Vector3 jumpDirection;
    private float extraJumpForce;
    private bool m_jumpCharging = false;
    private float timeOfLastJump;
    private float jumpVisualizationMagnitude;


    [Header("Player Move States")]
    public bool isGrounded = false;
    public bool isSprinting = false;
    public bool isJumping = false;
    public bool isTethered = false;
    public bool upperObstruction = false;
    public bool lowerObstruction = false;
    public bool isPaused = false;
    public bool isUnderwater = false;

    [Space(8)]
    // spring settings
    [Header("Grapple Settings")]
    public float spring;
    public float damper, massScale;
    public float maxTolerance, minTolerance;
    public LayerMask grappleLayer;
    private Vector3 grapplePoint;
    public float grappleDistance, aimAssistRadius;
    
    public Transform tongueTip;

    [Space(10)]
    [Header("Mouse Cursor Settings")]
    public LayerMask aimColliderLayerMask;
    public bool cursorLocked = true;
    public bool cursorInputForLook = true;
    private Vector3 mouseWorldPosition = Vector3.zero;
    private Ray aimRay;

    //deprecatedCameraSettingsREMOVEREMOVEREMOVEREMOVEREMOVE
    private float cameraYOffset = 1.2f;

    public GameObject playerCamera, cameraRoot;

    // audio
    public AudioSource jumpAudio1, jumpAudio2;

    [Header("Debug!")]
    public bool isDebugMode = false;

    public override void OnStartClient()
    {

        base.OnStartClient();
        
        if (base.IsOwner)
        {


            var CinemachineCamera = FindObjectOfType<CinemachineFreeLook>();
            
            CinemachineCamera.LookAt = cameraRoot.transform;
            CinemachineCamera.Follow = playerObject.transform;

            playerCamera = FindObjectOfType<CinemachineFreeLook>().gameObject;
            playerCamera.transform.position = new Vector3(transform.position.x, transform.position.y + cameraYOffset, transform.position.z);
            playerCamera.transform.SetParent(transform);
        }
        else
        {
            GetComponent<PlayerMethods>().enabled = false;
        }
    }


    void Awake()
    {
        controlMap = new ControlMap();
        rb = GetComponent<Rigidbody>();
    }

    private void OnEnable()
    {
        sprint = controlMap.Gameplay.Sprint;
        sprint.performed += DoSprint;
        sprint.Enable();

        move = controlMap.Gameplay.Move;
        move.Enable();

        shoot = controlMap.Gameplay.Shoot;
        shoot.performed += DoShoot;
        shoot.canceled += DoStopShoot;
        shoot.Enable();

        jump = controlMap.Gameplay.Jump;
        jump.performed += DoJump;
        jump.Enable();

        pause = controlMap.Gameplay.Pause;
        pause.performed += DoPause;
        pause.Enable();

    }

    private void OnDisable()
    {
        move.Disable();
        shoot.Disable();
        jump.Disable();
        sprint.Disable();
        pause.Disable();
    }

    void Update()
    {
        moveDirection = move.ReadValue<Vector2>();
    }

    void FixedUpdate()
    {
        GroundedCheck();
        WaterCheck();

        SmoothMovementAtBumps();
        MoveBody(moveDirection);
    }

    void LateUpdate()
    {
        DrawTongue();
    }

    // custom movement
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
                rb.velocity = new Vector3(rb.velocity.x, rb.velocity.y + stepSmoothVelocity, rb.velocity.z);
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
        if (Physics.Raycast(smoothBumpRayLower.position, transform.TransformDirection(1.5f, 0, 1), out RaycastHit infoLowerR, lowerObstructionLength, groundLayers))
        {
            lowerObstruction = true;
            if (isDebugMode)
            {
                Debug.Log("There is an obstruction at the foot level");
            }
            if (!Physics.Raycast(smoothBumpRayUpper.position, transform.TransformDirection(1.5f, 0, 1), out RaycastHit infoUpperR, upperObstructionLength, groundLayers))
            {
                upperObstruction = false;
                rb.velocity = new Vector3(rb.velocity.x, rb.velocity.y + stepSmoothVelocity, rb.velocity.z);
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
                rb.velocity = new Vector3(rb.velocity.x, rb.velocity.y + stepSmoothVelocity, rb.velocity.z);
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
    private void DoSprint(InputAction.CallbackContext context)
    {
        if (context.ReadValue<float>() > 0) isSprinting = true;

        isSprinting = false;
    }
    public void DoJump(InputAction.CallbackContext context)
    {
        switch (context.phase)
        {
            case InputActionPhase.Performed:

                if (isDebugMode) Debug.Log("Jump Action Performed");

                isJumping = true;

                if (isGrounded || isTethered)
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
                        if (isTethered)
                        {
                            joint.maxDistance = 0.0f;
                            Destroy(joint);
                        }
                    }
                    else
                    {
                        if (isTethered)
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
    private void WaterCheck()
    {
        // poll downwards for water layer a short distance,
        // poll upwards for water layer a large distance
        // if either are true you're underwater and your movement is changed for water in the fixed update

        if(Physics.Raycast(transform.position, Vector3.down, groundedRadius, waterLayers))
        {
            if (isDebugMode)
            {
                Debug.DrawRay(transform.position, Vector3.down * 2f, Color.blue);
            }
            isUnderwater = true;
        }
        if(Physics.Raycast(transform.position, Vector3.up, 100f, waterLayers))
        {
            if (isDebugMode) 
            {
                Debug.DrawRay(transform.position, Vector3.up * 100f, Color.blue);
            }
            
            isUnderwater = true;
        }
        else
        {
            isUnderwater=false;
        }
    }
    // movement methods
    void MoveBody(Vector2 input)
    {
        // targetSpeed reference for when 'sprint is added'? sprintSpeed : moveSpeed;
        float targetSpeed = isSprinting ? sprintSpeed : moveSpeed;

        // if no input is recorded, targetSpeed is adjusted
        if (input == Vector2.zero) targetSpeed = 0.0f;
        // useful var
        float currentHorizontalSpeed = new Vector3(rb.velocity.x, 0.0f, rb.velocity.z).magnitude;
        // read input from Move InputAction into a Vector3
        Vector3 newDirection = new Vector3(input.x, 0, input.y).normalized;

        var CinemachineCamera = FindObjectOfType<CinemachineFreeLook>();

        Quaternion cameraDirection = Quaternion.Euler(0f, CinemachineCamera.m_XAxis.Value, 0f);
        moveRotatedDirection = cameraDirection * newDirection;
        moveRotatedDirection = new Vector3(moveRotatedDirection.x, 0f, moveRotatedDirection.z).normalized;

        // check if player is midair
        if (!isGrounded)
        {
            ModifiedGravity(-1);

            transform.forward = Vector3.Slerp(transform.forward, moveRotatedDirection, Time.deltaTime * rotationSpeed);
            moveRotatedDirection *= airMoveDrag;
        }
        // Check if the player is grounded
        if (OnFloor())
        {
            ModifiedGravity(-1);

            transform.forward = Vector3.Slerp(transform.forward, moveRotatedDirection, Time.deltaTime * rotationSpeed);
            rb.AddForce(walkForce * moveRotatedDirection);
        }
        // check if player is underwater
        if (isUnderwater)
        {
            if(Physics.SphereCast(transform.position, 100f, transform.up, out RaycastHit info, waterLayers))
            {
                float dist = Vector3.Distance(transform.position, info.point);
                float buoyantForce = buoyantBias + buoyantDelta * dist;

                rb.AddForce(buoyantForce * Vector3.up);
                rb.AddForce(swimForce * moveRotatedDirection);
            }
        }
        // deceleration control
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
            rb.AddForce(8f * jumpForce * jumpDirection + 3.2f * extraJumpForce * jumpDirection + jumpHeightModifier * jumpForce * transform.up);
        }
        else
        {
            rb.AddForce(jumpForce * jumpDirection + jumpHeightModifier * jumpForce * transform.up);
        }
        extraJumpForce = 0f;
    }
    
    // GRAPPLE!
    private void DoShoot(InputAction.CallbackContext context)
    {
        Debug.Log("Shoot!");

        if (!isTethered)
        {
           
            Vector2 screenCenterPoint = new Vector2(Screen.width / 2f, Screen.height / 2f);
            Ray ray =   Camera.main.ScreenPointToRay(screenCenterPoint);

            if (Physics.Raycast(ray, out RaycastHit hit, grappleDistance, grappleLayer))
            {
                // create grapple joint
                playerObject.AddComponent<SpringJoint>();
                joint = playerObject.GetComponent<SpringJoint>();
                lr = playerObject.GetComponent<LineRenderer>();

                Debug.Log("Grappling!");
                grapplePoint = hit.point;

                joint.autoConfigureConnectedAnchor = false;
                joint.connectedAnchor = grapplePoint;

                float distanceFromPoint = Vector3.Distance(transform.position, grapplePoint);

                joint.maxDistance = distanceFromPoint * maxTolerance;
                joint.minDistance = distanceFromPoint * minTolerance;

                //grapple modifiers
                joint.spring = spring;
                joint.damper = damper;
                joint.massScale = massScale;

                lr.positionCount = 2;

                isTethered = true;
               
                
            }
        }
    }

    void DoStopShoot(InputAction.CallbackContext context)
    {
        lr.positionCount = 0;
        Destroy(joint);

        isTethered = false;
    }

    void DrawTongue()
    {
        if (isTethered)
        {
            // draw grapple
            lr.SetPosition(0, tongueTip.position);
            lr.SetPosition(1, grapplePoint);
        }
        
    }

    //// cursor state methods
    //private void OnApplicationFocus(bool hasFocus)
    //{
    //    SetCursorState(cursorLocked);
    //}

    //private void SetCursorState(bool newState)
    //{
    //    Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
    //    Cursor.visible = true;
    //}
    
    private void DoPause(InputAction.CallbackContext context)
    {

        //GameObject pauseView = viewParent.transform.GetChild(0).gameObject;
        Instance = this;

        if (!isPaused)
        {
            // pause
            
            ViewManager.Instance.Show<PauseView>();
            Cursor.lockState = CursorLockMode.Confined;
            isPaused = true;
        }
        else
        {
            // resume
            ViewManager.Instance.Show<PlayerView>();
            Cursor.lockState = CursorLockMode.Locked;
            isPaused = false;
        }
    }
}
