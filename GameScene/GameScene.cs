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

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        _game = new Game(new Board());
        _game.Reset();

        _lblScores = GetNode<Label>(new NodePath("CanvasLayer/LblScores"));
        _lblTime = GetNode<Label>(new NodePath("CanvasLayer/LblTime"));

        _timer = GetNode<Timer>(new NodePath("Timer"));
        _timer.WaitTime = 1; // sec
        _timer.Connect("timeout", this, "OnTimerTick");
        _timer.Start();

        UpdLblTime();
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
}
