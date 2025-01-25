using System;
using System.Collections.Generic;
using FlaxEngine;
using KCC;

namespace Game;
#nullable enable

/// <summary>
/// DemoGravitySource showcasing how to do custom gravity fields, this is in no way a full proper solution.
/// </summary>
public class DemoGravitySource : Script
{
	private readonly List<DemoFps> _affectedCharacters = [];
	public bool Planetary {get; set;} = false;

	public override void OnEnable()
	{
		Actor.As<Collider>().TriggerEnter += OnTriggerEnter;
		Actor.As<Collider>().TriggerExit += OnTriggerExit;

		PluginManager.GetPlugin<KCC.KCC>().PreSimulationUpdateEvent += PreSimulationUpdate;
	}

	public override void OnDisable()
	{
		Actor.As<Collider>().TriggerEnter -= OnTriggerEnter;
		Actor.As<Collider>().TriggerExit -= OnTriggerExit;

		PluginManager.GetPlugin<KCC.KCC>().PreSimulationUpdateEvent -= PreSimulationUpdate;
	}

	// This would work from FixedUpdate also, but this guarantees no shenanigans with interpolation.
    public void PreSimulationUpdate()
    {
        foreach(DemoFps character in _affectedCharacters)
		{
			//ring demo
			Vector3 dir = Actor.Position - character.Actor.Position;
			dir.Z = 0.0f;

			//planet demo
			if(Planetary)
			{
				dir = character.Actor.Position - Actor.Position;
			}

			Vector3 up = Vector3.Up * character.Actor.Orientation;
			Quaternion orient = Quaternion.GetRotationFromTo(up, dir.Normalized, Vector3.Up) * character.Actor.Orientation;;

			DebugDraw.DrawWireArrow(Actor.Position, orient, 10.0f, 1.0f, Color.Cyan, Time.DeltaTime, false);
			character.SetForward(orient);
		}
    }

    public void OnTriggerEnter(PhysicsColliderActor collider)
	{
		if(collider is not Actor actor)
		{
			return;
		}

		DemoFps? demoFps = actor.Parent.GetScript<DemoFps>();

		if(demoFps is null)
		{
			return;
		}

		if(_affectedCharacters.Contains(demoFps))
		{
			return;
		}

		_affectedCharacters.Add(demoFps);
	}

	public void OnTriggerExit(PhysicsColliderActor collider)
	{
		if(collider is not Actor actor)
		{
			return;
		}

		DemoFps? demoFps = actor.Parent.GetScript<DemoFps>();

		if(demoFps is null)
		{
			return;
		}

		if(!_affectedCharacters.Contains(demoFps))
		{
			return;
		}

		_affectedCharacters.Remove(demoFps);
		demoFps.SetForward(Quaternion.FromDirection(Vector3.Forward));
	}
}
