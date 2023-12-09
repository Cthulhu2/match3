using Godot;

// ReSharper disable CheckNamespace

public partial class MainMenu : Node2D
{
    private Button _btnPlay;

    public override void _Ready()
    {
        _btnPlay = GetNode<Button>("CanvasLayer/BtnPlay");
        _btnPlay.Connect("pressed", Callable.From(this.OnPlayPressed));
    }

    private void OnPlayPressed()
    {
        GetTree().ChangeSceneToFile("res://GameScene/GameScene.tscn");
    }
}
