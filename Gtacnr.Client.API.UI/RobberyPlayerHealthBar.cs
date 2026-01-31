using System.Linq;
using CitizenFX.Core;
using Gtacnr.Client.Anticheat;
using Gtacnr.Client.HUD;
using Gtacnr.Model;
using NativeUI;

namespace Gtacnr.Client.API.UI;

public class RobberyPlayerHealthBar : BarTimerBar
{
	public int PlayerId { get; set; }

	public RobberyPlayerHealthBar(int playerId)
		: base("", 1f)
	{
		PlayerId = playerId;
		Refresh();
	}

	public void Refresh()
	{
		Player val = MainScript.GetPlayerList().FirstOrDefault((Player p) => p.ServerId == PlayerId);
		if (val != (Player)null)
		{
			TimerBarScript.AddTimerBar(this);
			PlayerState playerState = LatentPlayers.Get(PlayerId);
			int? obj;
			if (val == null)
			{
				obj = null;
			}
			else
			{
				Ped character = val.Character;
				obj = ((character != null) ? new int?(((Entity)character).Health) : ((int?)null));
			}
			int? num = obj;
			int valueOrDefault = num.GetValueOrDefault();
			int? obj2;
			if (val == null)
			{
				obj2 = null;
			}
			else
			{
				Ped character2 = val.Character;
				obj2 = ((character2 != null) ? new int?(character2.Armor) : ((int?)null));
			}
			num = obj2;
			int val2 = valueOrDefault + num.GetValueOrDefault();
			int num2 = AntiHealthLockScript.MaxHealth - 100 + AntiHealthLockScript.MaxArmor;
			val2 = val2.Clamp(0, num2);
			base.Label = playerState.ColorTextCode + playerState.Name.ToUpperInvariant();
			base.Percentage = Gtacnr.Utils.ConvertRange(val2, 0f, num2, 0f, 1f);
			base.Color = ((base.Percentage == 0f) ? BarColors.Gray : ((base.Percentage >= 0.6f) ? BarColors.Blue : BarColors.Red));
		}
		else
		{
			TimerBarScript.RemoveTimerBar(this);
		}
	}

	public void Remove()
	{
		PlayerId = 0;
		TimerBarScript.RemoveTimerBar(this);
	}
}
