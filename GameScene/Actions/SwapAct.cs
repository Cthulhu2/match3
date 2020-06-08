using System.Threading.Tasks;
using GameEngine;
using Godot;

// ReSharper disable CheckNamespace

public class SwapAct : Object
{
    private readonly Sprite[,] _sprites;
    private readonly Tween _tween;
    private readonly SwapAction _act;

    private SwapAct(Sprite[,] itemSprites, SwapAction action, Tween tween)
    {
        _sprites = itemSprites;
        _act = action;
        _tween = tween;
    }

    public static async Task Exec(Sprite[,] sprites,
                                  SwapAction act,
                                  Tween tween)
    {
        await new SwapAct(sprites, act, tween).Exec();
    }

    private async Task Exec()
    {
        bool isHorizontal = (_act.SrcPos.Y == _act.DestPos.Y);
        Sprite srcSprite = _sprites[_act.SrcPos.X, _act.SrcPos.Y];
        Sprite destSprite = _sprites[_act.DestPos.X, _act.DestPos.Y];
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

        _tween.Start();
        await ToSignal(_tween, "tween_all_completed");
        //_tween.RemoveAll();

        _sprites[_act.DestPos.X, _act.DestPos.Y] = srcSprite;
        _sprites[_act.SrcPos.X, _act.SrcPos.Y] = destSprite;

        await Task.CompletedTask;
    }

    // ReSharper disable once SuggestBaseTypeForParameter
    private void HMoveTween(Sprite sprite, Vector2 targetPos)
    {
        // Move
        _tween.InterpolateProperty(sprite, new NodePath("position:x"),
            sprite.Position.x, targetPos.x,
            0.5f, Tween.TransitionType.Linear, Tween.EaseType.In);

        // Jump
        _tween.InterpolateProperty(sprite, new NodePath("scale:y"),
            sprite.Scale.y, sprite.Scale.y - 1,
            0.125f, Tween.TransitionType.Quad, Tween.EaseType.Out);

        _tween.InterpolateProperty(sprite, new NodePath("position:y"),
            sprite.Position.y, sprite.Position.y - 8,
            0.125f, Tween.TransitionType.Quad, Tween.EaseType.Out);

        // Fall
        _tween.InterpolateProperty(sprite, new NodePath("scale:y"),
            sprite.Scale.y - 1, sprite.Scale.y,
            0.125f, Tween.TransitionType.Quad, Tween.EaseType.In,
            0.125f);

        _tween.InterpolateProperty(sprite, new NodePath("position:y"),
            sprite.Position.y - 8, sprite.Position.y,
            0.125f, Tween.TransitionType.Quad, Tween.EaseType.In,
            0.125f);

        // Jump
        _tween.InterpolateProperty(sprite, new NodePath("scale:y"),
            sprite.Scale.y, sprite.Scale.y - 1,
            0.125f, Tween.TransitionType.Quad, Tween.EaseType.Out,
            0.25f);

        _tween.InterpolateProperty(sprite, new NodePath("position:y"),
            sprite.Position.y, sprite.Position.y - 8,
            0.125f, Tween.TransitionType.Quad, Tween.EaseType.Out,
            0.25f);

        // Fall
        _tween.InterpolateProperty(sprite, new NodePath("scale:y"),
            sprite.Scale.y - 1, sprite.Scale.y,
            0.125f, Tween.TransitionType.Quad, Tween.EaseType.In,
            0.375f);

        _tween.InterpolateProperty(sprite, new NodePath("position:y"),
            sprite.Position.y - 8, sprite.Position.y,
            0.125f, Tween.TransitionType.Quad, Tween.EaseType.In,
            0.375f);
    }

    // ReSharper disable once SuggestBaseTypeForParameter
    private void VMovTween(Sprite sprite, Vector2 targetPos)
    {
        // Move
        _tween.InterpolateProperty(sprite, new NodePath("position:y"),
            sprite.Position.y, targetPos.y,
            0.5f, Tween.TransitionType.Linear, Tween.EaseType.In);

        bool isDownTo = (sprite.Position.y < targetPos.y);
        if (isDownTo)
        {
            // Take a right side
            _tween.InterpolateProperty(sprite, new NodePath("position:x"),
                sprite.Position.x, sprite.Position.x + 10,
                0.25f, Tween.TransitionType.Linear, Tween.EaseType.In);

            _tween.InterpolateProperty(sprite, new NodePath("position:x"),
                sprite.Position.x + 10, sprite.Position.x,
                0.25f, Tween.TransitionType.Linear, Tween.EaseType.In,
                0.25f);
        }
        else
        {
            // Take a left side
            _tween.InterpolateProperty(sprite, new NodePath("position:x"),
                sprite.Position.x, sprite.Position.x - 10,
                0.25f, Tween.TransitionType.Linear, Tween.EaseType.In);

            _tween.InterpolateProperty(sprite, new NodePath("position:x"),
                sprite.Position.x - 10, sprite.Position.x,
                0.25f, Tween.TransitionType.Linear, Tween.EaseType.In,
                0.25f);
        }
    }
}
