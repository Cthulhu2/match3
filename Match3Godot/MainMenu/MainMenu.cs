using Godot;

// ReSharper disable UnusedType.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable CheckNamespace

public class MainMenu : Node2D
{
    private Button _btnPlay;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        _btnPlay = GetNode<Button>("CanvasLayer/BtnPlay");
        _btnPlay.Connect("pressed", this, "OnPlayPressed");
    }

//  // Called every frame. 'delta' is the elapsed time since the previous frame.
//  public override void _Process(float delta)
//  {
//      
//  }

    public void OnPlayPressed()
    {
        GetTree().ChangeScene("res://GameScene/GameScene.tscn");
    }
}
