using System;
using FlaxEngine;
using FlaxEngine.GUI;
using KCC;

namespace Game;

/// <summary>
/// Script used for benchmarking
/// </summary>
public class DemoAI : Script, IKinematicCharacter
{
	KinematicCharacterController _kcc;
	private Vector3 _velocity;
	private float _xRandomizer = 1.0f;
	private float _zRandomizer = 1.0f;
	private static readonly RandomStream _rnd = new((int)Time.StartupTime.Ticks);

	private const float JUMP_SPEED = 14.0f; // Jump speed in units per second
	private const float GRAVITY = 30.0f; // Gravity in units per second squared
	private const float WALK_SPEED = 10.0f; // Walk speed in units per second
	private const float WALK_ACCELERATION = 12.0f; // Walk acceleration in units per second squared
	private const float DECELERATION_SPEED = 10.0f; // Deceleration speed in units per second
	private const float FRICTION = 6.0f; // Friction coefficient

	private float _deltaTime => 1.0f / Time.PhysicsFPS; //HACK: for the time being work around a Flax bug https://github.com/FlaxEngine/FlaxEngine/issues/3585 
	private float _forceMultiplier => 60.0f / Time.PhysicsFPS;

	/// <inheritdoc/>
    public override void OnEnable()
    {
    	_kcc = Actor.As<KinematicCharacterController>();
		_kcc.Controller = this;
		_xRandomizer = _rnd.RandRange(0.5f, 1.5f);
		_zRandomizer = _rnd.RandRange(0.5f, 1.5f);

		_kcc.SetOrientation(Quaternion.FromDirection(Vector3.Forward));
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
			drop = control * friction * _deltaTime * multiplier;
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

		float accelerationToAdd = acceleration * _deltaTime * targetSpeed;
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
		input.X = Mathf.Sin(Time.GameTime * _xRandomizer);
		input.Z = Mathf.Cos(Time.GameTime * _zRandomizer);
		input.Normalize();

		if(!_kcc.IsGrounded)
		{
			//airmove
			_velocity.Y -= GRAVITY * _deltaTime * _forceMultiplier;
			Q3Accelerate(input, 5, 2.0f * _forceMultiplier);
		}
		else
		{
			//groundmove
			_velocity.Y = 0.0f;

			Q3Friction(DECELERATION_SPEED, FRICTION, _forceMultiplier);
			Q3Accelerate(input, WALK_SPEED, WALK_ACCELERATION * _forceMultiplier);
		}
		
		movement = _velocity;
    }

	public bool KinematicCollisionValid(PhysicsColliderActor other)
	{
		if(other.HasTag("nocollide"))
		{
			return false;
		}

		return true;
	}

	public void KinematicGroundingEvent(GroundState groundingState, GroundCheckResult checkResult, RayCastHit hit)
	{
	}

    public void KinematicAttachedRigidBodyUpdate(RigidBody rigidBody)
    {
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

    /// <summary>
	/// Bounce off the ceiling, avoiding bad feeling movement. A surface is considered a ceiling if its normal points in the general direction of the gravity,
	/// the contact point is above the character, and the character is moving upwards.
	/// </summary>
	/// <param name="hit"></param>
	private void HandleCeiling(RayCastHit hit)
	{
		float normalDotGravity = (float)Vector3.Dot(hit.Normal, _kcc.GravityEulerNormalized);
		float hitAbove = (float)Vector3.Dot(hit.Point - _kcc.Position, -_kcc.GravityEulerNormalized);
		float velocityAgainstGravity = (float)Vector3.Dot(_velocity, -_kcc.GravityEulerNormalized);

		if (normalDotGravity > 0.7f && hitAbove > 0.0f && velocityAgainstGravity > 0.0f)
		{
			_velocity.Y = 0.0f;
		}
	}


	/// <summary>
	/// Handles wall collisions during jumps, so we stop moving horizontally (unlike in Quake3)
	/// </summary>
	/// <param name="hit"></param>
	private void HandleWalls(RayCastHit hit)
	{
		if(_kcc.IsGrounded)  
		{  
			return;
		}

		//Early exit if a dynamic rigidbody, since we want to actually push them 
		RigidBody rb = hit.Collider.AttachedRigidBody;
		if(rb != null && !rb.IsKinematic)
		{
			return;
		}

		if(Math.Abs(Vector3.Dot(hit.Normal, _kcc.GravityEulerNormalized)) < 0.1f)  
		{
			Vector3 velocityTowardWall = Vector3.Project(_velocity, -hit.Normal);  
			if(Vector3.Dot(velocityTowardWall, -hit.Normal) > 0.0f)  
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
    }
}
