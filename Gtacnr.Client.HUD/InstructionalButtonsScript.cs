using System.Collections.Generic;
using System.Linq;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.API.UI;
using MenuAPI;

namespace Gtacnr.Client.HUD;

public class InstructionalButtonsScript : Script
{
	public enum KeyboardButtons
	{
		A,
		B,
		C,
		D,
		E,
		F,
		G,
		H,
		I,
		J,
		K,
		L,
		M,
		N,
		O,
		P,
		Q,
		R,
		S,
		T,
		U,
		V,
		W,
		X,
		Y,
		Z,
		_1,
		_2,
		_3,
		_4,
		_5,
		_6,
		_7,
		_8,
		_9,
		_0,
		DOT,
		COMA,
		EQUALS,
		DASH,
		L_BRACKET,
		R_BRACKET,
		GRAVE,
		MOUSE_LEFT_CLICK,
		MOUSE_RIGHT_CLICK,
		MOUSE_MIDDLE_CLICK,
		MOUSE_EXTRA1,
		MOUSE_EXTRA2,
		MOUSE_EXTRA3,
		MOUSE_EXTRA4,
		MOUSE_EXTRA5,
		MOUSE_EXTRA6,
		MOUSE_EXTRA7,
		MOUSE_EXTRA8,
		MOUSE_WHEEL_UP,
		MOUSE_WHEEL_DOWN,
		NUM_SUBSTRACT,
		NUM_ADD,
		NUM_MULTIPLY,
		NUM_ENTER,
		NUM_1,
		NUM_2,
		NUM_3,
		NUM_4,
		NUM_5,
		NUM_6,
		NUM_7,
		NUM_8,
		NUM_9,
		F1,
		F2,
		F3,
		F4,
		F5,
		F6,
		F7,
		F8,
		F9,
		F10,
		F11,
		F12,
		F13,
		F14,
		F15,
		F16,
		F17,
		F18,
		F19,
		F20,
		F21,
		F22,
		F23,
		F24,
		ARROW_UP,
		ARROW_DOWN,
		ARROW_LEFT,
		ARROW_RIGHT,
		DELETE,
		ESCAPE,
		INSERT,
		END,
		SHIFT,
		TAB,
		ENTER,
		BACKSPACE,
		HOME,
		PAGE_UP,
		PAGE_DOWN,
		CAPS_LOCK,
		CONTROL,
		RIGHT_CONTROL,
		LEFT_ALT,
		SPACE
	}

	private static Dictionary<string, InstructionalButton> instructionalButtons = new Dictionary<string, InstructionalButton>();

	private static int currentScaleform = -1;

	private static bool wasAnyMenuOpen = false;

	public static bool AreInstructionsActive => currentScaleform != -1;

