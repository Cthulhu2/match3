using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GameEngine;
using Godot;
using Environment = System.Environment;

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
        List<Task> destroyedBy = _act.MatchDestroyedPos
            .Select(dPos =>
            {
                GD.Print($"DestroyAct. byMatch {dPos.Pos} {dPos.Item.Dump()}");
                return DestroyItem(dPos, 0);
            })
            .ToList();
        
        _tween.Start();
        await ToSignal(_tween, "tween_all_completed");
        _tween.RemoveAll();
        foreach (Task task in destroyedBy)
        {
            await task;
        }

        _scene.UpdLblScores();

        foreach (ItemPos dPos in _act.MatchDestroyedPos)
        {
            _scene.KillSprite(dPos.Pos.X, dPos.Pos.Y);
        }

        ItemPos[] dPositions = _act.DestroyedBy.Values
            .SelectMany(p => p)
            .ToArray();
        foreach (ItemPos dPos in dPositions)
        {
            _scene.KillSprite(dPos.Pos.X, dPos.Pos.Y);
        }

        GD.Print("DestroyAct.Exec. Dump:");
        _scene.Game.Dump()
            .Split(Environment.NewLine)
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

    private async Task DestroyItem(ItemPos dPos, float delay)
    {
        Sprite item = _sprites[dPos.Pos.X, dPos.Pos.Y];
        if (dPos.Item.IsRegularShape)
        {
            RegularDestroyTween(item, delay);
        }
        else if (dPos.Item.IsBombShape)
        {
            await BombDestroy(dPos.Item.Color, item, _act.DestroyedBy[dPos]);
        }
        else if (dPos.Item.IsLineShape)
        {
            // TODO: Destroy dAct.LineDestroyedPos
            RegularDestroyTween(item, 0);
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
    private async Task BombDestroy(int color,
                                   Sprite bomb,
                                   ItemPos[] destroyedPos)
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

        List<Task> regularDestroyedBy = destroyedPos
            .Where(p => p.Item.IsRegularShape)
            .Select(dPos =>
            {
                GD.Print($"DestroyAct. byBomb {dPos.Pos} {dPos.Item.Dump()}");
                return DestroyItem(dPos, 0.25f); // With delay
            })
            .ToList();
        
        await ToSignal(player, "animation_finished");

        List<Task> bonusDestroyedBy = destroyedPos
            .Where(p => !p.Item.IsRegularShape)
            .Select(dPos =>
            {
                GD.Print($"DestroyAct. byBomb {dPos.Pos} {dPos.Item.Dump()}");
                return DestroyItem(dPos, 0);
            })
            .ToList();
        
        foreach (Task regular in regularDestroyedBy)
        {
            await regular;
        }
        foreach (Task bonus in bonusDestroyedBy)
        {
            await bonus;
        }

        bombExp.GetParent().RemoveChild(bombExp);
        bombExp.QueueFree();
    }
}
