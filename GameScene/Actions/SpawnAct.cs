using System.Threading.Tasks;
using GameEngine;
using Godot;

// ReSharper disable CheckNamespace

public class SpawnAct : Object
{
    private readonly GameScene _scene;
    private readonly ItemSpawnTween _spawnTween;
    private readonly SpawnAction _act;

    private SpawnAct(GameScene scene,
                     SpawnAction action,
                     ItemSpawnTween spawnTween)
    {
        _scene = scene;
        _act = action;
        _spawnTween = spawnTween;
    }

    public static async Task Exec(GameScene scene,
                                  SpawnAction act,
                                  ItemSpawnTween spawnTween)
    {
        await new SpawnAct(scene, act, spawnTween)
            .Exec();
    }

    private async Task Exec()
    {
        foreach (SpawnPos spPos in _act.Positions)
        {
            Sprite itemSprite =
                _scene.SpawnSprite(spPos.Pos.X, spPos.Pos.Y, spPos.Item);
            itemSprite.Scale = new Vector2(5, 0);
            itemSprite.Visible = true;
            _spawnTween.Tween(itemSprite);
        }

        _spawnTween.Start();
        await ToSignal(_spawnTween, "tween_all_completed");
        _spawnTween.RemoveAll();
        
        await Task.CompletedTask;
    }
}
