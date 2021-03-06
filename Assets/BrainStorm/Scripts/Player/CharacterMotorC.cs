﻿using UnityEngine;
using System.Collections;

//#pragma strict
//#pragma implicit
//#pragma downcast

// Require a character controller to be attached to the same game object
// I had to initally comment out the following RequireComponent
// line to make it compile and allow attaching to my Main Camera;
// Uncommenting afterwards worked fine.
// -- zephjc
[RequireComponent (typeof (CharacterController))]

//RequireComponent (CharacterMotor)
[AddComponentMenu("Character/Character Motor C")]
//@script AddComponentMenu ("Character/FPS Input Controller")


public class CharacterMotorC : MonoBehaviour {
	[System.Serializable]
	public enum MovementTransferOnJump {
		None, // The jump is not affected by velocity of floor at all.
		InitTransfer, // Jump gets its initial velocity from the floor, then gradualy comes to a stop.
		PermaTransfer, // Jump gets its initial velocity from the floor, and keeps that velocity until landing.
		PermaLocked // Jump is relative to the movement of the last touched floor and will move together with that floor.
	}
	
	[System.Serializable]
	public class CharacterMotorMovingPlatform {
		public bool enabled = true;
		public MovementTransferOnJump movementTransfer = MovementTransferOnJump.PermaTransfer;
		[System.NonSerialized]
		public Transform hitPlatform;
		[System.NonSerialized]
		public Transform activePlatform;
		[System.NonSerialized]
		public Vector3 activeLocalPoint;
		[System.NonSerialized]
		public Vector3 activeGlobalPoint;
		[System.NonSerialized]
		public Quaternion activeLocalRotation;
		[System.NonSerialized]
		public Quaternion activeGlobalRotation;
		[System.NonSerialized]
		public Matrix4x4 lastMatrix;
		[System.NonSerialized]
		public Vector3 platformVelocity;
		[System.NonSerialized]
		public bool newPlatform;
	}
	
	[System.Serializable]
	public class CharacterMotorMovement {
		// The maximum horizontal speed when moving
		public float maxForwardSpeed = 10.0f;
		public float maxSidewaysSpeed = 10.0f;
		public float maxBackwardsSpeed = 10.0f;
		// Curve for multiplying speed based on slope (negative = downwards)
		public AnimationCurve slopeSpeedMultiplier = new AnimationCurve(new Keyframe(-90, 1), new Keyframe(0, 1), new Keyframe(90, 0));
		// How fast does the character change speeds?  Higher is faster.
		public float maxGroundAcceleration = 30.0f;
		public float maxAirAcceleration = 20.0f;
		// The gravity for the character
		public float gravity = 10.0f;
		public float maxFallSpeed = 20.0f;
		public float airControlFactor = 0.4f;
		// For the next variables, [System.NonSerialized] tells Unity to not serialize the variable or show it in the inspector view.
		// Very handy for organization!
		// The last collision flags returned from controller.Move
		[System.NonSerialized]
		public CollisionFlags collisionFlags;
		// We will keep track of the character's current velocity,
		[System.NonSerialized]
		public Vector3 velocity;
		// This keeps track of our current velocity while we're not grounded
		[System.NonSerialized]
		public Vector3 frameVelocity = Vector3.zero;
		[System.NonSerialized]
		public Vector3 hitPoint = Vector3.zero;
		[System.NonSerialized]
		public Vector3 lastHitPoint = new Vector3(Mathf.Infinity, 0, 0);
	}
	
	[System.Serializable]
	public class CharacterMotorSprint {
		// Can we sprint?
		public bool enabled = true;
		// Top speed for sprinting
		public float sprintSpeed = 20f;
		// Acceleration
		public float sprintAccel = 60f;
		// How long can we sprint for
		public float sprintLength = 4f;
		// How quickly do we recover?
		public float recoveryRate = 1f;
		// How long do we ignore sequential sprint button presses for?
		public float cooldown = 0.5f;
		// Are we sprinting now?
		//[System.NonSerialized]
		public bool sprinting;
		// When was the last time the sprint button was pressed?
		public float lastStartTime {get; set;}
		// How much sprint time is remaining
		public float stamina {get; set;}
		public float stamina01 {get { return stamina / sprintLength; } }
	}
	
