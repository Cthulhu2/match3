using Godot;

// ReSharper disable CheckNamespace
// ReSharper disable ClassNeverInstantiated.Global

public class ItemHMovTween : Tween
{
    private const float DurationSec = 0.5f;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
    }

//  // Called every frame. 'delta' is the elapsed time since the previous frame.
//  public override void _Process(float delta)
//  {
//      
//  }

    public bool InterpolateCallback(Object obj, string callback)
    {
        return this.InterpolateCallback(obj, DurationSec, callback);
    }

    public void Tween(Sprite sprite, Vector2 targetPos)
    {
        // Move
        InterpolateProperty(sprite, new NodePath("position:x"),
            sprite.Position.x, targetPos.x,
            0.5f, TransitionType.Linear, EaseType.In);

        // Jump
        InterpolateProperty(sprite, new NodePath("scale:y"),
            sprite.Scale.y, sprite.Scale.y - 1,
            0.125f, TransitionType.Quad, EaseType.Out);

        InterpolateProperty(sprite, new NodePath("position:y"),
            sprite.Position.y, sprite.Position.y - 8,
            0.125f, TransitionType.Quad, EaseType.Out);

        // Fall
        InterpolateProperty(sprite, new NodePath("scale:y"),
            sprite.Scale.y - 1, sprite.Scale.y,
            0.125f, TransitionType.Quad, EaseType.In,
            0.125f);

        InterpolateProperty(sprite, new NodePath("position:y"),
            sprite.Position.y - 8, sprite.Position.y,
            0.125f, TransitionType.Quad, EaseType.In,
            0.125f);

        // Jump
        InterpolateProperty(sprite, new NodePath("scale:y"),
            sprite.Scale.y, sprite.Scale.y - 1,
            0.125f, TransitionType.Quad, EaseType.Out,
            0.25f);

        InterpolateProperty(sprite, new NodePath("position:y"),
            sprite.Position.y, sprite.Position.y - 8,
            0.125f, TransitionType.Quad, EaseType.Out,
            0.25f);

        // Fall
        InterpolateProperty(sprite, new NodePath("scale:y"),
            sprite.Scale.y - 1, sprite.Scale.y,
            0.125f, TransitionType.Quad, EaseType.In,
            0.375f);

        InterpolateProperty(sprite, new NodePath("position:y"),
            sprite.Position.y - 8, sprite.Position.y,
            0.125f, TransitionType.Quad, EaseType.In,
            0.375f);
    }
}
