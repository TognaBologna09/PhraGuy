# PhraGuy
3d Physics-movement gameplay with procedurally animated character

## Background
This was a side project attempted by myself and a friend that took place over 6 weeks. The goal was to create a 3D multiplayer racing game where players run, jump, and swing through an obstacle course competing for first place. As we began to implement features, development began to stall we halted the project for a few reasons: we were a 2-person team, neither of us had experience developing multiplayer games, nor were we proficient digital artists. We concluded that we had slim chances of creating a successful game of this scale given our current level of experience and halted development.

My role in the project consisted of developing player & camera controller scripts, player animations scripts, UI, as well as collaborated with my partner to develop the multiplayer integration and asset generation. 

![](https://github.com/TognaBologna09/PhraGuy/blob/main/PhraGuy_ScreenCap.png)

### Player & Camera Controllers
The player movement is based on the Unity Physics system so the movement is completely force-based. The player can run, jump, and swing with all of the functions in one script. This was a choice made to simplify the swinging mechanic when converting the game to multiplayer. The result is a script that can be executed with mouse/keyboard controls or with a gamepad, since it uses Unity's new Input System which simply facilitates the translation of inputs from one input-scheme to another. It is a designer friendly script with public parameters that can be altered within the Unity Editor.


Here is a method from the character controller script called MoveBody that takes a Vector2 input to dictate the movement. 
```
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
```

The result of all the movement scripts is seen below in this gif: The player can run, jump, and cast the frog tongue out to swing towards objects. 

![](https://github.com/TognaBologna09/PhraGuy/blob/main/PhrogGameplayGif.gif)

### Player Animations
The player is animated in real-time. The script to control the player animations was made to be designer-friendly, such that the animations could be tuned with parameters instead of relying on 3d animation. 
![](https://github.com/TognaBologna09/PhraGuy/blob/main/ProceduralAnimationFrogDemo.gif)