	// We will contain all the jumping related variables in one helper class for clarity.
	
	[System.Serializable]
	public class CharacterMotorJumping {
		// Can the character jump?
		public bool enabled = true;
		// How high do we jump when pressing jump and letting go immediately
		public float baseHeight = 1.0f;
		public bool extraJumpEnabled = true;
		// We add extraHeight units (meters) on top when holding the button down longer while jumping
		public float extraHeight = 4.1f;
		// How much does the character jump out perpendicular to the surface on walkable surfaces?
		// 0 means a fully vertical jump and 1 means fully perpendicular.
		public float perpAmount = 0.0f;
		// How much does the character jump out perpendicular to the surface on too steep surfaces?
		// 0 means a fully vertical jump and 1 means fully perpendicular.
		public float steepPerpAmount = 0.5f;
		// For the next variables, [System.NonSerialized] tells Unity to not serialize the variable or show it in the inspector view.
		// Very handy for organization!
		// Are we jumping? (Initiated with jump button and not grounded yet)
		// To see if we are just in the air (initiated by jumping OR falling) see the grounded variable.
		
		public bool superJump = false;
		public float superJumpHeight = 20.0f;
		
		[System.NonSerialized]
		public bool jumping = false;
		[System.NonSerialized]
		public bool holdingJumpButton = false;
		// the time we jumped at (Used to determine for how long to apply extra jump power after jumping.)
		[System.NonSerialized]
		public float lastStartTime = 0.0f;
		[System.NonSerialized]
		public float lastButtonDownTime = -100f;
		[System.NonSerialized]
		public Vector3 jumpDir = Vector3.up;
	}
	
	[System.Serializable]
	public class CharacterMotorJetpack {
		// Can we use a jetpack?
		public bool enabled = false;
		// How quickly do we reach max vertical speed?
		public float verticalAccel = 1f;
		// What's the fastest upwards we can go?
		public float maxVerticalSpeed = 1f;
		// How long does the jetpack last in seconds?
		public float maxJetpackFuel = 4f;
		// How quickly does the jetpack recharge?
		public float rechargeRate = 1f;
		// How long until we can use it again?
		public float cooldown = 1f;
		// is the jetpack in use right now?
		public bool jetpacking {get; set;}
		// the time we started jetpacking
		public float lastStartTime {get; set;}
		// how much time of jetpacking is remaining
		public float fuel {get; set;}
		public float fuel01 {get { return fuel/maxJetpackFuel; } }
	}
	
	[System.Serializable]
	public class CharacterMotorDashpack {
		// can we use the dashpack?
		public bool enabled = false;
		// How quickly do we reach max speed?
		public float accel = 1f;
		// How quickly do we stop falling (with zero directional input)?
		public float verticalAccel = 1f;
		// How quickly can we go?
		public float maxSpeed = 20f;
		// How long does the jetpack last in seconds?
		public float maxDashpackFuel = 4f;
		// How quickly does the jetpack recharge?
		public float rechargeRate = 1f;
		// How long until we can use it again?
		public float cooldown = 1f;
		// is the jetpack in use right now?
		public bool dashpacking { get; set; }
		// the time we started jetpacking
		public float lastStartTime { get; set; }
		// how much time of jetpacking is remaining
		public float fuel { get; set; }
		public float fuel01 { get {return fuel/maxDashpackFuel; } }
		// dashpack momentum
		public Vector3 dash { get; set; }
	}
	
	[System.Serializable]
	public class CharacterMotorSliding {
		// Does the character slide on too steep surfaces?
		public bool enabled = true;
		
		// How fast does the character slide on steep surfaces?
		public float slidingSpeed = 15f;
		
		// How much can the player control the sliding direction?
		// If the value is 0.5 the player can slide sideways with half the speed of the downwards sliding speed.
		public float sidewaysControl = 1.0f;
		
		// How much can the player influence the sliding speed?
		// If the value is 0.5 the player can speed the sliding up to 150% or slow it down to 50%.
		public float speedControl = 0.4f;
	}
	
	
	// Does this script currently respond to input?
	public bool canControl = true;
	
	public bool useFixedUpdate = true;
	
