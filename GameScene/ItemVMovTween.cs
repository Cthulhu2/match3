using Godot;

// ReSharper disable CheckNamespace
// ReSharper disable ClassNeverInstantiated.Global

public class ItemVMovTween : Tween
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
        InterpolateProperty(sprite, new NodePath("position:y"),
            sprite.Position.y, targetPos.y,
            0.5f, TransitionType.Linear, EaseType.In);

        bool isDownTo = (sprite.Position.y < targetPos.y); 
        if (isDownTo)
        {
            // Take a right side
            InterpolateProperty(sprite, new NodePath("position:x"),
                sprite.Position.x, sprite.Position.x + 10,
                0.25f, TransitionType.Linear, EaseType.In);

            InterpolateProperty(sprite, new NodePath("position:x"),
                sprite.Position.x + 10, sprite.Position.x,
                0.25f, TransitionType.Linear, EaseType.In,
                0.25f);
        }
        else
        {
            // Take a left side
            InterpolateProperty(sprite, new NodePath("position:x"),
                sprite.Position.x, sprite.Position.x - 10,
                0.25f, TransitionType.Linear, EaseType.In);
            
            InterpolateProperty(sprite, new NodePath("position:x"),
                sprite.Position.x - 10, sprite.Position.x,
                0.25f, TransitionType.Linear, EaseType.In,
                0.25f);
        }
    }
}
