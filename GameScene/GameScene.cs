using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using GameEngine;
using Godot;

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
    private Sprite[,] _itemSprites;

    private Queue<IAction> _actions;
    private bool _inProcess;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        Game = new Game(new Board());
        Game.Reset();
        _itemSprites = new Sprite[Game.BoardWidth, Game.BoardHeight];
        _actions = new Queue<IAction>();

        _lblScores = GetNode<Label>(new NodePath("Canvas/LblScores"));
        _lblTime = GetNode<Label>(new NodePath("Canvas/LblTime"));
        ItemTable = GetNode<TextureRect>(new NodePath("Canvas/ItemTable"));
        _itemSelTween = GetNode<ItemSelTween>(new NodePath("ItemSelTween"));
        _tween = GetNode<Tween>(new NodePath("Tween"));

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
        _selSprite = _itemSprites[x, y];
        _selSpritePoint = new Point(x, y);

        _itemSelTween.Tween(_selSprite);
    }

    private void Swap(int x, int y)
    {
        IAction[] actions = Game.Swap(_selSpritePoint, new Point(x, y));

        _selSprite = null;
        _selSpritePoint = new Point(-1, -1);

        _itemSelTween.TerminateAll();

        foreach (IAction action in actions)
        {
            _actions.Enqueue(action);
        }

        ProcessActions();
    }

    private async void ProcessActions()
    {
        _inProcess = true;
        while (_actions.Count > 0)
        {
            await ProcessAction(_actions.Dequeue());
        }

        _inProcess = false;
    }

    private async Task ProcessAction(IAction action)
    {
        switch (action)
        {
            case SwapAction swAct:
                await SwapAct.Exec(_itemSprites, swAct, _tween);
                break;

            case DestroyAction dAct:
                await DestroyAct.Exec(this, _itemSprites, dAct, _tween);
                break;

            case FallDownAction fdAct:
            {
                Task tFall = FallDownAct.Exec(_itemSprites, fdAct, _tween);

                if (_actions.Peek() is SpawnAction)
                {
                    var spAct = (SpawnAction) _actions.Dequeue();
                    await SpawnAct.Exec(this, spAct, _tween);
                }

                await tFall;
                break;
            }
        }

        await Task.CompletedTask;
    }

    public override void _Input(InputEvent evt)
    {
        if (!(evt is InputEventMouseButton))
        {
            return;
        }

        var mEvt = (InputEventMouseButton) evt;
        if (!evt.IsPressed()
            || !ItemTable.GetGlobalRect().HasPoint(mEvt.Position))
        {
            return;
        }

        for (int y = 0; y < Game.BoardHeight; y++)
        {
            for (int x = 0; x < Game.BoardWidth; x++)
            {
                Sprite s = _itemSprites[x, y];
                if (s != null && s.GetRect().HasPoint(s.ToLocal(mEvt.Position)))
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

    private static readonly Dictionary<int, string> Colors =
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

        _itemSprites[x, y] = sprite;
        ItemTable.AddChild(sprite);

        return sprite;
    }

    public static Vector2 ToItemTablePos(int x, int y)
    {
        // border + (x * textureSize [11 * 5 scale]) + (x * spriteDistance)
        return new Vector2(
            46 + (x * 55) + (x * 17.2f),
            38 + (y * 55) + (y * 5.2f));
    }
}