	// For the next variables, [System.NonSerialized] tells Unity to not serialize the variable or show it in the inspector view.
	// Very handy for organization!
	
	// The current global direction we want the character to move in.
	[System.NonSerialized]
	public Vector3 inputMoveDirection = Vector3.zero;
	
	// the current global direction the character is looking
	// used to determine dash jetpack direction
	[System.NonSerialized]
	public Vector3 inputLookDirection = Vector3.zero;
	
	// Is the jump button held down? We use this interface instead of checking
	// for the jump button directly so this script can also be used by AIs.
	[System.NonSerialized]
	public bool inputJump = false;
	
	[System.NonSerialized]
	// Is the sprint button held down?
	public bool inputSprint = false;
	
	public CharacterMotorMovement movement = new CharacterMotorMovement();
	public CharacterMotorSprint sprint = new CharacterMotorSprint();
	public CharacterMotorJumping jumping = new CharacterMotorJumping();
	public CharacterMotorJetpack jetpack = new CharacterMotorJetpack();
	public CharacterMotorDashpack dashpack = new CharacterMotorDashpack();
	public CharacterMotorMovingPlatform movingPlatform = new CharacterMotorMovingPlatform();
	public CharacterMotorSliding sliding = new CharacterMotorSliding();

	private bool grounded = true;
	private bool kickback = false;
	private Vector3 kickbackVector = Vector3.zero;
	
	[System.NonSerialized]
	Vector3 groundNormal = Vector3.zero;
	
	private Vector3 lastGroundNormal = Vector3.zero;
	private Transform tr;
	private CharacterController controller;
	
	void Awake () {
		controller = GetComponent <CharacterController>();
		tr = transform;
		sprint.stamina = sprint.sprintLength;
		jetpack.fuel = jetpack.maxJetpackFuel;
		dashpack.fuel = dashpack.maxDashpackFuel;
	}
	
	public void Kickback(Vector3 vector) {
		kickbackVector = vector;
		kickback = true;
	}
	
