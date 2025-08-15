using System;
using FlaxEngine;
using FlaxEngine.GUI;
using KCC;

namespace Game;

/// <summary>
/// DemoFps Script showcasing how to make a controller
/// </summary>
public class DemoFps : Script, IKinematicCharacter
{
	private const float JUMP_SPEED = 14.0f; // Jump speed in units per second
	private const float GRAVITY = 30.0f; // Gravity in units per second squared
	private const float WALK_SPEED = 10.0f; // Walk speed in units per second
	private const float WALK_ACCELERATION = 12.0f; // Walk acceleration in units per second squared
	private const float DECELERATION_SPEED = 10.0f; // Deceleration speed in units per second
	private const float FRICTION = 6.0f; // Friction coefficient

	private Actor _camera;
	private Quaternion _cameraOrientation;
	public Actor teleport;
	private Quaternion _forwardOrientation = Quaternion.FromDirection(Vector3.Forward);
	private Quaternion _smoothedForwardOrientation = Quaternion.FromDirection(Vector3.Forward);
	KinematicCharacterController _kcc;
	private Vector3 _velocity;
	private int physicsFpsMode = 2;

	private float deltaTime => 1.0f / Time.PhysicsFPS;
	private float forceMultiplier => 60.0f / Time.PhysicsFPS;

	/// <summary>
	/// Gets the current camera orientation.
	/// </summary>
	public Quaternion CameraOrientation => _cameraOrientation;

	/// <summary>
	/// Gets the current forward orientation.
	/// </summary>
	public Quaternion ForwardOrientation => _forwardOrientation;

	/// <inheritdoc/>
    public override void OnEnable()
    {
		SetForward(Quaternion.FromDirection(Vector3.Forward));

    	_kcc = Actor.As<KinematicCharacterController>();
		_kcc.Controller = this;

		_camera = Actor.GetChild<Camera>();
		Screen.CursorLock = CursorLockMode.Locked;
    }

    /// <inheritdoc/>
    public override void OnDisable()
    {
        Screen.CursorLock = CursorLockMode.None;
		Screen.CursorVisible = true;
    }

	
    /// <inheritdoc/>
    public override void OnUpdate()
    {
		Vector3 angles = _camera.LocalOrientation.EulerAngles;
		angles.Y += Input.GetAxis("mousex");
		angles.X += Input.GetAxis("mousey");
		angles.X = Math.Clamp(angles.X, -89.9f, 89.9f);

		_camera.LocalOrientation = Quaternion.Euler(angles);
		
		angles.X = 0.0f;
		_cameraOrientation = Quaternion.Euler(angles);

		if(Input.GetKey(KeyboardKeys.Escape))
		{
			Screen.CursorLock = CursorLockMode.None;
			Screen.CursorVisible = true;
		}

		if(Input.GetKey(KeyboardKeys.Alpha1))
		{
			_kcc.SetPosition(teleport.Position);
		}

		if(Input.GetKeyDown(KeyboardKeys.Q)) {
			physicsFpsMode = ++physicsFpsMode % 4;
			Debug.Log("Switching PhysicsFPS mode to: " + physicsFpsMode);
			Time.PhysicsFPS = physicsFpsMode switch
			{
				0 => 15,
				1 => 30,
				2 => 60,
				3 => 120,
				_ => Time.PhysicsFPS
			};
		}
    }

	public void SetForward(Quaternion orientation)
	{
		_forwardOrientation = orientation;
	}

	//borrowed directly from quake3 :)
	private void Q3Friction(float decelerationSpeed, float friction, float multiplier)
	{
		Vector3 tempVelocity = _velocity;
		tempVelocity.Y = 0.0f;

		float drop = 0.0f;
		if(_kcc.IsGrounded)
		{
			float control = (float)tempVelocity.Length < decelerationSpeed ? decelerationSpeed : (float)tempVelocity.Length;
			drop = control * friction * deltaTime * multiplier;
		}

		float newSpeed = (float)tempVelocity.Length - drop;
		if(newSpeed < 0)
		{
			newSpeed = 0;
		}
		else if(newSpeed > 0)
		{
			newSpeed /= (float)tempVelocity.Length;
		}

		_velocity.X *= newSpeed;
		_velocity.Z *= newSpeed;
	}

	//also borrowed directly from quake3
	private void Q3Accelerate(Vector3 targetDir, float targetSpeed, float acceleration)
	{
		float directionPenalty = (float)Vector3.Dot(_velocity, targetDir);
		float addSpeed = targetSpeed - directionPenalty;
		if(addSpeed < 0.0f)
		{
			return;
		}

		float accelerationToAdd = acceleration * deltaTime * targetSpeed;
		if(accelerationToAdd > addSpeed)
		{
			accelerationToAdd = addSpeed;
		}

		_velocity.X += accelerationToAdd * targetDir.X;
		_velocity.Z += accelerationToAdd * targetDir.Z;
	}	

