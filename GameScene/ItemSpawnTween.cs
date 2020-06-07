using Godot;

// ReSharper disable CheckNamespace
// ReSharper disable ClassNeverInstantiated.Global

public class ItemSpawnTween : Tween
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
            0, 5,
            0.125f, TransitionType.Quad, EaseType.In,
            0.125f);
    }
}
