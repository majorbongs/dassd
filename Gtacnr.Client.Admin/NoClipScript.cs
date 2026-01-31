using System.Collections.Generic;
using CitizenFX.Core;
using CitizenFX.Core.Native;
using Gtacnr.Client.API.UI;

namespace Gtacnr.Client.Admin;

public class NoClipScript : Script
{
	private static bool isNoClipActive = false;

	private static readonly float INITIAL_SPEED = 1f;

	private static readonly float MIN_SPEED = 0.1f;

	private static readonly float MAX_SPEED = 30f;

	private bool instructionsShown;

	private bool wasUsingKeyboard;

	private bool wasNoClipActive;

	private Dictionary<string, Control> keyboardControls = new Dictionary<string, Control>
	{
		{
			"speed_up",
			(Control)241
		},
		{
			"speed_down",
			(Control)242
		},
		{
			"move_up",
			(Control)209
		},
		{
			"move_down",
			(Control)203
		},
		{
			"move_forward",
			(Control)32
		},
		{
			"move_backward",
			(Control)33
		},
		{
			"move_left",
			(Control)34
		},
		{
			"move_right",
			(Control)35
		},
		{
			"exit",
			(Control)(-1)
		}
	};

	private Dictionary<string, Control> gamepadControls = new Dictionary<string, Control>
	{
		{
			"speed_up",
			(Control)206
		},
		{
			"speed_down",
			(Control)205
		},
		{
			"move_up",
			(Control)208
		},
		{
			"move_down",
			(Control)207
		},
		{
			"move_forward",
			(Control)32
		},
		{
			"move_backward",
			(Control)33
		},
		{
			"move_left",
			(Control)34
		},
		{
			"move_right",
			(Control)35
		},
		{
			"exit",
			(Control)194
		}
	};

	public static bool IsNoClipActive
	{
		get
		{
			return isNoClipActive;
		}
		set
		{
			if (SpectateScript.IsSpectating)
			{
				Utils.PlayErrorSound();
				return;
			}
			isNoClipActive = value;
			if (!value)
			{
				((Entity)Game.PlayerPed).IsInvincible = false;
			}
		}
	}

	private float moveSpeed { get; set; } = INITIAL_SPEED;

	public NoClipScript()
	{
		StaffLevelScript.StaffLevelInitializedOrChanged += OnStaffLevelInitializedOrChanged;
	}

	private async void OnStaffLevelInitializedOrChanged(object sender, StaffLevelArgs e)
	{
		if ((int)e.PreviousStaffLevel < 100 && (int)e.NewStaffLevel >= 100)
		{
			base.Update += NoClipTask;
		}
		else if ((int)e.PreviousStaffLevel >= 100 && (int)e.NewStaffLevel < 100)
		{
			IsNoClipActive = false;
			await BaseScript.Delay(500);
			base.Update -= NoClipTask;
		}
	}

	private Control GetControl(string ctrlName)
	{
		//IL_0001: Unknown result type (might be due to invalid IL or missing references)
		//IL_0029: Unknown result type (might be due to invalid IL or missing references)
		Control value = (Control)(-1);
		if (Utils.IsUsingKeyboard())
		{
			keyboardControls.TryGetValue(ctrlName, out value);
		}
		else
		{
			gamepadControls.TryGetValue(ctrlName, out value);
		}
		return value;
	}

