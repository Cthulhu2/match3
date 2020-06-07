using System.Collections.Generic;
using System.Drawing;
using GameEngine;
using Godot;

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable CheckNamespace

public class GameScene : Node2D
{
    private Game _game;

    private Timer _timer;

    private ItemSelTween _itemSelTween;
    private Sprite _selSprite;
    private Point _selSpritePoint = Point.Empty;

    private ItemHMovTween _itemHMovTween;
    private ItemVMovTween _itemVMovTween;

    private Label _lblScores;
    private Label _lblTime;
    private TextureRect _itemTable;
    private Sprite[,] _itemSprites;

    private Queue<IAction> _actions;
    private bool _inProcess;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        _game = new Game(new Board());
        _game.Reset();
        _itemSprites = new Sprite[Board.Width, Board.Height];
        _actions = new Queue<IAction>();

        _lblScores = GetNode<Label>(new NodePath("Canvas/LblScores"));
        _lblTime = GetNode<Label>(new NodePath("Canvas/LblTime"));
        _itemTable = GetNode<TextureRect>(new NodePath("Canvas/ItemTable"));
        _itemSelTween = GetNode<ItemSelTween>(new NodePath("ItemSelTween"));
        _itemHMovTween = GetNode<ItemHMovTween>(new NodePath("ItemHMovTween"));
        _itemVMovTween = GetNode<ItemVMovTween>(new NodePath("ItemVMovTween"));

        _timer = GetNode<Timer>(new NodePath("Timer"));
        _timer.WaitTime = 1; // sec
        _timer.Connect("timeout", this, "OnTimerTick");
        _timer.Start();

        UpdLblTime();
        UpdLblScores();
        GenSprites();
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
        IAction[] actions = _game.Swap(_selSpritePoint, new Point(x, y));

        _selSprite = null;
        _selSpritePoint = Point.Empty;

        _itemSelTween.TerminateAll();

        foreach (IAction action in actions)
        {
            _actions.Enqueue(action);
        }

        ProcessAction();
    }

    private void ProcessAction()
    {
        _inProcess = true;
        if (_actions.Count > 0)
        {
            ProcessAction(_actions.Dequeue());
        }
        else
        {
            _inProcess = false;
        }
    }

    private void ProcessAction(IAction action)
    {
        if (action is SwapAction act)
        {
            bool isHorizontal = (act.SrcPos.Y == act.DestPos.Y);
            Sprite srcSprite = _itemSprites[act.SrcPos.X, act.SrcPos.Y];
            Sprite destSprite = _itemSprites[act.DestPos.X, act.DestPos.Y];
            if (isHorizontal)
            {
                _itemHMovTween.Tween(srcSprite, destSprite.Position);
                _itemHMovTween.Tween(destSprite, srcSprite.Position);
                _itemHMovTween.Start();
                _itemHMovTween.InterpolateCallback(this, "ProcessAction");
            }
            else
            {
                _itemVMovTween.Tween(srcSprite, destSprite.Position);
                _itemVMovTween.Tween(destSprite, srcSprite.Position);
                _itemVMovTween.Start();
                _itemVMovTween.InterpolateCallback(this, "ProcessAction");
            }

            _itemSprites[act.DestPos.X, act.DestPos.Y] = srcSprite;
            _itemSprites[act.SrcPos.X, act.SrcPos.Y] = destSprite;
        }
        else
        {
            ProcessAction();
        }
    }

    public override void _Input(InputEvent evt)
    {
        if (!(evt is InputEventMouseButton))
        {
            return;
        }

        var mEvt = (InputEventMouseButton) evt;
        if (!evt.IsPressed()
            || !_itemTable.GetGlobalRect().HasPoint(mEvt.Position))
        {
            return;
        }

        for (int y = 0; y < Board.Height; y++)
        {
            for (int x = 0; x < Board.Width; x++)
            {
                Sprite s = _itemSprites[x, y];
                if (s.GetRect().HasPoint(s.ToLocal(mEvt.Position)))
                {
                    OnSpriteClicked(x, y);
                }
            }
        }
    }

    private void UpdLblTime()
    {
        _lblTime.Text = $"Time left: {_game.TimeLeftSec:D2} seconds";
    }

    private void UpdLblScores()
    {
        _lblScores.Text = $"Scores: {_game.Scores:D10}";
    }

    public void OnTimerTick()
    {
        _game.Tick();
        if (_game.IsGameOver)
        {
            GetTree().ChangeScene("res://GameOver/GameOver.tscn");
        }
        else
        {
            _timer.Start();
        }

        UpdLblTime();
    }

    private static Sprite GenSprite(Item item)
    {
        string textureName;
        switch (item.ItemType.Shape)
        {
            case ItemShape.Circle when item.ItemType.Color == 1:
                textureName = "Ball_Green.png";
                break;
            case ItemShape.Circle when item.ItemType.Color == 2:
                textureName = "Ball_Blue.png";
                break;
            case ItemShape.Circle when item.ItemType.Color == 3:
                textureName = "Ball_Grey.png";
                break;
            case ItemShape.Rect when item.ItemType.Color == 1:
                textureName = "Cube_Green.png";
                break;
            case ItemShape.Rect when item.ItemType.Color == 2:
                textureName = "Cube_Blue.png";
                break;
            default:
                textureName = "NoTextureFor" + item.ItemType;
                break;
        }

        return new Sprite
        {
            Texture = GD.Load<Texture>("res://GameScene/" + textureName)
        };
    }

    private void GenSprites()
    {
        for (int y = 0; y < Board.Height; y++)
        {
            for (int x = 0; x < Board.Width; x++)
            {
                Sprite itemSprite = GenSprite(_game.Items[x, y]);
                itemSprite.Scale = new Vector2(5, 5);
                itemSprite.Visible = true;
                itemSprite.Position = ToItemTablePos(x, y);
                _itemSprites[x, y] = itemSprite;
                _itemTable.AddChild(itemSprite);
            }
        }
    }

    private static Vector2 ToItemTablePos(int x, int y)
    {
        // border + (x * textureSize [11 * 5 scale]) + (x * spriteDistance)
        return new Vector2(
            46 + (x * 55) + (x * 17.2f),
            38 + (y * 55) + (y * 5.2f));
    }
}
