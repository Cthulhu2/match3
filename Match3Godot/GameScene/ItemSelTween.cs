using Godot;

// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable CheckNamespace

public partial class ItemSelTween : Node2D
{
    private Tween _tween;
    
    private Vector2 _oldPos;
    private Vector2 _oldScale;
    private Sprite2D _sprite;

    public void TerminateAll()
    {
        if (_tween != null)
        {
            _tween.Stop();
            _tween.Kill();
            _tween = null;
        }
        

        if (_sprite != null)
        {
            _sprite.Position = _oldPos;
            _sprite.Scale = _oldScale;
            _sprite = null;
        }
    }
    
    public void Tween(Sprite2D sprite)
    {
        if (_sprite == sprite)
        {
            return;
        }

        TerminateAll();

        _tween = GetTree().CreateTween().SetParallel(true).SetLoops();
        _tween.Stop();
        _sprite = sprite;
        _oldPos = sprite.Position;
        _oldScale = sprite.Scale;
        // Jump
        _tween.TweenProperty(_sprite, "scale:y", _sprite.Scale.Y - 1, 0.25f)
            .From(_sprite.Scale.Y)
            .SetTrans(Godot.Tween.TransitionType.Quad)
            .SetEase(Godot.Tween.EaseType.Out);

        _tween.TweenProperty(_sprite, "position:y", _sprite.Position.Y - 8, 0.25f)
            .From(_sprite.Position.Y)
            .SetTrans(Godot.Tween.TransitionType.Quad)
            .SetEase(Godot.Tween.EaseType.Out);

        // Fall
        _tween.TweenProperty(_sprite, "scale:y", _sprite.Scale.Y, 0.25f)
            .From(_sprite.Scale.Y - 1)
            .SetTrans(Godot.Tween.TransitionType.Quad)
            .SetEase(Godot.Tween.EaseType.In)
            .SetDelay(0.25f);

        _tween.TweenProperty(_sprite, "position:y", _sprite.Position.Y, 0.25f)
            .From(_sprite.Position.Y - 8)
            .SetTrans(Godot.Tween.TransitionType.Quad)
            .SetEase(Godot.Tween.EaseType.In)
            .SetDelay(0.25f);

        _tween.Play();
    }
}
