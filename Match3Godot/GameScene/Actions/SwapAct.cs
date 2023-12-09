using System.Threading.Tasks;
using GameEngine;
using Godot;

// ReSharper disable CheckNamespace

public partial class SwapAct : GodotObject
{
    private readonly Sprite2D[,] _sprites;
    private readonly Tween _tween;
    private readonly SwapAction _act;

    private SwapAct(Sprite2D[,] itemSprites, SwapAction action, Tween tween)
    {
        _sprites = itemSprites;
        _act = action;
        _tween = tween.SetParallel().SetLoops(1);
        _tween.Stop();
    }

    public static async Task Exec(Sprite2D[,] sprites,
                                  SwapAction act,
                                  Tween tween)
    {
        await new SwapAct(sprites, act, tween).Exec();
    }

    private async Task Exec()
    {
        bool isHorizontal = (_act.Src.Pos.Y == _act.Dest.Pos.Y);
        Sprite2D srcSprite = _sprites[_act.Src.Pos.X, _act.Src.Pos.Y];
        Sprite2D destSprite = _sprites[_act.Dest.Pos.X, _act.Dest.Pos.Y];
        if (isHorizontal)
        {
            HMoveTween(srcSprite, destSprite.Position);
            HMoveTween(destSprite, srcSprite.Position);
        }
        else
        {
            VMovTween(srcSprite, destSprite.Position);
            VMovTween(destSprite, srcSprite.Position);
        }

        _tween.Play();
        await ToSignal(_tween, Tween.SignalName.Finished);
        _tween.Stop();
        _tween.Kill();

        _sprites[_act.Dest.Pos.X, _act.Dest.Pos.Y] = srcSprite;
        _sprites[_act.Src.Pos.X, _act.Src.Pos.Y] = destSprite;
    }

    // ReSharper disable once SuggestBaseTypeForParameter
    private void HMoveTween(Sprite2D sprite, Vector2 targetPos)
    {
        // Move
        _tween.TweenProperty(sprite, "position:x", targetPos.X, 0.5f)
            .From(sprite.Position.X)
            .SetTrans(Tween.TransitionType.Linear)
            .SetEase(Tween.EaseType.In);

        // Jump
        _tween.TweenProperty(sprite, "scale:y", sprite.Scale.Y - 1, 0.125f)
            .From(sprite.Scale.Y)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out);

        _tween.TweenProperty(sprite, "position:y", sprite.Position.Y - 8, 0.125f)
            .From(sprite.Position.Y)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out);

        // Fall
        _tween.TweenProperty(sprite, "scale:y", sprite.Scale.Y, 0.125f)
            .From(sprite.Scale.Y - 1)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.In)
            .SetDelay(0.125f);

        _tween.TweenProperty(sprite, "position:y", sprite.Position.Y, 0.125f)
            .From(sprite.Position.Y - 8)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.In)
            .SetDelay(0.125f);

        // Jump
        _tween.TweenProperty(sprite, "scale:y", sprite.Scale.Y - 1, 0.125f)
            .From(sprite.Scale.Y)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out)
            .SetDelay(0.25f);

        _tween.TweenProperty(sprite, "position:y", sprite.Position.Y - 8, 0.125f)
            .From(sprite.Position.Y)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out)
            .SetDelay(0.25f);

        // Fall
        _tween.TweenProperty(sprite, "scale:y", sprite.Scale.Y, 0.125f)
            .From(sprite.Scale.Y - 1)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.In)
            .SetDelay(0.375f);

        _tween.TweenProperty(sprite, "position:y", sprite.Position.Y, 0.125f)
            .From(sprite.Position.Y - 8)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.In)
            .SetDelay(0.375f);
    }

    // ReSharper disable once SuggestBaseTypeForParameter
    private void VMovTween(Sprite2D sprite, Vector2 targetPos)
    {
        // Move
        _tween.TweenProperty(sprite, "position:y", targetPos.Y, 0.5f)
            .From(sprite.Position.Y)
            .SetTrans(Tween.TransitionType.Linear)
            .SetEase(Tween.EaseType.In);

        bool isDownTo = (sprite.Position.Y < targetPos.Y);
        if (isDownTo)
        {
            // Take a right side
            _tween.TweenProperty(sprite, "position:x", sprite.Position.X + 10, 0.25f)
                .From(sprite.Position.X)
                .SetTrans(Tween.TransitionType.Linear)
                .SetEase(Tween.EaseType.In);
            _tween.TweenProperty(sprite, "position:x", sprite.Position.X, 0.25f)
                .From(sprite.Position.X + 10)
                .SetTrans(Tween.TransitionType.Linear)
                .SetEase(Tween.EaseType.In)
                .SetDelay(0.25f);
        }
        else
        {
            // Take a left side
            _tween.TweenProperty(sprite, "position:x", sprite.Position.X - 10, 0.25f)
                .From(sprite.Position.X)
                .SetTrans(Tween.TransitionType.Linear)
                .SetEase(Tween.EaseType.In);

            _tween.TweenProperty(sprite, "position:x", sprite.Position.X, 0.25f)
                .From(sprite.Position.X - 10)
                .SetTrans(Tween.TransitionType.Linear)
                .SetEase(Tween.EaseType.In)
                .SetDelay(0.25f);
        }
    }
}