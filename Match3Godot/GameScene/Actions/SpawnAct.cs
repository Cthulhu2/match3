using System.Threading.Tasks;
using GameEngine;
using Godot;

// ReSharper disable CheckNamespace

public partial class SpawnAct : GodotObject
{
    private readonly GameScene _scene;
    private readonly Tween _tween;
    private readonly SpawnAction _act;

    private SpawnAct(GameScene scene, SpawnAction action, Tween tween)
    {
        _scene = scene;
        _act = action;
        _tween = tween.SetParallel().SetLoops(1);
        _tween.Stop();
    }

    public static async Task Exec(GameScene scene, SpawnAction act)
    {
        await new SpawnAct(scene, act, scene.CreateTween()).Exec();
    }

    private async Task Exec()
    {
        GD.Print("SpawnAct.Exec");
        foreach (ItemPos spPos in _act.Positions)
        {
            GD.Print($"SpawnAct. {spPos.Pos}, {spPos.Item.Dump()}");
            Sprite2D itemSprite =
                _scene.SpawnSprite(spPos.Pos.X, spPos.Pos.Y, spPos.Item);
            itemSprite.Scale = new Vector2(5, 0);
            itemSprite.Visible = true;
            SpawnTween(itemSprite);
        }

        if (_act.Positions == null || _act.Positions.Length == 0)
        {
            GD.PrintErr("!!!!");
        }
        else
        {
            _tween.Play();
            await ToSignal(_tween, Tween.SignalName.Finished);
            _tween.Stop();
            _tween.Kill();            
        }
    }
    
    // ReSharper disable once SuggestBaseTypeForParameter
    private void SpawnTween(Sprite2D sprite)
    {
        _tween.TweenProperty(sprite, "scale:y", 5, 0.125f)
            .From(0)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.In)
            .SetDelay(0.125f);
    }
}
