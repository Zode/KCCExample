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
			drop = control * friction * Time.DeltaTime * multiplier;
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
		if(addSpeed <= 0.0f)
		{
			return;
		}

		float accelerationToAdd = acceleration * Time.DeltaTime * targetSpeed;
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
			_velocity.Y -= 0.6f;
			Q3Accelerate(input, 5, 2.0f);
		}
		else
		{
			//groundmove
			if(_velocity.Y < 0)
			{
				_velocity.Y = 0.0f;
			}

			Q3Friction(12, 6.0f, 1.0f);
			Q3Accelerate(input, 6, 12.0f);
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

	public void KinematicGroundingEvent(GroundState groundState, RayCastHit? hit)
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

    public void KinematicCollision(ref RayCastHit hit)
    {
		//jumping against ceilings its bit awkward without reseting the Y velocity upon ceiling contact
		if(Vector3.Dot(hit.Normal, _kcc.GravityEulerNormalized) > 0.0f &&
			_velocity.Y > 0)
		{
			_velocity.Y = 0.0f;
		}
    }

    public void KinematicUnstuckEvent(Collider collider, Vector3 penetrationDirection, float penetrationDistance)
    {
    }
}