	void UpdateFunction () {
		// We copy the actual velocity into a temporary variable that we can manipulate.
		Vector3 velocity = movement.velocity;
		// Update velocity based on input
		velocity = ApplyInputVelocityChange(velocity);
		// Apply gravity and jumping force
		velocity = ApplyGravityAndJumping (velocity);
		// Apply jetpack force
		velocity = ApplyJetpack(velocity);
		// Apply dashpack force
		velocity = ApplyDashpack(velocity);
		
		// Moving platform support
		Vector3 moveDistance = Vector3.zero;
		
		if (MoveWithPlatform()) {
			Vector3 newGlobalPoint = movingPlatform.activePlatform.TransformPoint(movingPlatform.activeLocalPoint);
			moveDistance = (newGlobalPoint - movingPlatform.activeGlobalPoint);
			if (moveDistance != Vector3.zero)
				controller.Move(moveDistance);
			// Support moving platform rotation as well:
			Quaternion newGlobalRotation = movingPlatform.activePlatform.rotation * movingPlatform.activeLocalRotation;
			Quaternion rotationDiff = newGlobalRotation * Quaternion.Inverse(movingPlatform.activeGlobalRotation);
			float yRotation = rotationDiff.eulerAngles.y;
			if (yRotation != 0) {
				// Prevent rotation of the local up vector
				tr.Rotate(0, yRotation, 0);
			}
		}
		// Save lastPosition for velocity calculation.
		Vector3 lastPosition = tr.position;
		// We always want the movement to be framerate independent.  Multiplying by Time.deltaTime does this.
		Vector3 currentMovementOffset = velocity * Time.deltaTime;
		// Find out how much we need to push towards the ground to avoid loosing grouning
		// when walking down a step or over a sharp change in slope.
		float mathf1 = controller.stepOffset;
		float mathf2 = new Vector3(currentMovementOffset.x, 0, currentMovementOffset.z).magnitude;
		float pushDownOffset = Mathf.Max(mathf1, mathf2);
		if (grounded)
			currentMovementOffset -= pushDownOffset * Vector3.up;
		// Reset variables that will be set by collision function
		movingPlatform.hitPlatform = null;
		groundNormal = Vector3.zero;
		
		Vector3 kick = Vector3.zero;
		if (kickback) {
			kickback = false;
			kick = kickbackVector;
			if (!grounded) {
				kick *= 0.125f;
			}
		}
		
		// Move our character!
		movement.collisionFlags = controller.Move (currentMovementOffset + kick);
		movement.lastHitPoint = movement.hitPoint;
		lastGroundNormal = groundNormal;
		if (movingPlatform.enabled && movingPlatform.activePlatform != movingPlatform.hitPlatform) {
			if (movingPlatform.hitPlatform != null) {
				movingPlatform.activePlatform = movingPlatform.hitPlatform;
				movingPlatform.lastMatrix = movingPlatform.hitPlatform.localToWorldMatrix;
				movingPlatform.newPlatform = true;
			}
		}
		// Calculate the velocity based on the current and previous position.
		// This means our velocity will only be the amount the character actually moved as a result of collisions.
		Vector3 oldHVelocity = new Vector3(velocity.x, 0, velocity.z);
		movement.velocity = (tr.position - lastPosition) / Time.deltaTime;
		Vector3 newHVelocity = new Vector3(movement.velocity.x, 0, movement.velocity.z);
		// The CharacterController can be moved in unwanted directions when colliding with things.
		// We want to prevent this from influencing the recorded velocity.
		if (oldHVelocity == Vector3.zero) {
			movement.velocity = new Vector3(0, movement.velocity.y, 0);
		}
		else {
			float projectedNewVelocity = Vector3.Dot(newHVelocity, oldHVelocity) / oldHVelocity.sqrMagnitude;
			movement.velocity = oldHVelocity * Mathf.Clamp01(projectedNewVelocity) + movement.velocity.y * Vector3.up;
		}
		if (movement.velocity.y < velocity.y - 0.001) {
			if (movement.velocity.y < 0) {
				// Something is forcing the CharacterController down faster than it should.
				// Ignore this
				movement.velocity.y = velocity.y;
			}
			else {
				// The upwards movement of the CharacterController has been blocked.
				// This is treated like a ceiling collision - stop further jumping here.
				jumping.holdingJumpButton = false;
			}
		}
		// We were grounded but just loosed grounding
		if (grounded && !IsGroundedTest()) {
			grounded = false;
			// Apply inertia from platform
			if (movingPlatform.enabled &&
			    (movingPlatform.movementTransfer == MovementTransferOnJump.InitTransfer ||
			 movingPlatform.movementTransfer == MovementTransferOnJump.PermaTransfer)) {
				movement.frameVelocity = movingPlatform.platformVelocity;
				movement.velocity += movingPlatform.platformVelocity;
			}
			SendMessage("OnFall", SendMessageOptions.DontRequireReceiver);
			// We pushed the character down to ensure it would stay on the ground if there was any.
			// But there wasn't so now we cancel the downwards offset to make the fall smoother.
			tr.position += pushDownOffset * Vector3.up;
		}
		// We were not grounded but just landed on something
		else if (!grounded && IsGroundedTest()) {
			grounded = true;
			jumping.jumping = false;
			
			if (jetpack.jetpacking) {
				BroadcastMessage("OnJetpackStop", SendMessageOptions.DontRequireReceiver);
			}
			jetpack.jetpacking = false;
			
			if (dashpack.dashpacking) {
				BroadcastMessage("OnDashpackStop", SendMessageOptions.DontRequireReceiver);
			}
			dashpack.dashpacking = false;
			
			SubtractNewPlatformVelocity();
			SendMessage("OnLand", SendMessageOptions.DontRequireReceiver);
		}
		// Moving platforms support
		if (MoveWithPlatform()) {
			// Use the center of the lower half sphere of the capsule as reference point.
			// This works best when the character is standing on moving tilting platforms.
			//movingPlatform.activeGlobalPoint = tr.position +
			//float thisstuff = (float)(controller.center.y - controller.height*0.5 + controller.radius);
			//Vector3 testthis = Vector3.up * thisstuff;
			//movingPlatform.activeGlobalPoint *= (controller.center.y - controller.height*0.5 + controller.radius);
			movingPlatform.activeLocalPoint = movingPlatform.activePlatform.InverseTransformPoint(movingPlatform.activeGlobalPoint);
			// Support moving platform rotation as well:
			movingPlatform.activeGlobalRotation = tr.rotation;
			movingPlatform.activeLocalRotation = Quaternion.Inverse(movingPlatform.activePlatform.rotation) * movingPlatform.activeGlobalRotation;
		}

	}

