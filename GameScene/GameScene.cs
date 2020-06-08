using System;
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
    private Point _selSpritePoint = new Point(-1, -1);

    private ItemHMovTween _itemHMovTween;
    private ItemVMovTween _itemVMovTween;
    private ItemFallTween _itemFallTween;
    private ItemDestroyTween _itemDestroyTween;
    private ItemSpawnTween _itemSpawnTween;

    private Label _lblScores;
    private Label _lblTime;
    private TextureRect _itemTable;
    private Sprite[,] _itemSprites;

    private Queue<IAction> _actions;
    private IAction _curAction;
    private bool _inProcess;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        _game = new Game(new Board());
        _game.Reset();
        _itemSprites = new Sprite[_game.BoardWidth, _game.BoardHeight];
        _actions = new Queue<IAction>();

        _lblScores = GetNode<Label>(new NodePath("Canvas/LblScores"));
        _lblTime = GetNode<Label>(new NodePath("Canvas/LblTime"));
        _itemTable = GetNode<TextureRect>(new NodePath("Canvas/ItemTable"));
        _itemSelTween = GetNode<ItemSelTween>(new NodePath("ItemSelTween"));
        _itemHMovTween = GetNode<ItemHMovTween>(new NodePath("ItemHMovTween"));
        _itemVMovTween = GetNode<ItemVMovTween>(new NodePath("ItemVMovTween"));
        _itemFallTween = GetNode<ItemFallTween>(new NodePath("ItemFallTween"));
        _itemDestroyTween =
            GetNode<ItemDestroyTween>(new NodePath("ItemDestroyTween"));
        _itemSpawnTween =
            GetNode<ItemSpawnTween>(new NodePath("ItemSpawnTween"));


        _timer = GetNode<Timer>(new NodePath("Timer"));
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

        if (_game.CanSwap(_selSpritePoint, new Point(x, y)))
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
        _selSpritePoint = new Point(-1, -1);

        _itemSelTween.TerminateAll();

        foreach (IAction action in actions)
        {
            _actions.Enqueue(action);
        }

        ProcessNextAction();
    }

    private void ProcessNextAction()
    {
        _inProcess = true;
        if (_actions.Count > 0)
        {
            ProcessAction(_actions.Dequeue());
        }
        else
        {
            _inProcess = false;
            _curAction = null;
        }
    }

    public void OnSpawnBonusesTweenEnd()
    {
        ProcessNextAction();
    }
    
    public void OnDestroyActionEnd()
    {
        _itemDestroyTween.RemoveAll();
        if (!(_curAction is DestroyAction dAct))
        {
            GD.PrintErr($"OnDestroyActionEnd. Unknown action: {_curAction}");
            ProcessNextAction();
            return;
        }

        GD.Print($"OnDestroyActionEnd. Remove: {dAct.RegularDestroyedPos}");
        foreach (Point dPos in dAct.RegularDestroyedPos)
        {
            Sprite sprite = _itemSprites[dPos.X, dPos.Y];
            _itemSprites[dPos.X, dPos.Y] = null;
            _itemTable.RemoveChild(sprite);
        }

        // GD.Print("OnDestroyActionEnd. Dump:");
        // foreach (string l in _game.Dump().Split(System.Environment.NewLine))
        // {
        //     GD.Print(l);
        // }
        UpdLblScores();
        if (dAct.SpawnBonuses.Length > 0)
        {
            foreach (SpawnPos spPos in dAct.SpawnBonuses)
            {
                Sprite itemSprite =
                    SpawnSprite(spPos.Pos.X, spPos.Pos.Y, spPos.Item);
                itemSprite.Scale = new Vector2(5, 0);
                itemSprite.Visible = true;
                _itemSpawnTween.Tween(itemSprite);
            }
            _itemSpawnTween.InterpolateCallback(this, "OnSpawnBonusesTweenEnd");
            _itemSpawnTween.Start();
        }
        else
        {
            ProcessNextAction();            
        }
    }

    public void OnFallDownActionEnd()
    {
        _itemFallTween.TerminateAll();
        if (!(_curAction is FallDownAction))
        {
            GD.PrintErr($"OnFallDownActionEnd. Unknown action: {_curAction}");
            ProcessNextAction();
            return;
        }

        GD.Print("OnFallDownActionEnd. Dump:");
        foreach (string l in _game.Dump().Split(System.Environment.NewLine))
        {
            GD.Print(l);
        }

        ProcessNextAction();
    }

    public void OnSpawnActionEnd()
    {
        _itemSpawnTween.RemoveAll();
        //ProcessNextAction();
    }

    private void ProcessAction(IAction action)
    {
        _curAction = action;
        // ReSharper disable once ConvertIfStatementToSwitchStatement
        if (action is SwapAction swAct)
        {
            bool isHorizontal = (swAct.SrcPos.Y == swAct.DestPos.Y);
            Sprite srcSprite = _itemSprites[swAct.SrcPos.X, swAct.SrcPos.Y];
            Sprite destSprite = _itemSprites[swAct.DestPos.X, swAct.DestPos.Y];
            if (isHorizontal)
            {
                _itemHMovTween.RemoveAll();
                _itemHMovTween.Tween(srcSprite, destSprite.Position);
                _itemHMovTween.Tween(destSprite, srcSprite.Position);
                _itemHMovTween.InterpolateCallback(this, "ProcessNextAction");
                _itemHMovTween.Start();
            }
            else
            {
                _itemVMovTween.RemoveAll();
                _itemVMovTween.Tween(srcSprite, destSprite.Position);
                _itemVMovTween.Tween(destSprite, srcSprite.Position);
                _itemVMovTween.InterpolateCallback(this, "ProcessNextAction");
                _itemVMovTween.Start();
            }

            _itemSprites[swAct.DestPos.X, swAct.DestPos.Y] = srcSprite;
            _itemSprites[swAct.SrcPos.X, swAct.SrcPos.Y] = destSprite;
        }
        else if (action is DestroyAction dAct)
        {
            foreach (Point dPos in dAct.RegularDestroyedPos)
            {
                _itemDestroyTween.Tween(_itemSprites[dPos.X, dPos.Y]);
            }
            // TODO: Destroy dAct.LineDestroyedPos/BombDestroyedPos
            _itemDestroyTween.InterpolateCallback(this, "OnDestroyActionEnd");
            _itemDestroyTween.Start();
        }
        else if (action is FallDownAction fAct)
        {
            foreach (FallDownPos fPos in fAct.Positions)
            {
                Sprite sprite = _itemSprites[fPos.SrcPos.X, fPos.SrcPos.Y];
                _itemSprites[fPos.SrcPos.X, fPos.SrcPos.Y] = null;
                _itemSprites[fPos.DestPos.X, fPos.DestPos.Y] = sprite;
                Vector2 toPos = ToItemTablePos(fPos.DestPos.X, fPos.DestPos.Y);
                _itemFallTween.Tween(sprite, toPos);
            }

            if (_actions.Peek() is SpawnAction)
            {
                // Do Spawn and FallDown in parallel
                var spAct = (SpawnAction) _actions.Dequeue();

                foreach (SpawnPos spPos in spAct.Positions)
                {
                    Sprite itemSprite =
                        SpawnSprite(spPos.Pos.X, spPos.Pos.Y, spPos.Item);
                    itemSprite.Scale = new Vector2(5, 0);
                    itemSprite.Visible = true;
                    _itemSpawnTween.Tween(itemSprite);
                }

                _itemSpawnTween.InterpolateCallback(this, "OnSpawnActionEnd");
                _itemSpawnTween.Start();
            }

            _itemFallTween.InterpolateCallback(this, "OnFallDownActionEnd");
            _itemFallTween.Start();
        }
        else
        {
            ProcessNextAction();
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

        for (int y = 0; y < _game.BoardHeight; y++)
        {
            for (int x = 0; x < _game.BoardWidth; x++)
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
        for (int y = 0; y < _game.BoardHeight; y++)
        {
            for (int x = 0; x < _game.BoardWidth; x++)
            {
                SpawnSprite(x, y, _game.Items[x, y])
                    .Visible = true;
            }
        }
    }

    private Sprite SpawnSprite(int x, int y, Item item)
    {
        Sprite sprite = GenSprite(item);

        sprite.Scale = new Vector2(5, 5);
        sprite.Position = ToItemTablePos(x, y);

        _itemSprites[x, y] = sprite;
        _itemTable.AddChild(sprite);

        return sprite;
    }

    private static Vector2 ToItemTablePos(int x, int y)
    {
        // border + (x * textureSize [11 * 5 scale]) + (x * spriteDistance)
        return new Vector2(
            46 + (x * 55) + (x * 17.2f),
            38 + (y * 55) + (y * 5.2f));
    }
}