    public void KinematicMoveUpdate(out Vector3 movement)
    {
		Vector3 input = Vector3.Zero;
		input.Z += Input.GetAxis("forwards");
		input.X += Input.GetAxis("sideways");
		input.Normalize();
		input *= _cameraOrientation;

		float speedMultiplier = forceMultiplier * (Input.GetKey(KeyboardKeys.Shift) ? 1.8f : 1.0f);

		if(!_kcc.IsGrounded)
		{
			//airmove
			_velocity.Y -= GRAVITY * deltaTime * forceMultiplier;
			Q3Accelerate(input, 5 * speedMultiplier, 2.0f * forceMultiplier);
		}
		else
		{
			//groundmove
			if(_velocity.Y < 0)
			{
				_velocity.Y = 0.0f;
			}

			Q3Friction(DECELERATION_SPEED, FRICTION, forceMultiplier);
			Q3Accelerate(input, WALK_SPEED * speedMultiplier, WALK_ACCELERATION * forceMultiplier);
		}

		//auto-bhop wheeeee!
		if(Input.GetAction("jump"))
		{
			if(_kcc.IsGrounded)
			{
				_kcc.ForceUnground();
				_velocity.Y = JUMP_SPEED * forceMultiplier;
			}
		}
		
		//notice how this is clamped to a low value because we want a smooth transition between extremes
		float angle = Math.Clamp(1.0f - (Quaternion.AngleBetween(_smoothedForwardOrientation, _forwardOrientation) / 180.0f), 0.0f, 0.2f);
		_smoothedForwardOrientation = Quaternion.Slerp(_smoothedForwardOrientation, _forwardOrientation, angle);
		
		_kcc.SetOrientation(_smoothedForwardOrientation);
		movement = _velocity;
    }

	public bool KinematicCollisionValid(Collider other)
	{
		if(other.HasTag("nocollide"))
		{
			return false;
		}

		return true;
	}

	public void KinematicGroundingEvent(GroundState groundState, RayCastHit? hit)
	{
		Debug.Log($"KinematicGroundingEvent: {groundState}");
	}

    public void KinematicAttachedRigidBodyUpdate(RigidBody rigidBody)
    {
		//rotate camera with any platform we may be standing on
		Vector3 angularVelocity = rigidBody.AngularVelocity;
        _camera.LocalOrientation = Quaternion.RotationY((float)angularVelocity.Y * deltaTime) * _camera.LocalOrientation;
    }

    public Vector3 KinematicGroundProjection(Vector3 movement, Vector3 gravityEulerNormalized)
    {
        return _kcc.GroundTangent(movement.Normalized) * movement.Length;
    }

    public bool KinematicCanAttachToRigidBody(RigidBody rigidBody)
    {
        return true;
    }

    public void KinematicAttachedRigidBodyEvent(bool attached, RigidBody rigidBody)
	{
		if(attached)
		{
			//cancel out any momentum when attaching to a rigidbody
			_velocity -= Vector3.ProjectOnPlane(_kcc.KinematicAttachedVelocity, -_kcc.GravityEulerNormalized);
		}
		else
		{
			//preserve any momentum from moving platform when jumping off 
			_velocity += _kcc.KinematicAttachedVelocity;
		}
    }

    public void KinematicRigidBodyInteraction(RigidBodyInteraction rbInteraction)
    {
    }

    public void KinematicPostUpdate()
    {
    }

	private void HandleCeiling(RayCastHit hit)
	{
		//Avoid bad feeling movement by bouncing off of the ceiling.
		//Only treat as ceiling if:
		//1. The normal faces generally downward (dot with gravity > 0.7)
		float normalDotGravity = Vector3.Dot(hit.Normal, _kcc.GravityEulerNormalized);
		//2. The collision point is above the character's center (relative to gravity)
		Vector3 characterToHitDistance = hit.Point - _kcc.Position;
		float hitAbove = Vector3.Dot(characterToHitDistance, -_kcc.GravityEulerNormalized);
		//3. Character is moving upward (against gravity)
		float velocityAgainstGravity = Vector3.Dot(_velocity, -_kcc.GravityEulerNormalized);

		if (normalDotGravity > 0.7f && hitAbove > 0 && velocityAgainstGravity > 0)
		{
			_velocity.Y = 0.0f;
		}
	}

	private void HandleWalls(RayCastHit hit)
	{
		if(_kcc.IsGrounded)  
		{  
			return;
		}

		//Handle wall collisions during jumps, so we stop moving horizontally (unlike in Quake3)
		//Check if this is a wall collision (normal is roughly horizontal)  
		float wallThreshold = 0.1f; // Adjust as needed  
		if(Math.Abs(Vector3.Dot(hit.Normal, _kcc.GravityEulerNormalized)) < wallThreshold)  
		{
			//Remove velocity component toward the wall 
			Vector3 velocityTowardWall = Vector3.Project(_velocity, -hit.Normal);  
			if(Vector3.Dot(velocityTowardWall, -hit.Normal) > 0)  
			{
				_velocity -= velocityTowardWall;  
			}
		}
	}

    public void KinematicCollision(ref RayCastHit hit)
    {
		HandleCeiling(hit);
		HandleWalls(hit);
    }

    public void KinematicUnstuckEvent(Collider collider, Vector3 penetrationDirection, float penetrationDistance)
    {
		Debug.Log($"Unstuck event: direction {penetrationDirection}, distance {penetrationDistance}");
    }
}