	private bool IsPressed(string ctrlName)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		Control control = GetControl(ctrlName);
		if (!Game.IsControlPressed(2, control))
		{
			return Game.IsDisabledControlPressed(2, control);
		}
		return true;
	}

	private bool IsJustPressed(string ctrlName)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		Control control = GetControl(ctrlName);
		if (!Game.IsControlJustPressed(2, control))
		{
			return Game.IsDisabledControlJustPressed(2, control);
		}
		return true;
	}

	private bool IsJustReleased(string ctrlName)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_0009: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		Control control = GetControl(ctrlName);
		if (!Game.IsControlJustReleased(2, control))
		{
			return Game.IsDisabledControlJustReleased(2, control);
		}
		return true;
	}

	private async Coroutine NoClipTask()
	{
		int num = (Game.PlayerPed.IsInVehicle() ? ((PoolObject)Game.PlayerPed.CurrentVehicle).Handle : ((PoolObject)Game.PlayerPed).Handle);
		if (IsNoClipActive)
		{
			if (((Entity)Game.PlayerPed).IsDead)
			{
				IsNoClipActive = false;
				return;
			}
			API.FreezeEntityPosition(num, true);
			API.SetEntityInvincible(num, true);
			DisableControls();
			float num2 = 0f;
			float num3 = 0f;
			float num4 = 0f;
			if (!Utils.IsOnScreenKeyboardActive && !Game.IsPaused)
			{
				bool flag = Utils.IsUsingKeyboard();
				if ((wasUsingKeyboard && !flag) || (!wasUsingKeyboard && flag))
				{
					instructionsShown = false;
					EnableInstructionalButtons();
				}
				wasUsingKeyboard = flag;
				if (IsPressed("speed_up"))
				{
					moveSpeed += 0.25f;
					if (moveSpeed > 5f)
					{
						moveSpeed += 1f;
					}
					if (moveSpeed > 15f)
					{
						moveSpeed += 1.5f;
					}
					if (moveSpeed > 25f)
					{
						moveSpeed += 3f;
					}
					if (moveSpeed > MAX_SPEED)
					{
						moveSpeed = MAX_SPEED;
					}
				}
				else if (IsPressed("speed_down"))
				{
					moveSpeed -= 0.25f;
					if (moveSpeed > 5f)
					{
						moveSpeed -= 1f;
					}
					if (moveSpeed > 15f)
					{
						moveSpeed -= 1.5f;
					}
					if (moveSpeed > 25f)
					{
						moveSpeed -= 3f;
					}
					if (moveSpeed < MIN_SPEED)
					{
						moveSpeed = MIN_SPEED;
					}
				}
				if (IsPressed("move_forward"))
				{
					num3 = 0.5f;
				}
				else if (IsPressed("move_backward"))
				{
					num3 = -0.5f;
				}
				if (IsPressed("move_left"))
				{
					num2 = ((!flag) ? (-0.1f) : (-0.5f));
				}
				else if (IsPressed("move_right"))
				{
					num2 = ((!flag) ? 0.1f : 0.5f);
				}
				if (IsPressed("move_up"))
				{
					num4 = 0.21f;
				}
				else if (IsPressed("move_down"))
				{
					num4 = -0.21f;
				}
				if (IsJustPressed("exit"))
				{
					IsNoClipActive = false;
					return;
				}
			}
			if (num2 != 0f || num3 != 0f || num4 != 0f)
			{
				float num5 = moveSpeed / (1f / API.GetFrameTime()) * 60f;
				Vector3 offsetFromEntityInWorldCoords = API.GetOffsetFromEntityInWorldCoords(num, num2 * num5, num3 * num5, num4 * num5);
				API.SetEntityCoordsNoOffset(num, offsetFromEntityInWorldCoords.X, offsetFromEntityInWorldCoords.Y, offsetFromEntityInWorldCoords.Z, true, true, true);
			}
			API.SetEntityVelocity(num, 0f, 0f, 0f);
			API.SetEntityRotation(num, 0f, 0f, 0f, 0, false);
			API.SetEntityHeading(num, API.GetGameplayCamRelativeHeading());
			API.SetEntityCollision(num, false, false);
			API.SetEntityVisible(num, false, false);
			API.SetLocalPlayerVisibleLocally(true);
			API.SetEntityAlpha(num, 50, 0);
			EnableInstructionalButtons();
			if (!wasNoClipActive)
			{
				wasNoClipActive = true;
			}
		}
		else if (wasNoClipActive)
		{
			wasNoClipActive = false;
			API.FreezeEntityPosition(num, false);
			API.SetEntityInvincible(num, false);
			API.SetEntityCollision(num, true, true);
			API.SetEntityVisible(num, true, false);
			API.SetLocalPlayerVisibleLocally(true);
			API.ResetEntityAlpha(num);
			DisableInstructionalButtons();
		}
	}

	private void EnableInstructionalButtons()
	{
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		//IL_0041: Unknown result type (might be due to invalid IL or missing references)
		//IL_0061: Unknown result type (might be due to invalid IL or missing references)
		//IL_0081: Unknown result type (might be due to invalid IL or missing references)
		//IL_00a1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00c1: Unknown result type (might be due to invalid IL or missing references)
		//IL_00e1: Unknown result type (might be due to invalid IL or missing references)
		//IL_0101: Unknown result type (might be due to invalid IL or missing references)
		if (!instructionsShown)
		{
			instructionsShown = true;
			Utils.AddInstructionalButton("noclipSpdDown", new InstructionalButton("Speed -", 2, GetControl("speed_down")));
			Utils.AddInstructionalButton("noclipSpdUp", new InstructionalButton("Speed +", 2, GetControl("speed_up")));
			Utils.AddInstructionalButton("noclipDown", new InstructionalButton("Down", 2, GetControl("move_down")));
			Utils.AddInstructionalButton("noclipUp", new InstructionalButton("Up", 2, GetControl("move_up")));
			Utils.AddInstructionalButton("noclipRight", new InstructionalButton("Right", 2, GetControl("move_right")));
			Utils.AddInstructionalButton("noclipLeft", new InstructionalButton("Left", 2, GetControl("move_left")));
			Utils.AddInstructionalButton("noclipBwd", new InstructionalButton("Backward", 2, GetControl("move_backward")));
			Utils.AddInstructionalButton("noclipFwd", new InstructionalButton("Forward", 2, GetControl("move_forward")));
		}
	}

	private void DisableInstructionalButtons()
	{
		if (instructionsShown)
		{
			instructionsShown = false;
			Utils.RemoveInstructionalButton("noclipFwd");
			Utils.RemoveInstructionalButton("noclipBwd");
			Utils.RemoveInstructionalButton("noclipLeft");
			Utils.RemoveInstructionalButton("noclipRight");
			Utils.RemoveInstructionalButton("noclipUp");
			Utils.RemoveInstructionalButton("noclipDown");
			Utils.RemoveInstructionalButton("noclipSpdUp");
			Utils.RemoveInstructionalButton("noclipSpdDown");
		}
	}

	private void DisableControls()
	{
		Game.DisableControlThisFrame(0, (Control)32);
		Game.DisableControlThisFrame(0, (Control)268);
		Game.DisableControlThisFrame(0, (Control)31);
		Game.DisableControlThisFrame(0, (Control)269);
		Game.DisableControlThisFrame(0, (Control)33);
		Game.DisableControlThisFrame(0, (Control)266);
		Game.DisableControlThisFrame(0, (Control)34);
		Game.DisableControlThisFrame(0, (Control)30);
		Game.DisableControlThisFrame(0, (Control)267);
		Game.DisableControlThisFrame(0, (Control)35);
		Game.DisableControlThisFrame(0, (Control)22);
		Game.DisableControlThisFrame(0, (Control)21);
		Game.DisableControlThisFrame(0, (Control)25);
		Game.DisableControlThisFrame(0, (Control)24);
		Game.DisableControlThisFrame(0, (Control)257);
		Game.DisableControlThisFrame(0, (Control)263);
		Game.DisableControlThisFrame(0, (Control)264);
		Game.DisableControlThisFrame(0, (Control)142);
		Game.DisableControlThisFrame(0, (Control)141);
		Game.DisableControlThisFrame(0, (Control)140);
		Game.DisableControlThisFrame(0, (Control)143);
		Game.DisableControlThisFrame(0, (Control)16);
		Game.DisableControlThisFrame(0, (Control)17);
		Game.DisableControlThisFrame(0, (Control)37);
		Game.DisableControlThisFrame(0, (Control)44);
		if (Game.PlayerPed.IsInVehicle())
		{
			Game.DisableControlThisFrame(0, (Control)85);
		}
	}
}
