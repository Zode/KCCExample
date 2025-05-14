using System;
using System.Collections.Generic;
using FlaxEngine;

namespace Game;

/// <summary>
/// AIBenchmark Script.
/// </summary>
public class AIBenchmark : Script
{
    public Prefab AIPrefab;
    public int Rows = 10;
    public int Columns = 10;
    public float DistModifier = 10.0f;

    /// <inheritdoc/>
    public override void OnStart()
    {
        // Here you can add code that needs to be called when script is created, just before the first game update
    }
    
    /// <inheritdoc/>
    public override void OnEnable()
    {
        for(int x = 0; x < Rows; x++)
        {
            for(int z = 0; z < Columns; z++)
            {
                Actor actor = PrefabManager.SpawnPrefab(AIPrefab, Actor.Position + new Vector3(x * DistModifier, 0.0f, z * DistModifier));
                actor.SetParent(Actor, true, false);
            }
        }
    }

    /// <inheritdoc/>
    public override void OnDisable()
    {
        // Here you can add code that needs to be called when script is disabled (eg. unregister from events)
    }

    /// <inheritdoc/>
    public override void OnUpdate()
    {
        // Here you can add code that needs to be called every frame
    }
}