	void FixedUpdate () {
		if (movingPlatform.enabled) {
			if (movingPlatform.activePlatform != null) {
				if (!movingPlatform.newPlatform) {
					//Vector3 lastVelocity = movingPlatform.platformVelocity;
					movingPlatform.platformVelocity = (
						movingPlatform.activePlatform.localToWorldMatrix.MultiplyPoint3x4(movingPlatform.activeLocalPoint)
						- movingPlatform.lastMatrix.MultiplyPoint3x4(movingPlatform.activeLocalPoint)
						) / Time.deltaTime;
				}
				movingPlatform.lastMatrix = movingPlatform.activePlatform.localToWorldMatrix;
				movingPlatform.newPlatform = false;
			}
			else {
				movingPlatform.platformVelocity = Vector3.zero;
			}
		}
		if (useFixedUpdate)
			UpdateFunction();
	}
	
	void Update () {
		if (!useFixedUpdate)
			UpdateFunction();
	}
	
	private Vector3 ApplyInputVelocityChange (Vector3 velocity) {
		if (!canControl)
			inputMoveDirection = Vector3.zero;
		
		// First, check if we're sprinting
		if (sprint.enabled) {
			if (inputSprint && !sprint.sprinting && canSprint) {
				sprint.sprinting = true;
				sprint.lastStartTime = Time.time;
				BroadcastMessage("OnSprintStart",SendMessageOptions.DontRequireReceiver);
			}
			if (sprint.sprinting) {
				if (sprint.stamina > 0f && inputSprint) {
					sprint.stamina -= Time.deltaTime * movement.velocity.magnitude * 0.125f;
					// sprint is applied in MaxSpeedInDirection()
				}
				else { // sprint button release or ran out of stamina
					sprint.sprinting = false;
					BroadcastMessage("OnSprintStop", SendMessageOptions.DontRequireReceiver);
				}
			}
			else {
				sprint.stamina += sprint.recoveryRate * Time.deltaTime;
				sprint.stamina = Mathf.Min(sprint.stamina, sprint.sprintLength);
			}
		}
		
		// Find desired velocity
		Vector3 desiredVelocity;
		if (grounded && TooSteep()) {
			// The direction we're sliding in
			desiredVelocity = new Vector3(groundNormal.x, 0, groundNormal.z).normalized;
			// Find the input movement direction projected onto the sliding direction
			Vector3 projectedMoveDir = Vector3.Project(inputMoveDirection, desiredVelocity);
			// Add the sliding direction, the spped control, and the sideways control vectors
			desiredVelocity = desiredVelocity + projectedMoveDir * sliding.speedControl + (inputMoveDirection - projectedMoveDir) * sliding.sidewaysControl;
			// Multiply with the sliding speed
			desiredVelocity *= sliding.slidingSpeed;
		}
		else
			desiredVelocity = GetDesiredHorizontalVelocity();
		if (movingPlatform.enabled && movingPlatform.movementTransfer == MovementTransferOnJump.PermaTransfer) {
			desiredVelocity += movement.frameVelocity;
			desiredVelocity.y = 0;
		}
		if (grounded)
			desiredVelocity = AdjustGroundVelocityToNormal(desiredVelocity, groundNormal);
		else
			velocity.y = 0;
		// Enforce max velocity change
		float maxVelocityChange = GetMaxAcceleration(grounded) * Time.deltaTime;
		Vector3 velocityChangeVector = (desiredVelocity - velocity);
		if (velocityChangeVector.sqrMagnitude > maxVelocityChange * maxVelocityChange) {
			velocityChangeVector = velocityChangeVector.normalized * maxVelocityChange;
		}
		// If we're in the air and don't have control, don't apply any velocity change at all.
		// If we're on the ground and don't have control we do apply it - it will correspond to friction.
		if (grounded || dashpack.dashpacking || jetpack.jetpacking)
			velocity += velocityChangeVector;
		else 
			velocity += velocityChangeVector * movement.airControlFactor;// reduced control while falling
		if (grounded) {
			// When going uphill, the CharacterController will automatically move up by the needed amount.
			// Not moving it upwards manually prevent risk of lifting off from the ground.
			// When going downhill, DO move down manually, as gravity is not enough on steep hills.
			velocity.y = Mathf.Min(velocity.y, 0);
		}
		return velocity;
	}
	
