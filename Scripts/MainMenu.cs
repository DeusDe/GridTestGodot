using Godot;
using System;

public partial class MainMenu : Control
{
	[Export] Button _startButton;
	[Export] SpinBox _heightSpin;
	[Export] SpinBox _widthSpin;
	[Export] PackedScene _gameOfLifeScene;
	
	public override void _Ready()
    {
        _startButton.Pressed += OnStartButtonPressed;
    }


	public override void _Process(double delta)
	{
	}


	private void OnStartButtonPressed()
    {
        var GOLInstance = _gameOfLifeScene.Instantiate()  as GameOfLife;
		GOLInstance.GridWidth = (int)_widthSpin.Value;
		GOLInstance.GridHeight = (int)_heightSpin.Value;
		GetParent().AddChild(GOLInstance);
		QueueFree();

    }
}
