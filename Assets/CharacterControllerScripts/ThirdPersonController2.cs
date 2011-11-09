using UnityEngine;
using System;

// Require a character controller to be attached to the same game object
[RequireComponent (typeof (CharacterController))]
[AddComponentMenu("Third Person Player/Third Person Controller")]

public class ThirdPersonController2: MonoBehaviour {

	public float walkSpeed = 3.0f; // The speed when walking
	public float trotSpeed = 4.0f; // after trotAfterSeconds of walking we trot with trotSpeed
	public float runSpeed = 6.0f; // when pressing "Fire3" button (cmd) we start running
	
	public float inAirControlAcceleration = 3.0f;
	public float jumpHeight = 0.5f; // How high do we jump when pressing jump and letting go immediately
	public float extraJumpHeight = 2.5f; // We add extraJumpHeight meters on top when holding the button down longer while jumping
	
	public float gravity = 20.0f; // The gravity for the character
	public float speedSmoothing = 10.0f;
	public float rotateSpeed = 500.0f;
	public float trotAfterSeconds = 3.0f;
	
	public bool canJump = true;
	
	private float jumpRepeatTime = 0.05f;
	private float jumpTimeout = 0.15f;
	private float groundedTimeout = 0.25f;
	
	private Vector3 moveDirection = Vector3.zero; // The current move direction in x-z
	private float verticalSpeed = 0.0f; // The current vertical speed
	private float moveSpeed = 0.0f; // The current x-z move speed
	
	private CollisionFlags collisionFlags; // The last collision flags returned from controller.Move
	
	private bool jumping = false; // Are we jumping? (Initiated with jump button and not grounded yet)
	private bool jumpingReachedApex = false;
	
	private bool movingBack = false;  // Are we moving backwards (This locks the camera to not do a 180 degree spin)
	private bool isMoving = false;    // Is the user pressing any keys?
	private float walkTimeStart = 0.0f; // When did the user start walking (Used for going into trot after a while)
	private float lastJumpButtonTime = -10.0f; // Last time the jump button was clicked down
	private float lastJumpTime = -1.0f; // Last time we performed a jump
	
	private float lastJumpStartHeight = 0.0f; // the height we jumped from (Used to determine for how long to apply extra jump power after jumping)
	
	private Vector3 inAirVelocity = Vector3.zero;
	private float lastGroundedTime = 0.0f;
	
	private bool isControllable = true;
	
	private Tilter_MainGame tilterMainGame;
	private SpawnFruits spawnFruits;
	
	private float joyX=0.0f;
	private float joyY=0.0f;
	
	public void Awake () {
		moveDirection = transform.TransformDirection(Vector3.forward); // local space of player's forward -> world space
		tilterMainGame = GameObject.Find("Platform").GetComponent<Tilter_MainGame>();
		spawnFruits = GameObject.Find("All Fruit Spawn Pts").GetComponent<SpawnFruits>();
	}
	
	public void Update() {
		joyX = tilterMainGame.joystickPosition.x;
		joyY = tilterMainGame.joystickPosition.y;
		
		if (!isControllable) { // kill all inputs if not controllable.
			Input.ResetInputAxes();
		}
		//if (Input.GetButtonDown ("Jump")){
		if (tilterMainGame.jump){
			tilterMainGame.jump = false;
			lastJumpButtonTime = Time.time;
		}
		UpdateSmoothedMovementDirection();
		ApplyGravity (); // Apply gravity -- extra power jump modifies gravity
		ApplyJumping (); // Apply jumping logic
		
		// Calculate actual motion
		Vector3 movement = moveDirection * moveSpeed + new Vector3(0, verticalSpeed, 0) + inAirVelocity;
		movement *= Time.deltaTime;
		
		//-------- MOVE THE CONTROLLER ----------
		CharacterController controller = GetComponent<CharacterController>();
		collisionFlags = controller.Move(movement);
		
		// Set rotation to the move direction
		if (IsGrounded()){
			transform.rotation = Quaternion.LookRotation(moveDirection);
		}
		
		// We are in jump mode but just became grounded
		if (IsGrounded()) {
			lastGroundedTime = Time.time;
			inAirVelocity = Vector3.zero;
			if (jumping) {
				jumping = false;
				tilterMainGame.didLand = true;   // hackish...
				SendMessage("DidLand", SendMessageOptions.DontRequireReceiver); // WHY NOT LIKE THE OTHER ANIMATIONS???!!!!!!
			}
		}
	}
	
