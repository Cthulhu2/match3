using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GameEngine;
using Godot;
using Environment = System.Environment;

// ReSharper disable ConvertIfStatementToSwitchStatement
// ReSharper disable ParameterTypeCanBeEnumerable.Local
// ReSharper disable CheckNamespace

public partial class DestroyAct : GodotObject
{
    private readonly Node2D _animTemplate;
    private readonly GameScene _scene;
    private readonly Sprite2D[,] _sprites;
    private readonly DestroyAction _act;
    private readonly Dictionary<Sprite2D, ItemPos> _destroyedBy;

    private DestroyAct(GameScene scene,
                       Sprite2D[,] itemSprites,
                       DestroyAction act,
                       Node2D animTemplate)
    {
        _scene = scene;
        _sprites = itemSprites;
        _act = act;
        _animTemplate = animTemplate;
        //
        _destroyedBy = new Dictionary<Sprite2D, ItemPos>();
        foreach (ItemPos dPos in _act.DestroyedBy.Values.SelectMany(p => p))
        {
            _destroyedBy[_sprites[dPos.Pos.X, dPos.Pos.Y]] = dPos;
        }
    }

    public static async Task Exec(GameScene scene,
                                  Sprite2D[,] sprites,
                                  DestroyAction act,
                                  Node2D animTemplate)
    {
        await new DestroyAct(scene, sprites, act, animTemplate).Exec();
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
        
        foreach (Task task in destroyedBy)
        {
            await task;
        }

        _scene.UpdLblScores();

        _scene.KillSprites(_act.MatchDestroyedPos);
        _scene.KillSprites(_act.DestroyedBy.Values.SelectMany(p => p));

        GD.Print("DestroyAct.Exec. Dump:");
        _scene.Game.Dump()
            .Split(Environment.NewLine)
            .ToList()
            .ForEach(l => GD.Print(l));

        if (_act.SpawnBonuses.Length > 0)
        {
            Tween tween = _scene.CreateTween().SetLoops(1).SetParallel();
            foreach (ItemPos spPos in _act.SpawnBonuses)
            {
                Sprite2D itemSprite = _scene
                    .SpawnSprite(spPos.Pos.X, spPos.Pos.Y, spPos.Item);
                itemSprite.Scale = new Vector2(5, 0);
                itemSprite.Visible = true;
                BonusSpawnTween(tween, itemSprite);
            }
            tween.Play();
            await ToSignal(tween, Tween.SignalName.Finished);
            tween.Stop();
            tween.Kill();
        }
    }

    private async Task DestroyItem(ItemPos dPos, float delay)
    {
        Sprite2D sprite = _sprites[dPos.Pos.X, dPos.Pos.Y];
        if (dPos.Item.IsRegularShape)
        {
            Tween tween = _scene.CreateTween().SetLoops(1).SetParallel();
            RegularDestroyTween(tween, sprite, delay);
            tween.Play();
            await ToSignal(tween, Tween.SignalName.Finished);
            tween.Stop();
            tween.Kill();
        }
        else if (dPos.Item.IsBombShape)
        {
            await BombDestroy(dPos.Item.Color, sprite, _act.DestroyedBy[dPos]);
        }
        else if (dPos.Item.IsLineShape)
        {
            Tween tween = _scene.CreateTween().SetLoops(1).SetParallel();
            LineDestroy(tween, dPos, sprite, _act.DestroyedBy[dPos]);
            tween.Play();
            await ToSignal(tween, Tween.SignalName.Finished);
            tween.Stop();
            tween.Kill();
        }
    }

    private static void RegularDestroyTween(Tween tween, Sprite2D sprite, float delay)
    {
        tween.TweenProperty(sprite, "scale:y", 0, 0.25f)
            .From(sprite.Scale.Y)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out)
            .SetDelay(delay);

