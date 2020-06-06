using GameEngine;
using Godot;

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable CheckNamespace

public class GameScene : Node2D
{
    private Game _game;

    private Timer _timer;

    private Label _lblScores;
    private Label _lblTime;
    private TextureRect _itemTable;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        _game = new Game(new Board());
        _game.Reset();

        _lblScores = GetNode<Label>(new NodePath("Canvas/LblScores"));
        _lblTime = GetNode<Label>(new NodePath("Canvas/LblTime"));
        _itemTable = GetNode<TextureRect>(new NodePath("Canvas/ItemTable"));

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
