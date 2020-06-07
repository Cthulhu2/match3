using System;
using Godot;

// ReSharper disable CheckNamespace
// ReSharper disable ClassNeverInstantiated.Global

public class ItemFallTween : Tween
{
    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        
    }

//  // Called every frame. 'delta' is the elapsed time since the previous frame.
//  public override void _Process(float delta)
//  {
//      
//  }

    public float MaxDuration { get; private set; }

    public void TerminateAll()
    {
        RemoveAll();
        MaxDuration = 0;
    }
    
    public bool InterpolateCallback(Godot.Object obj, string callback)
    {
        return this.InterpolateCallback(obj, MaxDuration, callback);
    }
    
    public void Tween(Sprite sprite, Vector2 targetPos)
    {
        // Move
        float duration = 0.125f * ((targetPos.y - sprite.Position.y) / 60.2f);
        
        InterpolateProperty(sprite, new NodePath("position:y"),
            sprite.Position.y, targetPos.y,
            duration, TransitionType.Linear, EaseType.In);

        MaxDuration = Math.Max(MaxDuration, duration);
    }
}
