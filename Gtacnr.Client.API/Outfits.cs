using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Gtacnr.Model;

namespace Gtacnr.Client.API;

public static class Outfits
{
	private class InnerScript : Script
	{
		public static InnerScript Instance { get; private set; }

		public InnerScript()
		{
			Instance = this;
		}

		public async Task<Apparel> GetOutfit(string job, int slot)
		{
			string text = await TriggerServerEventAsync<string>("gtacnr:outfits:get", new object[3] { null, job, slot });
			if (string.IsNullOrWhiteSpace(text))
			{
				return Apparel.GetDefault();
			}
			return new Apparel(text.Unjson<List<string>>());
		}

		public async Task<bool> SaveOutfit(string job, int slot, Apparel apparel)
		{
			return await TriggerServerEventAsync<bool>("gtacnr:outfits:save", new object[4]
			{
				null,
				job,
				slot,
				apparel.Items.ToList().Json()
			});
		}

		public async Task<bool> UseOutfit(string job, int slot)
		{
			return await TriggerServerEventAsync<bool>("gtacnr:outfits:use", new object[3] { null, job, slot });
		}
	}

	public static async Task<Apparel> GetOutfit(string job, int slot)
	{
		return await InnerScript.Instance.GetOutfit(job, slot);
	}

	public static async Task<bool> SaveOutfit(string job, int slot, Apparel apparel)
	{
		return await InnerScript.Instance.SaveOutfit(job, slot, apparel);
	}

	public static async Task<bool> UseOutfit(string job, int slot)
	{
		return await InnerScript.Instance.UseOutfit(job, slot);
	}
}
