using System.Threading.Tasks;
using GameEngine;
using Godot;

// ReSharper disable CheckNamespace

public partial class FallDownAct : GodotObject
{
    private readonly Sprite2D[,] _sprites;
    private readonly FallDownAction _act;
    private readonly Tween _tween;

    private FallDownAct(Sprite2D[,] itemSprites, FallDownAction act, Tween tween)
    {
        _sprites = itemSprites;
        _act = act;
        _tween = tween.SetParallel().SetLoops(1);
        _tween.Stop();
    }

    public static async Task Exec(Sprite2D[,] sprites,
        FallDownAction act,
        Tween tween)
    {
        await new FallDownAct(sprites, act, tween).Exec();
    }

    private async Task Exec()
    {
        GD.Print("FallDownAct.Exec");
        foreach (FallDownPos fPos in _act.Positions)
        {
            Sprite2D sprite = _sprites[fPos.SrcPos.X, fPos.SrcPos.Y];
            _sprites[fPos.SrcPos.X, fPos.SrcPos.Y] = null;
            _sprites[fPos.DestPos.X, fPos.DestPos.Y] = sprite;
            Vector2 toPos = GameScene.ToItemTablePos(fPos.DestPos.X, fPos.DestPos.Y);
            FallTween(sprite, toPos);
        }

        if (_act.Positions.Length > 0)
        {
            _tween.Play();
            await ToSignal(_tween, Tween.SignalName.Finished);
            _tween.Stop();
            _tween.Kill();
        }
    }

    // ReSharper disable once SuggestBaseTypeForParameter
    private void FallTween(Sprite2D sprite, Vector2 targetPos)
    {
        // Move
        float duration = 0.125f * ((targetPos.Y - sprite.Position.Y) / 60.2f);

        _tween.TweenProperty(sprite, "position:y", targetPos.Y, duration)
            .From(sprite.Position.Y)
            .SetTrans(Tween.TransitionType.Linear)
            .SetEase(Tween.EaseType.In);
    }
}