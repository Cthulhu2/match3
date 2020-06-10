using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using GameEngine;
using Godot;
using Environment = System.Environment;

// ReSharper disable ArrangeAccessorOwnerBody
// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable CheckNamespace

// ReSharper disable once ClassNeverInstantiated.Global
public class GameScene : Node2D
{
    public Game Game { get; private set; }

    private Timer _timer;

    private ItemSelTween _itemSelTween;
    private Sprite _selSprite;
    private Point _selSpritePoint = new Point(-1, -1);

    private Tween _tween;
    public TextureRect ItemTable { get; private set; }

    private Label _lblScores;
    private Label _lblTime;
    public Sprite[,] ItemSprites { get; private set; }
    private Node2D _animTemplate;

    private Queue<IAction> _actions;
    private bool _inProcess;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        Game = new Game(new Board());
        Game.Reset();
        ItemSprites = new Sprite[Game.BoardWidth, Game.BoardHeight];
        _actions = new Queue<IAction>();

        _lblScores = GetNode<Label>(new NodePath("Canvas/LblScores"));
        _lblTime = GetNode<Label>(new NodePath("Canvas/LblTime"));
        ItemTable = GetNode<TextureRect>(new NodePath("Canvas/ItemTable"));
        _itemSelTween = GetNode<ItemSelTween>(new NodePath("ItemSelTween"));
        _tween = GetNode<Tween>(new NodePath("Tween"));
        _animTemplate = GetNode<Node2D>(new NodePath("Canvas/AnimTemplate"));

        _timer = GetNode<Timer>(new NodePath("GameTimer"));
        _timer.WaitTime = 1; // sec
        _timer.Connect("timeout", this, "OnTimerTick");
        _timer.Start();

        UpdLblTime();
        UpdLblScores();
        SpawnSprites();
    }

