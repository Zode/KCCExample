using FlaxEngine;
using KCC;

namespace Game;

/// <summary>
/// Make an IKinematicMover spew out events
/// </summary>
public class MoverDebugger : Script
{
	public void OnKinematicUpdate()
	{
		//need to start as a "root event" since KinematicMovers (or KinematicBases) do not automatically start it.
		KCCDebugger.BeginEvent(Actor, "Mover");

		Collider collider = Actor.GetChild<Collider>();
		if(collider != null)
		{
			KCCDebugger.DrawCollider(collider, Color.Transparent, Color.FromRGBA(0xFFFFFF2F), false);

			//add subevent since any draws in the "root event" are considered onion skin drawables.
			KCCDebugger.BeginEvent("Move");
			KCCDebugger.DrawCollider(collider, Color.Transparent, Color.Red, false);
			KCCDebugger.EndEvent();
		}

		KCCDebugger.EndEvent();
	}
}