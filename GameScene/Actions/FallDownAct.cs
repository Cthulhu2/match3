using System.Threading.Tasks;
using GameEngine;
using Godot;

// ReSharper disable CheckNamespace

public class FallDownAct : Object
{
    private readonly Sprite[,] _sprites;
    private readonly FallDownAction _act;
    private readonly ItemFallTween _fallTween;

    private FallDownAct(Sprite[,] itemSprites,
                        FallDownAction act,
                        ItemFallTween fallTween)
    {
        _sprites = itemSprites;
        _act = act;
        _fallTween = fallTween;
    }

    public static async Task Exec(Sprite[,] sprites,
                                  FallDownAction act,
                                  ItemFallTween fallTween)
    {
        await new FallDownAct(sprites, act, fallTween)
            .Exec();
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
            _fallTween.Tween(sprite, toPos);
        }

        _fallTween.Start();
        await ToSignal(_fallTween, "tween_all_completed");
        _fallTween.RemoveAll();

        await Task.CompletedTask;
    }
}
