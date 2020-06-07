using Godot;

// ReSharper disable CheckNamespace
// ReSharper disable ClassNeverInstantiated.Global

public class ItemDestroyTween : Tween
{
    private const float DurationSec = 0.25f;

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

    public void Tween(Sprite sprite)
    {
        InterpolateProperty(sprite, new NodePath("scale:y"),
            sprite.Scale.y, 0,
            0.25f, TransitionType.Quad, EaseType.Out);

        InterpolateProperty(sprite, new NodePath("position:y"),
            sprite.Position.y, sprite.Position.y + sprite.GetRect().Size.y,
            0.25f, TransitionType.Quad, EaseType.Out);
    }
}