	private static KeyboardButtons? SingleButtonTextToEnum(string button)
	{
		return button switch
		{
			"t_A" => KeyboardButtons.A, 
			"t_B" => KeyboardButtons.B, 
			"t_C" => KeyboardButtons.C, 
			"t_D" => KeyboardButtons.D, 
			"t_E" => KeyboardButtons.E, 
			"t_F" => KeyboardButtons.F, 
			"t_G" => KeyboardButtons.G, 
			"t_H" => KeyboardButtons.H, 
			"t_I" => KeyboardButtons.I, 
			"t_J" => KeyboardButtons.J, 
			"t_K" => KeyboardButtons.K, 
			"t_L" => KeyboardButtons.L, 
			"t_M" => KeyboardButtons.M, 
			"t_N" => KeyboardButtons.N, 
			"t_O" => KeyboardButtons.O, 
			"t_P" => KeyboardButtons.P, 
			"t_Q" => KeyboardButtons.Q, 
			"t_R" => KeyboardButtons.R, 
			"t_S" => KeyboardButtons.S, 
			"t_T" => KeyboardButtons.T, 
			"t_U" => KeyboardButtons.U, 
			"t_V" => KeyboardButtons.V, 
			"t_W" => KeyboardButtons.W, 
			"t_X" => KeyboardButtons.X, 
			"t_Y" => KeyboardButtons.Y, 
			"t_Z" => KeyboardButtons.Z, 
			"t_1" => KeyboardButtons._1, 
			"t_2" => KeyboardButtons._2, 
			"t_3" => KeyboardButtons._3, 
			"t_4" => KeyboardButtons._4, 
			"t_5" => KeyboardButtons._5, 
			"t_6" => KeyboardButtons._6, 
			"t_7" => KeyboardButtons._7, 
			"t_8" => KeyboardButtons._8, 
			"t_9" => KeyboardButtons._9, 
			"t_0" => KeyboardButtons._0, 
			"t_." => KeyboardButtons.DOT, 
			"t_," => KeyboardButtons.COMA, 
			"t_=" => KeyboardButtons.EQUALS, 
			"t_-" => KeyboardButtons.DASH, 
			"t_[" => KeyboardButtons.L_BRACKET, 
			"t_]" => KeyboardButtons.R_BRACKET, 
			"t_`" => KeyboardButtons.GRAVE, 
			"b_100" => KeyboardButtons.MOUSE_LEFT_CLICK, 
			"b_101" => KeyboardButtons.MOUSE_RIGHT_CLICK, 
			"b_102" => KeyboardButtons.MOUSE_MIDDLE_CLICK, 
			"b_103" => KeyboardButtons.MOUSE_EXTRA1, 
			"b_104" => KeyboardButtons.MOUSE_EXTRA2, 
			"b_105" => KeyboardButtons.MOUSE_EXTRA3, 
			"b_106" => KeyboardButtons.MOUSE_EXTRA4, 
			"b_107" => KeyboardButtons.MOUSE_EXTRA5, 
			"b_108" => KeyboardButtons.MOUSE_EXTRA6, 
			"b_109" => KeyboardButtons.MOUSE_EXTRA7, 
			"b_110" => KeyboardButtons.MOUSE_EXTRA8, 
			"b_115" => KeyboardButtons.MOUSE_WHEEL_UP, 
			"b_116" => KeyboardButtons.MOUSE_WHEEL_DOWN, 
			"b_130" => KeyboardButtons.NUM_SUBSTRACT, 
			"b_131" => KeyboardButtons.NUM_ADD, 
			"b_134" => KeyboardButtons.NUM_MULTIPLY, 
			"b_135" => KeyboardButtons.NUM_ENTER, 
			"b_137" => KeyboardButtons.NUM_1, 
			"b_138" => KeyboardButtons.NUM_2, 
			"b_139" => KeyboardButtons.NUM_3, 
			"b_140" => KeyboardButtons.NUM_4, 
			"b_141" => KeyboardButtons.NUM_5, 
			"b_142" => KeyboardButtons.NUM_6, 
			"b_143" => KeyboardButtons.NUM_7, 
			"b_144" => KeyboardButtons.NUM_8, 
			"b_145" => KeyboardButtons.NUM_9, 
			"b_170" => KeyboardButtons.F1, 
			"b_171" => KeyboardButtons.F2, 
			"b_172" => KeyboardButtons.F3, 
			"b_173" => KeyboardButtons.F4, 
			"b_174" => KeyboardButtons.F5, 
			"b_175" => KeyboardButtons.F6, 
			"b_176" => KeyboardButtons.F7, 
			"b_177" => KeyboardButtons.F8, 
			"b_178" => KeyboardButtons.F9, 
			"b_179" => KeyboardButtons.F10, 
			"b_180" => KeyboardButtons.F11, 
			"b_181" => KeyboardButtons.F12, 
			"b_182" => KeyboardButtons.F13, 
			"b_183" => KeyboardButtons.F14, 
			"b_184" => KeyboardButtons.F15, 
			"b_185" => KeyboardButtons.F16, 
			"b_186" => KeyboardButtons.F17, 
			"b_187" => KeyboardButtons.F18, 
			"b_188" => KeyboardButtons.F19, 
			"b_189" => KeyboardButtons.F20, 
			"b_190" => KeyboardButtons.F21, 
			"b_191" => KeyboardButtons.F22, 
			"b_192" => KeyboardButtons.F23, 
			"b_193" => KeyboardButtons.F24, 
			"b_194" => KeyboardButtons.ARROW_UP, 
			"b_195" => KeyboardButtons.ARROW_DOWN, 
			"b_196" => KeyboardButtons.ARROW_LEFT, 
			"b_197" => KeyboardButtons.ARROW_RIGHT, 
			"b_198" => KeyboardButtons.DELETE, 
			"b_199" => KeyboardButtons.ESCAPE, 
			"b_200" => KeyboardButtons.INSERT, 
			"b_201" => KeyboardButtons.END, 
			"b_210" => KeyboardButtons.DELETE, 
			"b_211" => KeyboardButtons.INSERT, 
			"b_212" => KeyboardButtons.END, 
			"b_1000" => KeyboardButtons.SHIFT, 
			"b_1002" => KeyboardButtons.TAB, 
			"b_1003" => KeyboardButtons.ENTER, 
			"b_1004" => KeyboardButtons.BACKSPACE, 
			"b_1008" => KeyboardButtons.HOME, 
			"b_1009" => KeyboardButtons.PAGE_UP, 
			"b_1010" => KeyboardButtons.PAGE_DOWN, 
			"b_1012" => KeyboardButtons.CAPS_LOCK, 
			"b_1013" => KeyboardButtons.CONTROL, 
			"b_1014" => KeyboardButtons.RIGHT_CONTROL, 
			"b_1015" => KeyboardButtons.LEFT_ALT, 
			"b_1055" => KeyboardButtons.HOME, 
			"b_1056" => KeyboardButtons.PAGE_UP, 
			"b_2000" => KeyboardButtons.SPACE, 
			_ => null, 
		};
	}

