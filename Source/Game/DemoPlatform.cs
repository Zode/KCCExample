using FlaxEngine;
using KCC;

namespace Game;

/// <summary>
/// DemoPlatform Script.
/// </summary>
public class DemoPlatform : Script, IKinematicMover
{
    private Vector3 _originalPosition;
    private Quaternion _originalOrientation;
    public Vector3 Axis = Vector3.Up;
    public Vector3 RotationAxis = Vector3.Up;
    public Vector3 RotationOscillationAxis = Vector3.Right;
    public float Speed = 1.0f;
    public float Distance = 100.0f;
    public float RotationSpeed = 33.0f;
    public float RotationOscillationSpeed = 2.0f;
    public float RotationOscillationDistance = 25.0f;

    /// <inheritdoc/>
    public override void OnEnable()
    {
        KinematicMover mover = Actor.As<KinematicMover>();
        mover.Controller = this;
        _originalPosition = Actor.Position;
        _originalOrientation = Actor.Orientation;
    }
    
    public void KinematicUpdate(out Vector3 goalPosition, out Quaternion goalRotation)
    {
        goalPosition = _originalPosition + Axis.Normalized * (Mathf.Sin(Time.GameTime * Speed) * Distance);
        Quaternion oscillation = Quaternion.Euler(RotationOscillationAxis * (Mathf.Sin(Time.GameTime * RotationOscillationSpeed) * RotationOscillationDistance)) * _originalOrientation;
        goalRotation = Quaternion.Euler(RotationAxis * RotationSpeed * Time.GameTime) * oscillation;
    }
}
