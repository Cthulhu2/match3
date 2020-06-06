using Godot;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable CheckNamespace

public class ItemSelTween : Tween
{
    private Vector2 _oldPos;
    private Vector2 _oldScale;
    private Sprite _sprite;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
    }

//  // Called every frame. 'delta' is the elapsed time since the previous frame.
//  public override void _Process(float delta)
//  {
//      
//  }

    public void Tween(Sprite sprite)
    {
        if (_sprite == sprite)
        {
            return;
        }

        RemoveAll();

        if (_sprite != null)
        {
            _sprite.Position = _oldPos;
            _sprite.Scale = _oldScale;
        }

        _sprite = sprite;
        _oldPos = sprite.Position;
        _oldScale = sprite.Scale;
        // Jump
        InterpolateProperty(_sprite, new NodePath("scale:y"),
            _sprite.Scale.y, _sprite.Scale.y - 1,
            0.25f, TransitionType.Quad, EaseType.Out);

        InterpolateProperty(_sprite, new NodePath("position:y"),
            _sprite.Position.y, _sprite.Position.y - 8,
            0.25f, TransitionType.Quad, EaseType.Out);

        // Fall
        InterpolateProperty(_sprite, new NodePath("scale:y"),
            _sprite.Scale.y - 1, _sprite.Scale.y,
            0.25f, TransitionType.Quad, EaseType.In,
            0.25f);

        InterpolateProperty(_sprite, new NodePath("position:y"),
            _sprite.Position.y - 8, _sprite.Position.y,
            0.25f, TransitionType.Quad, EaseType.In,
            0.25f);

        Start();
    }
}
