using Godot;
using System;

public partial class BuildingTabs : TabContainer
{
	[Export]
	public Label SelectedText;

	public void OnTabSelected(int index)
	{
		if (index >= 0)
		{
			SelectedText.Text = GetTabTitle(index);
			SelectedText.Visible = true;
		}
		else
		{
			SelectedText.Text = "No Tab Selected";
			SelectedText.Visible = false;
		}
	}

    public override void _Ready()
    {
        OnTabSelected(GetCurrentTab());
    }
}
