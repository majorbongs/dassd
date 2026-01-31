using CitizenFX.Core;
using CitizenFX.Core.Native;

namespace NativeUI;

public class InstructionalButton
{
	private readonly string _buttonString;

	private readonly Control _buttonControl;

	private readonly bool _usingControls;

	public string Text { get; set; }

	public UIMenuItem ItemBind { get; private set; }

	public InstructionalButton(Control control, string text)
	{
		//IL_000e: Unknown result type (might be due to invalid IL or missing references)
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		Text = text;
		_buttonControl = control;
		_usingControls = true;
	}

	public InstructionalButton(string keystring, string text)
	{
		Text = text;
		_buttonString = keystring;
		_usingControls = false;
	}

	public void BindToItem(UIMenuItem item)
	{
		ItemBind = item;
	}

	public string GetButtonId()
	{
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		//IL_0026: Expected I4, but got Unknown
		if (!_usingControls)
		{
			return "t_" + _buttonString;
		}
		return API.GetControlInstructionalButton(2, (int)_buttonControl, 0);
	}
}