        tween.TweenProperty(sprite, "position:y",
                sprite.Position.Y + sprite.GetRect().Size.Y, 0.25f)
            .From(sprite.Position.Y)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.Out)
            .SetDelay(delay);
    }

    // ReSharper disable once SuggestBaseTypeForParameter
    private static void BonusSpawnTween(Tween tween, Sprite2D sprite)
    {
        tween.TweenProperty(sprite, "scale:y", 5, 0.125f)
            .From(0)
            .SetTrans(Tween.TransitionType.Quad)
            .SetEase(Tween.EaseType.In)
            .SetDelay(0.125f);
    }

    // ReSharper disable once SuggestBaseTypeForParameter
    private async Task BombDestroy(int color,
                                   Sprite2D bomb,
                                   ItemPos[] destroyedPos)
    {
        var bombExp = (Node2D) _animTemplate.Duplicate();
        var player = bombExp.GetNode<AnimationPlayer>(new NodePath("AnimationPlayer"));
        bomb.GetParent().AddChild(bombExp);
        bombExp.Position = bomb.Position;
        var sprite = bombExp.GetNode<AnimatedSprite2D>(new NodePath("AnimatedSprite2D"));
        sprite.Frame = GameScene.BombAnimFirstFrame[color];
        string animName = $"Bomb_{GameScene.Colors[color]}_Explosion";
        player.GetAnimation(animName).LoopMode = Animation.LoopModeEnum.None; // Sometimes it's true(???)
        player.Play(animName);
        bombExp.Visible = true;
        bomb.Visible = false; // removed later in Exec

        List<Task> regularDestroyedBy = destroyedPos
            .Where(p => p.Item.IsRegularShape)
            .Select(dPos =>
            {
                GD.Print($"DestroyAct. byBomb {dPos.Pos} {dPos.Item.Dump()}");
                return DestroyItem(dPos, 0.25f); // With delay
            })
            .ToList();

        await ToSignal(player, AnimationMixer.SignalName.AnimationFinished);

        bombExp.GetParent().RemoveChild(bombExp);
        bombExp.QueueFree();

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
    }

    private void DestroyerLMovTween(Tween tween, Destroyer destroyer)
    {
        const float toX = 0;
        float distance = Math.Abs(toX - destroyer.Position.X);
        float time = distance / Destroyer.Speed;

        tween.TweenProperty(destroyer, "position:x", toX, time)
            .From(destroyer.Position.X)
            .SetTrans(Tween.TransitionType.Linear)
            .SetEase(Tween.EaseType.In);

        tween.TweenCallback(Callable.From(destroyer.Kill)).SetDelay(time);
    }

    private void DestroyerRMovTween(Tween tween, Destroyer destroyer)
    {
        float toX = _scene.ItemTable.Size.X;
        float distance = Math.Abs(toX - destroyer.Position.X);
        float time = distance / Destroyer.Speed;

        tween.TweenProperty(destroyer, "position:x", toX, time)
            .From(destroyer.Position.X)
            .SetTrans(Tween.TransitionType.Linear)
            .SetEase(Tween.EaseType.In);

        tween.TweenCallback(Callable.From(destroyer.Kill)).SetDelay(time);
    }

    private void DestroyerUMovTween(Tween tween, Destroyer destroyer)
    {
        const float toY = 0;
        float distance = Math.Abs(toY - destroyer.Position.Y);
        float time = distance / Destroyer.Speed;

        tween.TweenProperty(destroyer, "position:y", toY, time)
            .From(destroyer.Position.Y)
            .SetTrans(Tween.TransitionType.Linear)
            .SetEase(Tween.EaseType.In);

        tween.TweenCallback(Callable.From(destroyer.Kill)).SetDelay(time);
    }

    private void DestroyerDMovTween(Tween tween, Destroyer destroyer)
    {
        float toY = _scene.ItemTable.Size.Y;
        float distance = Math.Abs(toY - destroyer.Position.Y);
        float time = distance / Destroyer.Speed;

        tween.TweenProperty(destroyer, "position:y", toY, time)
            .From(destroyer.Position.Y)
            .SetTrans(Tween.TransitionType.Linear)
            .SetEase(Tween.EaseType.In);

        tween.TweenCallback(Callable.From(destroyer.Kill)).SetDelay(time);
    }

    private async void OnDestroyerHit(Sprite2D toHit)
    {
        ItemPos hit = _destroyedBy[toHit];
        await DestroyItem(hit, 0);
    }

    private void LineDestroy(Tween tween,
                             ItemPos linePos,
                             Sprite2D line,
                             ItemPos[] destroyedPos)
    {
        Sprite2D[] destroyedSprites = destroyedPos
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
            DestroyerLMovTween(tween, d1);
            DestroyerRMovTween(tween, d2);
        }
        else if (linePos.Item.Shape == ItemShape.VLine)
        {
            DestroyerUMovTween(tween, d1);
            DestroyerDMovTween(tween, d2);
        }

        d1.HitSignal += OnDestroyerHit;
        d2.HitSignal += OnDestroyerHit;
    }
}

public partial class Destroyer : Sprite2D
{
    [Signal]
    public delegate void HitSignalEventHandler(Sprite2D toHit);

    public const float Speed = 200f; // Pixels per second

    private const string FireAnim = "Destroyer_Fire";

    private readonly IDictionary<Sprite2D, Rect2> _destroyed;
    private readonly Node2D _body;

    private AnimationPlayer _player;

    public Destroyer(Sprite2D[] destroyed, Node2D body)
    {
        _destroyed = destroyed.ToDictionary(
            s => s,
            s => new Rect2(s.Position, s.Texture.GetSize() * s.Scale));
        _body = body;
    }

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        _player =
            _body.GetNode<AnimationPlayer>(new NodePath("AnimationPlayer"));
        _player.CurrentAnimation = FireAnim;
        _player.GetAnimation(FireAnim).LoopMode = Animation.LoopModeEnum.None; // Sometimes it's true(???)
        var sprite =
            _body.GetNode<AnimatedSprite2D>(new NodePath("AnimatedSprite2D"));
        sprite.Frame = 9;
        AddChild(_body);
        _body.Position = new Vector2(0, 0);
        _body.Visible = true;
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
        KeyValuePair<Sprite2D, Rect2> underFire = _destroyed.FirstOrDefault(sr =>
            sr.Value.Intersects(new Rect2(Position, 55, 55)));

        if (underFire.Key == null)
        {
            return;
        }

        _destroyed.Remove(underFire);
        EmitSignal(nameof(HitSignal), underFire.Key);

        if (_player.IsPlaying())
        {
            return;
        }

        _player.Play(FireAnim);
    }

    public void Kill()
    {
        GetParent().RemoveChild(this);
        QueueFree();
    }
}
