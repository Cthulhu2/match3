using System.Threading.Tasks;
using GameEngine;
using Godot;

// ReSharper disable CheckNamespace

public class SwapAct : Object
{
    private readonly Sprite[,] _sprites;
    private readonly ItemHMovTween _hMovTween;
    private readonly ItemVMovTween _vMovTween;
    private readonly SwapAction _act;

    private SwapAct(Sprite[,] itemSprites,
                    SwapAction action,
                    ItemHMovTween itemHMovTween,
                    ItemVMovTween itemVMovTween)
    {
        _sprites = itemSprites;
        _act = action;
        _hMovTween = itemHMovTween;
        _vMovTween = itemVMovTween;
    }

    public static async Task Exec(Sprite[,] sprites,
                                  SwapAction act,
                                  ItemHMovTween hMovTween,
                                  ItemVMovTween vMovTween)
    {
        await new SwapAct(sprites, act, hMovTween, vMovTween)
            .Exec();
    }

    private async Task Exec()
    {
        bool isHorizontal = (_act.SrcPos.Y == _act.DestPos.Y);
        Sprite srcSprite = _sprites[_act.SrcPos.X, _act.SrcPos.Y];
        Sprite destSprite = _sprites[_act.DestPos.X, _act.DestPos.Y];
        if (isHorizontal)
        {
            _hMovTween.Tween(srcSprite, destSprite.Position);
            _hMovTween.Tween(destSprite, srcSprite.Position);
            _hMovTween.Start();
            await ToSignal(_hMovTween, "tween_all_completed");
            _hMovTween.RemoveAll();
        }
        else
        {
            _vMovTween.Tween(srcSprite, destSprite.Position);
            _vMovTween.Tween(destSprite, srcSprite.Position);
            _vMovTween.Start();
            await ToSignal(_vMovTween, "tween_all_completed");
            _vMovTween.RemoveAll();
        }

        _sprites[_act.DestPos.X, _act.DestPos.Y] = srcSprite;
        _sprites[_act.SrcPos.X, _act.SrcPos.Y] = destSprite;

        await Task.CompletedTask;
    }
}
