using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using GameEngine;
using Godot;

// ReSharper disable ParameterTypeCanBeEnumerable.Local
// ReSharper disable CheckNamespace

public class DestroyAct : Object
{
    private readonly KinematicBody2D _bombTemplate;
    private readonly GameScene _scene;
    private readonly Sprite[,] _sprites;
    private readonly DestroyAction _act;
    private readonly Tween _tween;

    private DestroyAct(GameScene scene,
                       Sprite[,] itemSprites,
                       DestroyAction act,
                       Tween tween,
                       KinematicBody2D bombTemplate)
    {
        _scene = scene;
        _sprites = itemSprites;
        _act = act;
        _tween = tween;
        _bombTemplate = bombTemplate;
    }

    public static async Task Exec(GameScene scene,
                                  Sprite[,] sprites,
                                  DestroyAction act,
                                  Tween tween,
                                  KinematicBody2D bombTemplate)
    {
        await new DestroyAct(scene, sprites, act, tween, bombTemplate).Exec();
    }

    private async Task Exec()
    {
        foreach (ItemPos dPos in _act.RegularDestroyedPos)
        {
            Sprite item = _sprites[dPos.Pos.X, dPos.Pos.Y];
            if (!dPos.Item.IsBombShape)
            {
                RegularDestroyTween(item, 0);
            }
            else if (dPos.Item.IsBombShape)
            {
                Sprite[] bombNeighbour = _scene.Game.GetBombNeighbour(dPos.Pos)
                    .Where(p => _act.BombDestroyedPos.Contains(p))
                    .Select(p => _sprites[p.X, p.Y])
                    .ToArray();
                BombDestroy(dPos.Item.Color, item, bombNeighbour);
            }
        }

        // TODO: Destroy dAct.LineDestroyedPos
        _tween.Start();
        await ToSignal(_tween, "tween_all_completed");
        _tween.RemoveAll();

        _scene.UpdLblScores();

        foreach (ItemPos dPos in _act.RegularDestroyedPos)
        {
            Sprite sprite = _sprites[dPos.Pos.X, dPos.Pos.Y];
            GD.Print($"DestroyAct.Exec. RemoveByMatch: {dPos.Pos},"
                     + $" {sprite.Texture.ResourcePath}");
            _sprites[dPos.Pos.X, dPos.Pos.Y] = null;
            _scene.ItemTable.RemoveChild(sprite);
            sprite.QueueFree();
        }
        foreach (Point dPos in _act.BombDestroyedPos)
        {
            Sprite sprite = _sprites[dPos.X, dPos.Y];
            GD.Print($"DestroyAct.Exec. RemoveByBomb: {dPos}, "
                     + $" {sprite.Texture.ResourcePath}");
            _sprites[dPos.X, dPos.Y] = null;
            _scene.ItemTable.RemoveChild(sprite);
            sprite.QueueFree();
        }
        foreach (Point dPos in _act.LineDestroyedPos)
        {
            Sprite sprite = _sprites[dPos.X, dPos.Y];
            GD.Print($"DestroyAct.Exec. RemoveByLine: {dPos}, "
                     + $" {sprite.Texture.ResourcePath}");
            _sprites[dPos.X, dPos.Y] = null;
            _scene.ItemTable.RemoveChild(sprite);
            sprite.QueueFree();
        }

        GD.Print("DestroyAct.Exec. Dump:");
        _scene.Game.Dump()
            .Split(System.Environment.NewLine)
            .ToList()
            .ForEach(l => GD.Print(l));

        if (_act.SpawnBonuses.Length > 0)
        {
            foreach (ItemPos spPos in _act.SpawnBonuses)
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
    }

    private void RegularDestroyTween(Sprite sprite, float delay)
    {
        _tween.InterpolateProperty(sprite, new NodePath("scale:y"),
            sprite.Scale.y, 0,
            0.25f, Tween.TransitionType.Quad, Tween.EaseType.Out,
            delay);

        _tween.InterpolateProperty(sprite, new NodePath("position:y"),
            sprite.Position.y, sprite.Position.y + sprite.GetRect().Size.y,
            0.25f, Tween.TransitionType.Quad, Tween.EaseType.Out,
            delay);
    }

    // ReSharper disable once SuggestBaseTypeForParameter
    private void BonusSpawnTween(Sprite sprite)
    {
        _tween.InterpolateProperty(sprite, new NodePath("scale:y"),
            0, 5,
            0.125f, Tween.TransitionType.Quad, Tween.EaseType.In,
            0.125f);
    }

    // ReSharper disable once SuggestBaseTypeForParameter
    private async void BombDestroy(int color,
                                   Sprite bomb,
                                   Sprite[] destroyed)
    {
        var bombExp = (KinematicBody2D) _bombTemplate.Duplicate();
        var player =
            bombExp.GetNode<AnimationPlayer>(new NodePath("AnimationPlayer"));
        bomb.GetParent().AddChild(bombExp);
        bombExp.Position = bomb.Position;
        string animName = $"Bomb_{GameScene.Colors[color]}_Explosion";
        player.GetAnimation(animName).Loop = false; // Sometimes it's true???
        player.Play(animName);
        bomb.Visible = false; // removed later in Exec
        bombExp.Visible = true;

        foreach (Sprite sprite in destroyed)
        {
            RegularDestroyTween(sprite, 0.25f);
        }

        await ToSignal(player, "animation_finished");
        bombExp.GetParent().RemoveChild(bombExp);
        bombExp.QueueFree();
    }
}