	private Vector3 ApplyGravityAndJumping (Vector3 velocity) {
		if (!inputJump || !canControl) {
			jumping.holdingJumpButton = false;
			jumping.lastButtonDownTime = -100;
		}
		if (inputJump && jumping.lastButtonDownTime < 0 && canControl)
			jumping.lastButtonDownTime = Time.time;
		if (grounded)
			velocity.y = Mathf.Min(0, velocity.y) - movement.gravity * Time.deltaTime;
		else {
			velocity.y = movement.velocity.y - movement.gravity * Time.deltaTime;
			// When jumping up we don't apply gravity for some time when the user is holding the jump button.
			// This gives more control over jump height by pressing the button longer.
			if (jumping.jumping && jumping.holdingJumpButton) {
				// Calculate the duration that the extra jump force should have effect.
				// If we're still less than that duration after the jumping time, apply the force.
				if (jumping.extraJumpEnabled && Time.time < jumping.lastStartTime + jumping.extraHeight / CalculateJumpVerticalSpeed(jumping.baseHeight)) {
					// Negate the gravity we just applied, except we push in jumpDir rather than jump upwards.
					velocity += jumping.jumpDir * movement.gravity * Time.deltaTime;
				}
			}
			
			// Make sure we don't fall any faster than maxFallSpeed. This gives our character a terminal velocity.
			velocity.y = Mathf.Max (velocity.y, -movement.maxFallSpeed);
		}
		if (grounded) {
			// Jump only if the jump button was pressed down in the last 0.2 seconds.
			// We use this check instead of checking if it's pressed down right now
			// because players will often try to jump in the exact moment when hitting the ground after a jump
			// and if they hit the button a fraction of a second too soon and no new jump happens as a consequence,
			// it's confusing and it feels like the game is buggy.
			if (jumping.enabled && canControl && (Time.time - jumping.lastButtonDownTime < 0.2)) {
				grounded = false;
				jumping.jumping = true;
				jumping.lastStartTime = Time.time;
				jumping.lastButtonDownTime = -100;
				jumping.holdingJumpButton = true;
				// Calculate the jumping direction
				if (TooSteep())
					jumping.jumpDir = Vector3.Slerp(Vector3.up, groundNormal, jumping.steepPerpAmount);
				else
					jumping.jumpDir = Vector3.Slerp(Vector3.up, groundNormal, jumping.perpAmount);
				
				// use superJump if its enabled
				float jumpHeight = jumping.superJump ? jumping.superJumpHeight : jumping.baseHeight;
				// Apply the jumping force to the velocity. Cancel any vertical velocity first.
				velocity.y = 0;
				velocity += jumping.jumpDir * CalculateJumpVerticalSpeed (jumpHeight);
				// Apply inertia from platform
				if (movingPlatform.enabled &&
				    (movingPlatform.movementTransfer == MovementTransferOnJump.InitTransfer ||
				 movingPlatform.movementTransfer == MovementTransferOnJump.PermaTransfer)) {
					movement.frameVelocity = movingPlatform.platformVelocity;
					velocity += movingPlatform.platformVelocity;
					
				}
				SendMessage("OnJump", SendMessageOptions.DontRequireReceiver);
			}
			else {
				jumping.holdingJumpButton = false;
			}
		}

		
		return velocity;
	}
	
