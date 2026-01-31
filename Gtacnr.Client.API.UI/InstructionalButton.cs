using CitizenFX.Core;

namespace Gtacnr.Client.API.UI;

public struct InstructionalButton
{
	public string Text { get; set; }

	public int ControlGroup { get; set; }

	public int Control { get; set; }

	public InstructionalButton(string text, int controlGroup, int control)
	{
		Text = text;
		ControlGroup = controlGroup;
		Control = control;
	}

	public InstructionalButton(string text, int controlGroup, Control control)
	{
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Expected I4, but got Unknown
		Text = text;
		ControlGroup = controlGroup;
		Control = (int)control;
	}
}