	private void UpdateSmoothedMovementDirection() {
	
		//Transform cameraTransform = Camera.main.transform;
		bool grounded = IsGrounded();
			
		// Forward vector relative to the camera along the x-z plane
		Vector3 charForward = tilterMainGame.ARCameraForward;
		charForward.y = 0;
		charForward = charForward.normalized;
	
		Vector3 right = new Vector3(charForward.z, 0, -charForward.x); // Right vector relative to the camera -- Always orthogonal to the charForward vector
	
		// Are we moving backwards or looking backwards
		if (joyY < -0.2) movingBack = true;
		else          movingBack = false;
		
		//bool wasMoving = isMoving;
		isMoving = Mathf.Abs (joyX) > 0.1 || Mathf.Abs (joyY) > 0.1;
			
		// Target direction (direction the character constantly interpolates to) relative to the camera
		Vector3 targetDirection = joyX * right + joyY * charForward;
		
		// Grounded controls
		if (grounded) {
			// We store speed and direction seperately, so that when the character stands still we still have 
			// a valid forward direction moveDirection is always normalized, and we only update it if there is user input.
			if (targetDirection != Vector3.zero) {
				// If we are really slow, just snap to the target direction
				if (moveSpeed < walkSpeed * 0.9 && grounded){
					moveDirection = targetDirection.normalized;
				}
				// Otherwise smoothly turn towards it
				else {
					moveDirection = Vector3.RotateTowards(moveDirection, targetDirection, rotateSpeed * Mathf.Deg2Rad * Time.deltaTime, 1000);
					moveDirection = moveDirection.normalized;
				}
			}
			
			float curSmooth = speedSmoothing * Time.deltaTime; // Smooth the speed based on the current target direction
			
			// Choose target speed
			//* We want to support analog input but make sure you can't walk faster diagonally than just forward or sideways
			float targetSpeed = Mathf.Min(targetDirection.magnitude, 1.0f);
		
			if (Input.GetButton ("Fire3")) { // Pick speed modifier
				targetSpeed *= runSpeed;
			}
			else if (Time.time - trotAfterSeconds > walkTimeStart) {
				targetSpeed *= trotSpeed;
			}
			else {
				targetSpeed *= walkSpeed;
			}
			
			moveSpeed = Mathf.Lerp(moveSpeed, targetSpeed, curSmooth);
			
			if (moveSpeed < walkSpeed * 0.3) // Reset walk time start when we slow down
				walkTimeStart = Time.time;
		}
		// In air controls
		else {
			if (isMoving)
				inAirVelocity += targetDirection.normalized * Time.deltaTime * inAirControlAcceleration;
		}
		
	}
	
	// This next function responds to the "HidePlayer" message by hiding the player. 
	// The message is also 'replied to' by identically-named functions in the collision-handling scripts.
	// - Used by the LevelStatus script when the level completed animation is triggered.
	private void HidePlayer() {
		GameObject.Find("Raccoon_Low").GetComponent<SkinnedMeshRenderer>().enabled = false; // stop rendering the player.
		GameObject.Find("pSphere6").GetComponent<MeshRenderer>().enabled = false; // stop rendering the right eye.
		GameObject.Find("pSphere7").GetComponent<MeshRenderer>().enabled = false; // stop rendering the left eye.
		isControllable = false;	// disable player controls.
	}
	
	// This is a complementary function to the above. We don't use it in the tutorial, but it's included for
	// the sake of completeness. (I like orthogonal APIs; so sue me!)
	private void ShowPlayer() {
		GameObject.Find("Raccoon_Low").GetComponent<SkinnedMeshRenderer>().enabled = true; // start rendering the player again.
		GameObject.Find("pSphere6").GetComponent<MeshRenderer>().enabled = true; // start rendering the right eye.
		GameObject.Find("pSphere7").GetComponent<MeshRenderer>().enabled = true; // start rendering the left eye.
		isControllable = true;	// allow player to control the character again.
	}
	
