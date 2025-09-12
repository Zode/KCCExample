using System;
using System.Collections.Generic;
using FlaxEngine;
using FlaxEngine.GUI;

namespace Game;

/// <summary>
/// FPSCounter Script.
/// </summary>
public class FPSCounter : Script
{
    Label label;
    /// <inheritdoc/>
    public override void OnEnable()
    {
        label = (Label)Actor.As<UIControl>().Control; 
    }

	public override void OnUpdate()
	{
		label.Text = $"FPS: {Engine.FramesPerSecond}";
	}
}