	private Vector3 ApplyJetpack(Vector3 velocity) {
		// if we're falling and the inputJump button is held true
		if (jetpack.enabled) {
			// if jump button held true and we're falling and not jetpacking and jetpack wasn't recently activated
			if (inputJump && velocity.y < 0f && !jetpack.jetpacking && canJetpack) {
				jetpack.jetpacking = true;
				jetpack.lastStartTime = Time.time;
				BroadcastMessage("OnJetpackStart", SendMessageOptions.DontRequireReceiver);
			}
			if (jetpack.jetpacking) {
				if (jetpack.fuel > 0f && inputJump) {
					jetpack.fuel -= Time.deltaTime;
					velocity.y += jetpack.verticalAccel * Time.deltaTime;
					velocity.y = Mathf.Min(velocity.y, jetpack.maxVerticalSpeed);
				}
				else { // jump button released or ran out of fuel
					jetpack.jetpacking = false;
					BroadcastMessage("OnJetpackStop", SendMessageOptions.DontRequireReceiver);
				}
			}
			else {
				jetpack.fuel += jetpack.rechargeRate * Time.deltaTime;
				jetpack.fuel = Mathf.Min(jetpack.fuel, jetpack.maxJetpackFuel);
			}
		}
		
		return velocity;
	}
	
	private Vector3 ApplyDashpack(Vector3 velocity) {
		if (dashpack.enabled) {
			// if jump button held down and we're not already dashpacking
			// and we're able to dashpack
			if (inputJump && !dashpack.dashpacking && canDashpack) {
				dashpack.dashpacking = true;
				dashpack.lastStartTime = Time.time;
				BroadcastMessage("OnDashpackStart", SendMessageOptions.DontRequireReceiver);
			}
			if (dashpack.dashpacking) {
				if (dashpack.fuel > 0f && inputJump) {
					dashpack.fuel -= Time.deltaTime;
					// hold steady vertical while dashpacking
					
					if (velocity.y < 0f) 
						velocity.y += dashpack.verticalAccel * Time.deltaTime;
					// dash in look direction input
					if (velocity.magnitude < dashpack.maxSpeed) 
						velocity += inputLookDirection.normalized * dashpack.accel * Time.deltaTime;
				}
				else {
					dashpack.dashpacking = false;
					BroadcastMessage("OnDashpackStop", SendMessageOptions.DontRequireReceiver);
				}
			}
			else {
				dashpack.fuel += dashpack.rechargeRate * Time.deltaTime;
				dashpack.fuel = Mathf.Min(dashpack.fuel, dashpack.maxDashpackFuel);
				dashpack.dash = Vector3.zero;
			}
		}
		
		return velocity;
	}
	
	void OnControllerColliderHit (ControllerColliderHit hit) {
		if (hit.normal.y > 0 && hit.normal.y > groundNormal.y && hit.moveDirection.y < 0) {
			if ((hit.point - movement.lastHitPoint).sqrMagnitude > 0.001 || lastGroundNormal == Vector3.zero)
				groundNormal = hit.normal;
			else
				groundNormal = lastGroundNormal;
			movingPlatform.hitPlatform = hit.collider.transform;
			movement.hitPoint = hit.point;
			movement.frameVelocity = Vector3.zero;
		}
	}
	
	private IEnumerator SubtractNewPlatformVelocity () {
		// When landing, subtract the velocity of the new ground from the character's velocity
		// since movement in ground is relative to the movement of the ground.
		if (movingPlatform.enabled &&
		    (movingPlatform.movementTransfer == MovementTransferOnJump.InitTransfer ||
		 movingPlatform.movementTransfer == MovementTransferOnJump.PermaTransfer)) {
			// If we landed on a new platform, we have to wait for two FixedUpdates
			// before we know the velocity of the platform under the character
			if (movingPlatform.newPlatform) {
				Transform platform = movingPlatform.activePlatform;
				yield return new WaitForFixedUpdate();
				yield return new WaitForFixedUpdate();
				if (grounded && platform == movingPlatform.activePlatform)
					yield return 1;
			}
			movement.velocity -= movingPlatform.platformVelocity;
		}
	}
	
	private bool MoveWithPlatform () {
		return (
			movingPlatform.enabled
			&& (grounded || movingPlatform.movementTransfer == MovementTransferOnJump.PermaLocked)
			&& movingPlatform.activePlatform != null
			);
	}
	
