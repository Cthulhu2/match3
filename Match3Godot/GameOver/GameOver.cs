using Godot;

// ReSharper disable UnusedType.Global
// ReSharper disable CheckNamespace

public class GameOver : Node2D
{
    private Button _btnOk;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
    {
        _btnOk = GetNode<Button>("CanvasLayer/BtnOk");
        _btnOk.Connect("pressed", this, nameof(OnOkPressed));
    }

//  // Called every frame. 'delta' is the elapsed time since the previous frame.
//  public override void _Process(float delta)
//  {
//      
//  }

    private void OnOkPressed()
    {
        GetTree().ChangeScene("res://MainMenu/MainMenu.tscn");
    }
}
