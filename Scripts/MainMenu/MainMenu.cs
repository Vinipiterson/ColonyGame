using Godot;
using System;

public partial class MainMenu : Control
{
	public void _on_play_btn_pressed()
	{
		GetTree().ChangeSceneToFile("res://Scenes/Main.tscn");
	}

	public void _on_settings_btn_pressed()
	{
		GD.Print("Settings Btn");
	}

	public void _on_quit_btn_pressed()
	{
		GetTree().Quit();
	}
}
