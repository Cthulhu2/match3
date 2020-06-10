using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GameEngine;
using Godot;
using Environment = System.Environment;
using Object = Godot.Object;

// ReSharper disable ConvertIfStatementToSwitchStatement
// ReSharper disable ParameterTypeCanBeEnumerable.Local
// ReSharper disable CheckNamespace

public class DestroyAct : Object
{
    private readonly Node2D _animTemplate;
    private readonly GameScene _scene;
    private readonly Sprite[,] _sprites;
    private readonly DestroyAction _act;
    private readonly Tween _tween;
    private readonly Dictionary<Sprite, ItemPos> _destroyedBy;

    private DestroyAct(GameScene scene,
                       Sprite[,] itemSprites,
                       DestroyAction act,
                       Tween tween,
                       Node2D animTemplate)
    {
        _scene = scene;
        _sprites = itemSprites;
        _act = act;
        _tween = tween;
        _animTemplate = animTemplate;
        //
        _destroyedBy = new Dictionary<Sprite, ItemPos>();
        foreach (ItemPos dPos in _act.DestroyedBy.Values.SelectMany(p => p))
        {
            _destroyedBy[_sprites[dPos.Pos.X, dPos.Pos.Y]] = dPos;
        }
    }

    public static async Task Exec(GameScene scene,
                                  Sprite[,] sprites,
                                  DestroyAction act,
                                  Tween tween,
                                  Node2D animTemplate)
    {
        await new DestroyAct(scene, sprites, act, tween, animTemplate).Exec();
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
        Sprite sprite = _sprites[dPos.Pos.X, dPos.Pos.Y];
        if (dPos.Item.IsRegularShape)
        {
            RegularDestroyTween(sprite, delay);
        }
        else if (dPos.Item.IsBombShape)
        {
            await BombDestroy(dPos.Item.Color, sprite, _act.DestroyedBy[dPos]);
        }
        else if (dPos.Item.IsLineShape)
        {
            LineDestroy(dPos, sprite, _act.DestroyedBy[dPos]);
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
        var bombExp = (Node2D) _animTemplate.Duplicate();
        var player =
            bombExp.GetNode<AnimationPlayer>(new NodePath("AnimationPlayer"));
        bomb.GetParent().AddChild(bombExp);
        bombExp.Position = bomb.Position;
        string animName = $"Bomb_{GameScene.Colors[color]}_Explosion";
        player.GetAnimation(animName).Loop = false; // Sometimes it's true(???)
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

    private void DestroyerLMovTween(Destroyer destroyer)
    {
        const float toX = 0;
        float distance = Math.Abs(toX - destroyer.Position.x);
        float time = distance / Destroyer.Speed;

        _tween.InterpolateProperty(destroyer, new NodePath("position:x"),
            destroyer.Position.x, toX,
            time, Tween.TransitionType.Linear, Tween.EaseType.In);

        _tween.InterpolateCallback(destroyer, time, nameof(destroyer.Kill));
    }

    private void DestroyerRMovTween(Destroyer destroyer)
    {
        float toX = _scene.ItemTable.RectSize.x;
        float distance = Math.Abs(toX - destroyer.Position.x);
        float time = distance / Destroyer.Speed;

        _tween.InterpolateProperty(destroyer, new NodePath("position:x"),
            destroyer.Position.x, toX,
            time, Tween.TransitionType.Linear, Tween.EaseType.In);

        _tween.InterpolateCallback(destroyer, time, nameof(destroyer.Kill));
    }

    private void DestroyerUMovTween(Destroyer destroyer)
    {
        const float toY = 0;
        float distance = Math.Abs(toY - destroyer.Position.y);
        float time = distance / Destroyer.Speed;

        _tween.InterpolateProperty(destroyer, new NodePath("position:y"),
            destroyer.Position.y, toY,
            time, Tween.TransitionType.Linear, Tween.EaseType.In);

        _tween.InterpolateCallback(destroyer, time, nameof(destroyer.Kill));
    }

    private void DestroyerDMovTween(Destroyer destroyer)
    {
        float toY = _scene.ItemTable.RectSize.y;
        float distance = Math.Abs(toY - destroyer.Position.y);
        float time = distance / Destroyer.Speed;

        _tween.InterpolateProperty(destroyer, new NodePath("position:y"),
            destroyer.Position.y, toY,
            time, Tween.TransitionType.Linear, Tween.EaseType.In);

        _tween.InterpolateCallback(destroyer, time, nameof(destroyer.Kill));
    }

    private async void OnDestroyerHit(Sprite toHit)
    {
        ItemPos hit = _destroyedBy[toHit];
        await DestroyItem(hit, 0);
    }

    private void LineDestroy(ItemPos linePos,
                             Sprite line,
                             ItemPos[] destroyedPos)
    {
        Sprite[] destroyedSprites = destroyedPos
            .Select(p => _scene.ItemSprites[p.Pos.X, p.Pos.Y])
            .ToArray();

        var d1Body = (Node2D) _animTemplate.Duplicate();
        var d1 = new Destroyer(destroyedSprites, d1Body);
        line.GetParent().AddChild(d1);
        d1.Position = line.Position;
        //
        if (linePos.Item.Shape == ItemShape.VLine)
        {
            d1.RotationDegrees = 90f;
        }

        d1.Visible = true;

        var d2Body = (Node2D) _animTemplate.Duplicate();
        var d2 = new Destroyer(destroyedSprites, d2Body);
        line.GetParent().AddChild(d2);
        d2.Position = line.Position;
        //
        if (linePos.Item.Shape == ItemShape.HLine)
        {
            d2.RotationDegrees = 180f;
        }
        else if (linePos.Item.Shape == ItemShape.VLine)
        {
            d2.RotationDegrees = -90f;
        }

        d2.Visible = true;

        line.Visible = false; // removed later in Exec

        if (linePos.Item.Shape == ItemShape.HLine)
        {
            DestroyerLMovTween(d1);
            DestroyerRMovTween(d2);
        }
        else if (linePos.Item.Shape == ItemShape.VLine)
        {
            DestroyerUMovTween(d1);
            DestroyerDMovTween(d2);
        }

        d1.Connect(nameof(Destroyer.HitSignal), this, nameof(OnDestroyerHit));
        d2.Connect(nameof(Destroyer.HitSignal), this, nameof(OnDestroyerHit));
    }
}

public class Destroyer : Sprite
{
    [Signal]
    public delegate void HitSignal(Sprite toHit);

    public const float Speed = 200f; // Pixels per second

    private const string FireAnim = "Destroyer_Fire";

    private readonly List<Sprite> _destroyed;
    private readonly Node2D _body;

    private AnimationPlayer _player;

    public Destroyer(Sprite[] destroyed, Node2D body)
    {
        _destroyed = destroyed.ToList();
        _body = body;
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        _player =
            _body.GetNode<AnimationPlayer>(new NodePath("AnimationPlayer"));
        _player.CurrentAnimation = FireAnim;
        _player.GetAnimation(FireAnim).Loop = false; // Sometimes it's true(???)
        _player.Stop();
        AddChild(_body);
        _body.Position = new Vector2(0, 0);
        _body.Visible = true;
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(float delta)
    {
        if (_player.IsPlaying())
        {
            return;
        }

        Sprite underFire = _destroyed
            .Find(s =>
            {
                var rect = new Rect2(s.Position, s.Texture.GetSize() * s.Scale);
                return rect.Intersects(new Rect2(Position, 55, 55));
            });

        if (underFire == null)
        {
            return;
        }

        _destroyed.Remove(underFire);
        _player.Play(FireAnim);
        EmitSignal(nameof(HitSignal), underFire);
    }

    public void Kill()
    {
        GetParent().RemoveChild(this);
        QueueFree();
    }
}
