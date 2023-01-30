# PhraGuy
3d Physics-movement gameplay with procedurally animated character

## Background
This was a side project attempted by myself and a friend that took place over 6 weeks. The goal was to create a 3D multiplayer racing game where players run, jump, and swing through an obstacle course competing for first place. As we began to implement features, development began to stall we halted the project for a few reasons: we were a 2-person team, neither of us had experience developing multiplayer games, nor were we proficient digital artists. We concluded that we had slim chances of creating a successful game of this scale given our current level of experience and halted development.

My role in the project consisted of developing player & camera controller scripts, player animations scripts, UI, as well as collaborated with my partner to develop the multiplayer integration and asset generation. 

![](https://github.com/TognaBologna09/PhraGuy/blob/main/PhraGuy_ScreenCap.png)

### Player & Camera Controllers
The player movement is based on the Unity Physics system so the movement is completely force-based. The player can run, jump, and swing with all of the functions in one script. This was a choice made to simplify the swinging mechanic when converting the game to multiplayer. The result is a script that can be executed with mouse/keyboard controls or with a gamepad, since it uses Unity's new Input System which simply facilitates the translation of inputs from one input-scheme to another. It is a designer friendly script with public parameters that can be altered within the Unity Editor.


Unity makes working with quaternions simple, and so I used them to align the player's movement with the 3D angle of the camera. 
```
       
        Quaternion cameraDirection = Quaternion.Euler(0f, CinemachineCamera.m_XAxis.Value, 0f);
        moveRotatedDirection = cameraDirection * newDirection;
        moveRotatedDirection = new Vector3(moveRotatedDirection.x, 0f, moveRotatedDirection.z).normalized;

```

The result of all the movement scripts is seen below in this gif: The player can run, jump, and cast the frog tongue out to swing towards objects. 

![](https://github.com/TognaBologna09/PhraGuy/blob/main/PhrogGameplayGif.gif)

### Player Animations
The player is animated in real-time. The script to control the player animations was made to be designer-friendly, such that the animations could be tuned with parameters instead of relying on 3d animation. The ragdoll limbs were set to have limited strength to point to the goal position, such that they comically flop around as the players move. I found that increasing the spring-strength of the ragdoll limb helped to move the limb directly to the position, but made the animations look rigid and bland. If I had more ability to design 3D bone structures in Blender, the procedural animations may have been able to look better but considering the time spent on the project I opted not to dwell on this problem and made the limbs flail the way you see them in the gif below.   

![](https://github.com/TognaBologna09/PhraGuy/blob/main/ProceduralAnimationFrogDemo.gif)