	private void ApplyJumping () {
		if (lastJumpTime + jumpRepeatTime > Time.time) // Prevent jumping too fast after each other
			return;
	
		if (IsGrounded()) {
			// Jump
			// - Only when pressing the button down
			// - With a timeout so you can press the button slightly before landing		
			if (canJump && Time.time < lastJumpButtonTime + jumpTimeout) {
				verticalSpeed = CalculateJumpVerticalSpeed (jumpHeight);
				SendMessage("DidJump", SendMessageOptions.DontRequireReceiver);
			}
		}
	}
	
	private void ApplyGravity () {
		if (isControllable) {	// don't move player at all if not controllable.
			bool jumpButton = Input.GetButton("Jump");
			
			// When we reach the apex of the jump we send out a message
			if (jumping && !jumpingReachedApex && verticalSpeed <= 0.0) {
				jumpingReachedApex = true;
				SendMessage("DidJumpReachApex", SendMessageOptions.DontRequireReceiver);
			}
		
			// * When jumping up we don't apply gravity for some time when the user is holding the jump button
			//   This gives more control over jump height by pressing the button longer
			bool extraPowerJump =  IsJumping () && verticalSpeed > 0.0 && jumpButton && transform.position.y < lastJumpStartHeight + extraJumpHeight;
			
			if (extraPowerJump)
				return;
			else if (IsGrounded ())
				verticalSpeed = 0.0f;
			else
				verticalSpeed -= gravity * Time.deltaTime;
		}
	}
	
	private float CalculateJumpVerticalSpeed(float targetJumpHeight) {
		// From the jump height and gravity we deduce the upwards speed for the character to reach at the apex.
		return Mathf.Sqrt(2 * targetJumpHeight * gravity);
	}
	
	private void DidJump () {
		jumping = true;
		jumpingReachedApex = false;
		lastJumpTime = Time.time;
		lastJumpStartHeight = transform.position.y;
		lastJumpButtonTime = -10;
	}
	
	void OnControllerColliderHit (ControllerColliderHit hit){
	//	Debug.DrawRay(hit.point, hit.normal);
		if(hit.gameObject.tag == "Fruit") {
			Rigidbody f = hit.collider.attachedRigidbody;
			spawnFruits.DestroyFruit(f);
			tilterMainGame.collectedFruits++;
		}
		if (hit.moveDirection.y > 0.01)
			return;
	}
	
	public float GetSpeed () {
		return moveSpeed;
	}
	
	public bool IsJumping () {
		return jumping;
	}
	
	public bool IsGrounded () {
		return (collisionFlags & CollisionFlags.CollidedBelow) != 0;
	}
	
	void SuperJump (float height){
		verticalSpeed = CalculateJumpVerticalSpeed (height);
		collisionFlags = CollisionFlags.None;
		SendMessage("DidJump", SendMessageOptions.DontRequireReceiver);
	}
	
	void SuperJump (float height, Vector3 jumpVelocity) {
		verticalSpeed = CalculateJumpVerticalSpeed (height);
		inAirVelocity = jumpVelocity;
		
		collisionFlags = CollisionFlags.None;
		SendMessage("DidJump", SendMessageOptions.DontRequireReceiver);
	}
	
	public Vector3 GetDirection () {
		return moveDirection;
	}
	
	public bool IsMovingBackwards () {
		return movingBack;
	}
	
	public bool IsMoving() {
		return Mathf.Abs(joyX) + Mathf.Abs(joyY) > 0.5;
	}
	
	public bool HasJumpReachedApex () {
		return jumpingReachedApex;
	}
	
	public bool IsGroundedWithTimeout () {
		return lastGroundedTime + groundedTimeout > Time.time;
	}
	
	void Reset () {
		gameObject.tag = "Player";
	}
}