	private static KeyboardButtons[] ButtonsTextToEnum(string btn)
	{
		return (from b in btn.Split('%')
			select SingleButtonTextToEnum(b) into i
			where i.HasValue
			select i.Value).ToArray();
	}

	[Update]
	private async Coroutine DrawTask()
	{
		bool num = MenuController.IsAnyMenuOpen();
		if (!num)
		{
			if (wasAnyMenuOpen)
			{
				RefreshInstructionalButtons();
			}
			if (currentScaleform != -1)
			{
				API.DrawScaleformMovieFullscreen(currentScaleform, 255, 255, 255, 255, 0);
			}
		}
		wasAnyMenuOpen = num;
	}

	[EventHandler("gtacnr:hud:addInstructionalButton")]
	private void OnAddInstructionalButton(string key, string ibtnJson)
	{
		InstructionalButton instructionalButton = ibtnJson.Unjson<InstructionalButton>();
		AddInstructionalButton(key, instructionalButton);
	}

	[EventHandler("gtacnr:hud:removeInstructionalButton")]
	private void OnRemoveInstructionalButton(string key)
	{
		RemoveInstructionalButton(key);
	}

	[EventHandler("gtacnr:hud:clearInstructionalButtons")]
	private void OnClearInstructionalButtons()
	{
		ClearInstructionalButtons();
	}

	public static void AddInstructionalButton(string key, InstructionalButton instructionalButton)
	{
		instructionalButtons[key] = instructionalButton;
		RefreshInstructionalButtons();
	}

	public static void RemoveInstructionalButton(string key)
	{
		if (instructionalButtons.ContainsKey(key))
		{
			instructionalButtons.Remove(key);
		}
		RefreshInstructionalButtons();
	}

	public static void ClearInstructionalButtons()
	{
		currentScaleform = -1;
	}

	public static async void RefreshInstructionalButtons()
	{
		currentScaleform = API.RequestScaleformMovie("instructional_buttons");
		while (!API.HasScaleformMovieLoaded(currentScaleform))
		{
			await BaseScript.Delay(0);
		}
		API.DrawScaleformMovieFullscreen(currentScaleform, 255, 255, 255, 0, 0);
		API.PushScaleformMovieFunction(currentScaleform, "CLEAR_ALL");
		API.PopScaleformMovieFunctionVoid();
		API.PushScaleformMovieFunction(currentScaleform, "SET_CLEAR_SPACE");
		API.PushScaleformMovieFunctionParameterInt(200);
		API.PopScaleformMovieFunctionVoid();
		if (instructionalButtons.Count == 0)
		{
			currentScaleform = -1;
			return;
		}
		int num = 0;
		foreach (InstructionalButton value in instructionalButtons.Values)
		{
			API.PushScaleformMovieFunction(currentScaleform, "SET_DATA_SLOT");
			API.PushScaleformMovieFunctionParameterInt(num);
			_Button(value.ControlGroup, value.Control);
			_ButtonLabel(value.Text);
			API.PopScaleformMovieFunctionVoid();
			num++;
		}
		API.PushScaleformMovieFunction(currentScaleform, "DRAW_INSTRUCTIONAL_BUTTONS");
		API.PopScaleformMovieFunctionVoid();
		API.PushScaleformMovieFunction(currentScaleform, "SET_BACKGROUND_COLOUR");
		API.PushScaleformMovieFunctionParameterInt(0);
		API.PushScaleformMovieFunctionParameterInt(0);
		API.PushScaleformMovieFunctionParameterInt(0);
		API.PushScaleformMovieFunctionParameterInt(80);
		API.PopScaleformMovieFunctionVoid();
		static void _Button(int group, int control)
		{
			API.ScaleformMovieMethodAddParamPlayerNameString(API.GetControlInstructionalButton(group, control, 1));
		}
		static void _ButtonLabel(string text)
		{
			API.BeginTextCommandScaleformString("STRING");
			API.AddTextComponentScaleform(text.AddFontTags());
			API.EndTextCommandScaleformString();
		}
	}
}
