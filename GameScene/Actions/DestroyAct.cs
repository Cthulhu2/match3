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
    private readonly ItemDestroyTween _destroyTween;
    private readonly ItemSpawnTween _spawnTween;

    private DestroyAct(GameScene scene,
                       Sprite[,] itemSprites,
                       DestroyAction act,
                       ItemDestroyTween destroyTween,
                       ItemSpawnTween spawnTween)
    {
        _scene = scene;
        _sprites = itemSprites;
        _act = act;
        _destroyTween = destroyTween;
        _spawnTween = spawnTween;
    }

    public static async Task Exec(GameScene scene,
                                  Sprite[,] sprites,
                                  DestroyAction act,
                                  ItemDestroyTween destroyTween,
                                  ItemSpawnTween spawnTween)
    {
        await new DestroyAct(scene, sprites, act, destroyTween, spawnTween)
            .Exec();
    }

    private async Task Exec()
    {
        foreach (Point dPos in _act.RegularDestroyedPos)
        {
            _destroyTween.Tween(_sprites[dPos.X, dPos.Y]);
        }

        // TODO: Destroy dAct.LineDestroyedPos/BombDestroyedPos
        _destroyTween.Start();
        await ToSignal(_destroyTween, "tween_all_completed");
        _destroyTween.RemoveAll();

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
                _spawnTween.Tween(itemSprite);
            }

            _spawnTween.Start();
            await ToSignal(_spawnTween, "tween_all_completed");
            _spawnTween.RemoveAll();
        }

        await Task.CompletedTask;
    }
}
