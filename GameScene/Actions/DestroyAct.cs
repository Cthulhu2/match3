using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using GameEngine;
using Godot;

// ReSharper disable CheckNamespace

public class DestroyAct : Object
{
    private readonly GameScene _scene;
    private readonly Sprite[,] _sprites;
    private readonly DestroyAction _act;
    private readonly Tween _tween;

    private DestroyAct(GameScene scene,
                       Sprite[,] itemSprites,
                       DestroyAction act,
                       Tween tween)
    {
        _scene = scene;
        _sprites = itemSprites;
        _act = act;
        _tween = tween;
    }

    public static async Task Exec(GameScene scene,
                                  Sprite[,] sprites,
                                  DestroyAction act,
                                  Tween tween)
    {
        await new DestroyAct(scene, sprites, act, tween).Exec();
    }

    private async Task Exec()
    {
        foreach (Point dPos in _act.RegularDestroyedPos)
        {
            RegularDestroyTween(_sprites[dPos.X, dPos.Y]);
        }

        // TODO: Destroy dAct.LineDestroyedPos/BombDestroyedPos
        _tween.Start();
        await ToSignal(_tween, "tween_all_completed");
        _tween.RemoveAll();

        _scene.UpdLblScores();

        foreach (Point dPos in _act.RegularDestroyedPos)
        {
            GD.Print($"DestroyAct.Exec. Remove: {dPos}");
            Sprite sprite = _sprites[dPos.X, dPos.Y];
            _sprites[dPos.X, dPos.Y] = null;
            _scene.ItemTable.RemoveChild(sprite);
        }

        GD.Print("DestroyAct.Exec. Dump:");
        _scene.Game.Dump()
            .Split(System.Environment.NewLine)
            .ToList()
            .ForEach(l => GD.Print(l));

        if (_act.SpawnBonuses.Length > 0)
        {
            foreach (SpawnPos spPos in _act.SpawnBonuses)
            {
                Sprite itemSprite = _scene
                    .SpawnSprite(spPos.Pos.X, spPos.Pos.Y, spPos.Item);
                itemSprite.Scale = new Vector2(5, 0);
                itemSprite.Visible = true;
                BonusSpawnTween(itemSprite);
            }

            _tween.Start();
            await ToSignal(_tween, "tween_all_completed");
            _tween.RemoveAll();
        }

        await Task.CompletedTask;
    }
    
    private void RegularDestroyTween(Sprite sprite)
    {
        _tween.InterpolateProperty(sprite, new NodePath("scale:y"),
            sprite.Scale.y, 0,
            0.25f, Tween.TransitionType.Quad, Tween.EaseType.Out);

        _tween.InterpolateProperty(sprite, new NodePath("position:y"),
            sprite.Position.y, sprite.Position.y + sprite.GetRect().Size.y,
            0.25f, Tween.TransitionType.Quad, Tween.EaseType.Out);
    }
    
    // ReSharper disable once SuggestBaseTypeForParameter
    private void BonusSpawnTween(Sprite sprite)
    {
        _tween.InterpolateProperty(sprite, new NodePath("scale:y"),
            0, 5,
            0.125f, Tween.TransitionType.Quad, Tween.EaseType.In,
            0.125f);
    }
}
