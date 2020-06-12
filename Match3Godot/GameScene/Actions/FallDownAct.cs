using System.Threading.Tasks;
using GameEngine;
using Godot;

// ReSharper disable CheckNamespace

public class FallDownAct : Object
{
    private readonly Sprite[,] _sprites;
    private readonly FallDownAction _act;
    private readonly Tween _tween;

    private FallDownAct(Sprite[,] itemSprites, FallDownAction act, Tween tween)
    {
        _sprites = itemSprites;
        _act = act;
        _tween = tween;
    }

    public static async Task Exec(Sprite[,] sprites,
                                  FallDownAction act,
                                  Tween tween)
    {
        await new FallDownAct(sprites, act, tween).Exec();
    }

    private async Task Exec()
    {
        foreach (FallDownPos fPos in _act.Positions)
        {
            Sprite sprite = _sprites[fPos.SrcPos.X, fPos.SrcPos.Y];
            _sprites[fPos.SrcPos.X, fPos.SrcPos.Y] = null;
            _sprites[fPos.DestPos.X, fPos.DestPos.Y] = sprite;
            Vector2 toPos =
                GameScene.ToItemTablePos(fPos.DestPos.X, fPos.DestPos.Y);
            FallTween(sprite, toPos);
        }

        _tween.Start();
        await ToSignal(_tween, "tween_all_completed");
        _tween.RemoveAll();
    }

    // ReSharper disable once SuggestBaseTypeForParameter
    private void FallTween(Sprite sprite, Vector2 targetPos)
    {
        // Move
        float duration = 0.125f * ((targetPos.y - sprite.Position.y) / 60.2f);

        _tween.InterpolateProperty(sprite, new NodePath("position:y"),
            sprite.Position.y, targetPos.y,
            duration, Tween.TransitionType.Linear, Tween.EaseType.In);
    }
}