//  // Called every frame. 'delta' is the elapsed time since the previous frame.
//  public override void _Process(float delta)
//  {
//      
//  }

    private void OnSpriteClicked(int x, int y)
    {
        if (_inProcess)
        {
            return; // no input in animation process
        }

        if (_selSpritePoint == new Point(x, y))
        {
            return;
        }

        if (Game.CanSwap(_selSpritePoint, new Point(x, y)))
        {
            Swap(x, y);
        }
        else
        {
            Select(x, y);
        }
    }

    private void Select(int x, int y)
    {
        _selSprite = ItemSprites[x, y];
        _selSpritePoint = new Point(x, y);

        _itemSelTween.Tween(_selSprite);
    }

    private void Swap(int x, int y)
    {
        IAction[] actions = Game.Swap(_selSpritePoint, new Point(x, y));

        ProcessActions(actions);
    }

    private async void ProcessActions(IAction[] actions)
    {
        _selSprite = null;
        _selSpritePoint = new Point(-1, -1);

        _itemSelTween.TerminateAll();

        foreach (IAction action in actions)
        {
            _actions.Enqueue(action);
        }

        _inProcess = true;
        while (_actions.Count > 0)
        {
            await ProcessAction(_actions.Dequeue());
        }

        _inProcess = false;
    }

    private async Task ProcessAction(IAction action)
    {
        GD.Print(action);
        switch (action)
        {
            case SwapAction swAct:
                await SwapAct.Exec(ItemSprites, swAct, _tween);
                break;

            case DestroyAction dAct:
                GD.Print(dAct.Dump());
                await DestroyAct.Exec(this, ItemSprites, dAct, _tween,
                    _animTemplate);
                break;

            case FallDownAction fdAct:
            {
                Task tFall = FallDownAct.Exec(ItemSprites, fdAct, _tween);

                if (_actions.Peek() is SpawnAction)
                {
                    var spAct = (SpawnAction) _actions.Dequeue();
                    await SpawnAct.Exec(this, spAct, _tween);
                }

                await tFall;
                break;
            }

            case SpawnAction spAct:
                await SpawnAct.Exec(this, spAct, _tween);
                break;
        }

        await Task.CompletedTask;
    }

    public override void _Input(InputEvent evt)
    {
        switch (evt)
        {
            case InputEventMouse mEvt:
                OnMouseEvent(mEvt);
                break;
            case InputEventKey kEvt:
                OnKeyEvent(kEvt);
                break;
        }
    }

    private void OnKeyEvent(InputEventKey evt)
    {
        GD.Print($"OnKeyEvent. evt: {evt.Scancode}");
        if (_inProcess || !evt.IsPressed() || !evt.Alt)
        {
            return;
        }

        if (evt.Scancode == (ulong) KeyList.Key1)
        {
            ProcessActions(Game.CheatBonus(ItemShape.Bomb));
        }
        else if (evt.Scancode == (ulong) KeyList.Key2)
        {
            ProcessActions(Game.CheatBonus(ItemShape.HLine));
        }
        else if (evt.Scancode == (ulong) KeyList.Key3)
        {
            ProcessActions(Game.CheatBonus(ItemShape.VLine));
        }
    }

    private void OnMouseEvent(InputEventMouse evt)
    {
        if (!evt.IsPressed()
            || !ItemTable.GetGlobalRect().HasPoint(evt.Position))
        {
            return;
        }

        for (int y = 0; y < Game.BoardHeight; y++)
        {
            for (int x = 0; x < Game.BoardWidth; x++)
            {
                Sprite s = ItemSprites[x, y];
                if (s != null && s.GetRect().HasPoint(s.ToLocal(evt.Position)))
                {
                    OnSpriteClicked(x, y);
                }
            }
        }
    }

    private void UpdLblTime()
    {
        _lblTime.Text = $"Time left: {Game.TimeLeftSec:D2} seconds";
    }

    public void UpdLblScores()
    {
        _lblScores.Text = $"Scores: {Game.Scores:D10}";
    }

    public void OnTimerTick()
    {
        Game.Tick();
        if (Game.IsOver)
        {
            GetTree().ChangeScene("res://GameOver/GameOver.tscn");
        }
        else
        {
            _timer.Start();
        }

        UpdLblTime();
    }

    public static readonly Dictionary<int, string> Colors =
        new Dictionary<int, string>
        {
            {1, "Green"},
            {2, "Blue"},
            {3, "Grey"},
        };

    private static readonly Dictionary<ItemShape, string> Shapes =
        new Dictionary<ItemShape, string>
        {
            {ItemShape.Ball, "Ball"},
            {ItemShape.Cube, "Cube"},
            {ItemShape.HLine, "HLine"},
            {ItemShape.VLine, "HLine"}, // and rotate
            {ItemShape.Bomb, "Bomb"},
        };

    private static Sprite GenSprite(Item item)
    {
        string shape = Shapes[item.Shape];
        string color = Colors[item.Color];
        string texture = $"{shape}_{color}.png";

        var sprite = new Sprite
        {
            Texture = GD.Load<Texture>("res://GameScene/Art/" + texture)
        };
        if (item.Shape == ItemShape.VLine)
        {
            sprite.Rotation = (float) (90f * (Math.PI / 180f));
        }

        return sprite;
    }

    private void SpawnSprites()
    {
        for (int y = 0; y < Game.BoardHeight; y++)
        {
            for (int x = 0; x < Game.BoardWidth; x++)
            {
                SpawnSprite(x, y, Game.Items[x, y])
                    .Visible = true;
            }
        }
    }

    public Sprite SpawnSprite(int x, int y, Item item)
    {
        Sprite sprite = GenSprite(item);

        sprite.Scale = new Vector2(5, 5);
        sprite.Position = ToItemTablePos(x, y);

        if (ItemSprites[x, y] != null)
        {
            GD.PrintErr($"Doubled Sprite!!! {x}:{y} {item}");
            Environment.StackTrace
                .Split(Environment.NewLine)
                .ToList()
                .ForEach(l => GD.PrintErr(l));
        }

        ItemSprites[x, y] = sprite;
        ItemTable.AddChild(sprite);

        return sprite;
    }

    public void KillSprite(int x, int y)
    {
        Sprite sprite = ItemSprites[x, y];
        ItemSprites[x, y] = null;
        ItemTable.RemoveChild(sprite);
        sprite.QueueFree();
    }

    public static Vector2 ToItemTablePos(int x, int y)
    {
        // border + (x * textureSize [11 * 5 scale]) + (x * spriteDistance)
        return new Vector2(
            46 + (x * 55) + (x * 17.2f),
            38 + (y * 55) + (y * 5.2f));
    }
}
