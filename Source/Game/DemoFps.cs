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
	private Actor _camera;
	private Quaternion _cameraOrientation;
	public Actor teleport;
	private Quaternion _forwardOrientation = Quaternion.FromDirection(Vector3.Forward);
	private Quaternion _smoothedForwardOrientation = Quaternion.FromDirection(Vector3.Forward);
	KinematicCharacterController _kcc;
	private Vector3 _velocity;

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
			float control = tempVelocity.Length < decelerationSpeed ? decelerationSpeed : tempVelocity.Length;
			drop = control * friction * Time.DeltaTime * multiplier;
		}

		float newSpeed = tempVelocity.Length - drop;
		if(newSpeed < 0)
		{
			newSpeed = 0;
		}
		else if(newSpeed > 0)
		{
			newSpeed /= tempVelocity.Length;
		}

		_velocity.X *= newSpeed;
		_velocity.Z *= newSpeed;
	}

	//also borrowed directly from quake3
	private void Q3Accelerate(Vector3 targetDir, float targetSpeed, float acceleration)
	{
		float directionPenalty = Vector3.Dot(_velocity, targetDir);
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

    public void KinematicMoveUpdate(out Vector3 velocity, out Quaternion orientation)
    {
		Vector3 input = Vector3.Zero;
		input.Z += Input.GetAxis("forwards");
		input.X += Input.GetAxis("sideways");
		input.Normalize();
		input *= _cameraOrientation;

		if(!_kcc.IsGrounded)
		{
			//airmove
			_velocity.Y -= 30 * Time.DeltaTime;
			Q3Accelerate(input, input.Length * 5, 2.0f);
		}
		else
		{
			//groundmove
			if(_velocity.Y < 0)
			{
				_velocity.Y = 0.0f;
			}

			Q3Friction(12, 6.0f, 1.0f);
			Q3Accelerate(input, input.Length * 10, 12.0f);
		}

		//auto-bhop wheeeee!
		if(Input.GetAction("jump"))
		{
			if(_kcc.IsGrounded)
			{
				_kcc.ForceUnground();
				_velocity.Y = 800 * Time.DeltaTime;
			}
		}
		
		//notice how this is clamped to a low value because we want a smooth transition between extremes
		float angle = Math.Clamp(1.0f - (Quaternion.AngleBetween(_smoothedForwardOrientation, _forwardOrientation) / 180.0f), 0.0f, 0.2f);
		_smoothedForwardOrientation = Quaternion.Slerp(_smoothedForwardOrientation, _forwardOrientation, angle);
		orientation = _smoothedForwardOrientation;
		velocity = _velocity + input;
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
        _camera.LocalOrientation = Quaternion.RotationY(angularVelocity.Y * Time.DeltaTime) * _camera.LocalOrientation;
    }

    public Vector3 KinematicGroundProjection(Vector3 velocity, Vector3 gravityEulerNormalized)
    {
        return _kcc.GroundTangent(velocity.Normalized) * velocity.Length;
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

    public void KinematicCollision(RayCastHit hit)
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
