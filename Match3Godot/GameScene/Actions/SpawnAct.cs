using System.Threading.Tasks;
using GameEngine;
using Godot;

// ReSharper disable CheckNamespace

public class SpawnAct : Object
{
    private readonly GameScene _scene;
    private readonly Tween _tween;
    private readonly SpawnAction _act;

    private SpawnAct(GameScene scene, SpawnAction action, Tween tween)
    {
        _scene = scene;
        _act = action;
        _tween = tween;
    }

    public static async Task Exec(GameScene scene,
                                  SpawnAction act,
                                  Tween tween)
    {
        await new SpawnAct(scene, act, tween).Exec();
    }

    private async Task Exec()
    {
        foreach (ItemPos spPos in _act.Positions)
        {
            GD.Print($"SpawnAct. {spPos.Pos}, {spPos.Item.Dump()}");
            Sprite itemSprite =
                _scene.SpawnSprite(spPos.Pos.X, spPos.Pos.Y, spPos.Item);
            itemSprite.Scale = new Vector2(5, 0);
            itemSprite.Visible = true;
            SpawnTween(itemSprite);
        }

        _tween.Start();
        await ToSignal(_tween, "tween_all_completed");
    }
    
    // ReSharper disable once SuggestBaseTypeForParameter
    private void SpawnTween(Sprite sprite)
    {
        _tween.InterpolateProperty(sprite, new NodePath("scale:y"),
            0, 5,
            0.125f, Tween.TransitionType.Quad, Tween.EaseType.In,
            0.125f);
    }
}
