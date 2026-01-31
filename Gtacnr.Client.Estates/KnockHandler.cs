using System;
using System.Threading.Tasks;
using CitizenFX.Core;
using Gtacnr.Client.API.Scripts;
using Gtacnr.Client.API.UI;
using Gtacnr.Client.Jobs.Police.Arrest;
using Gtacnr.Localization;
using Gtacnr.Model;

namespace Gtacnr.Client.Estates;

public abstract class KnockHandler
{
	private static readonly Control ANSWER_KEYBOARD_CONTROL = (Control)246;

	private static readonly Control ANSWER_GAMEPAD_CONTROL = (Control)303;

	protected KnockInfo knockInfo;

	private readonly TimeSpan KnockingTimeout = TimeSpan.FromSeconds(8.0);

	public async Task Knock(KnockInfo newKnock)
	{
		knockInfo = newKnock;
		EnableAnswerKeys();
		await Utils.Delay(KnockingTimeout.TotalMilliseconds.ToInt());
		if (knockInfo != null)
		{
			Utils.DisplayHelpText();
			DisableAnswerKeys();
			knockInfo = null;
		}
	}

	public bool IsKnockActive()
	{
		return knockInfo != null;
	}

	private void EnableAnswerKeys()
	{
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		//IL_0054: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0021: Unknown result type (might be due to invalid IL or missing references)
		if (Utils.IsUsingKeyboard())
		{
			Utils.AddInstructionalButton("answerKnock", new InstructionalButton("Answer", 2, ANSWER_KEYBOARD_CONTROL));
			KeysScript.AttachListener(ANSWER_KEYBOARD_CONTROL, OnAnswerKeyEvent, 100);
		}
		else
		{
			Utils.AddInstructionalButton("answerKnock", new InstructionalButton("Answer (hold)", 2, ANSWER_GAMEPAD_CONTROL));
			KeysScript.AttachListener(ANSWER_GAMEPAD_CONTROL, OnAnswerKeyEvent, 100);
		}
	}

	private void DisableAnswerKeys()
	{
		Utils.RemoveInstructionalButton("answerKnock");
		KeysScript.DetachListener((Control)246, OnAnswerKeyEvent);
		KeysScript.DetachListener((Control)303, OnAnswerKeyEvent);
	}

	private bool OnAnswerKeyEvent(Control control, KeyEventType eventType, InputType inputType)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		//IL_0006: Invalid comparison between Unknown and I4
		//IL_000f: Unknown result type (might be due to invalid IL or missing references)
		//IL_0015: Invalid comparison between Unknown and I4
		bool num = (int)control == 246 && eventType == KeyEventType.JustPressed;
		bool flag = (int)control == 303 && eventType == KeyEventType.Held;
		if (num || flag)
		{
			if (knockInfo == null || Gtacnr.Utils.CheckTimePassed(knockInfo.Timestamp, KnockingTimeout))
			{
				return false;
			}
			if (CuffedScript.IsCuffed || CuffedScript.IsBeingCuffedOrUncuffed)
			{
				Utils.DisplayHelpText(LocalizationController.S(Entries.Vehicles.CANT_DO_WHEN_CUFFED));
				return false;
			}
			OnAnswer();
			DisableAnswerKeys();
			knockInfo = null;
			return true;
		}
		return false;
	}

	protected abstract void OnAnswer();
}