	private Vector3 GetDesiredHorizontalVelocity () {
		// Find desired velocity
		Vector3 desiredLocalDirection = tr.InverseTransformDirection(inputMoveDirection);
		double maxSpeed = MaxSpeedInDirection(desiredLocalDirection);
		if (grounded) {
			// Modify max speed on slopes based on slope speed multiplier curve
			var movementSlopeAngle = Mathf.Asin(movement.velocity.normalized.y)  * Mathf.Rad2Deg;
			maxSpeed *= movement.slopeSpeedMultiplier.Evaluate(movementSlopeAngle);
		}
		return tr.TransformDirection(desiredLocalDirection * (float)maxSpeed);
	}
	
	
	
	private Vector3 AdjustGroundVelocityToNormal (Vector3 hVelocity, Vector3 groundNormal) {
		Vector3 sideways = Vector3.Cross(Vector3.up, hVelocity);
		return Vector3.Cross(sideways, groundNormal).normalized * hVelocity.magnitude;
	}
	
	private bool IsGroundedTest () {
		return (groundNormal.y > 0.01);
	}
	
	float GetMaxAcceleration (bool grounded) {
		// Maximum acceleration on ground and in air
		if (grounded)
			if (sprint.enabled && sprint.sprinting)
				return sprint.sprintAccel;
			else
				return movement.maxGroundAcceleration;
		else
			return movement.maxAirAcceleration;
	}
	
	float CalculateJumpVerticalSpeed (float targetJumpHeight) {
		// From the jump height and gravity we deduce the upwards speed
		// for the character to reach at the apex.
		return Mathf.Sqrt (2 * targetJumpHeight * movement.gravity);
	}
	
	public bool isFalling {
		get { return movement.velocity.y < 0f; }
	}
	
	public bool canJetpack {
		get { 
			if (!jetpack.enabled) return false;
			return jetpack.lastStartTime + jetpack.cooldown < Time.time
				&& jetpack.fuel > jetpack.rechargeRate
				&& !grounded;
		}
	}
	
	public bool canDashpack {
		get {
			if (!dashpack.enabled) return false;
			return dashpack.lastStartTime + dashpack.cooldown < Time.time
				&& dashpack.fuel > dashpack.rechargeRate
				&& !grounded;
		}
	}
	
	public bool canSprint {
		get {
			if (!sprint.enabled) return false;
			return sprint.lastStartTime + sprint.cooldown < Time.time
				&& sprint.stamina > sprint.recoveryRate;
				//&& grounded;
		}
	}
	
	bool IsJumping () {
		return jumping.jumping;
	}
	
	bool IsSliding () {
		return (grounded && sliding.enabled && TooSteep());
	}
	
	bool IsTouchingCeiling () {
		return (movement.collisionFlags & CollisionFlags.CollidedAbove) != 0;
	}
	
	bool IsGrounded () {
		return grounded;
	}
	
	bool TooSteep () {
		return (groundNormal.y <= Mathf.Cos(controller.slopeLimit * Mathf.Deg2Rad));
	}
	
	Vector3 GetDirection () {
		return inputMoveDirection;
	}
	
	void SetControllable (bool controllable) {
		canControl = controllable;
	}
	
	// Project a direction onto elliptical quater segments based on forward, sideways, and backwards speed.
	// The function returns the length of the resulting vector.
	double MaxSpeedInDirection (Vector3 desiredMovementDirection) {
		float forwardSpeed = sprint.sprinting ? sprint.sprintSpeed : movement.maxForwardSpeed;
		if (desiredMovementDirection == Vector3.zero)
			return 0;
		else {
			double zAxisEllipseMultiplier = (desiredMovementDirection.z > 0 ? forwardSpeed : movement.maxBackwardsSpeed) / movement.maxSidewaysSpeed;
			float dMD = (float)desiredMovementDirection.x;
			float uio = (float)(desiredMovementDirection.z / zAxisEllipseMultiplier);
			Vector3 temp = new Vector3(dMD, 0, uio).normalized;
			float t1 = (float)temp.x; float t2 = (float)(temp.z * zAxisEllipseMultiplier);
			double length = new Vector3(t1, 0, t2).magnitude * movement.maxSidewaysSpeed;
			return length;
		}
	}
	
	void SetVelocity (Vector3 velocity) {
		grounded = false;
		movement.velocity = velocity;
		movement.frameVelocity = Vector3.zero;
		SendMessage("OnExternalVelocity");
	}
}