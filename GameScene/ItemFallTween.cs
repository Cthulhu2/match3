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

    public void Tween(Sprite sprite, Vector2 targetPos)
    {
        // Move
        float duration = 0.125f * ((targetPos.y - sprite.Position.y) / 60.2f);

        InterpolateProperty(sprite, new NodePath("position:y"),
            sprite.Position.y, targetPos.y,
            duration, TransitionType.Linear, EaseType.In);
    }
}
