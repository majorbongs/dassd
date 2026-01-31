using System;
using System.Threading.Tasks;
using CitizenFX.Core;
using CitizenFX.Core.Native;

namespace Gtacnr.Client.API;

public class DisposableModel : IDisposable
{
	public static readonly TimeSpan DEFAULT_TIMEOUT = TimeSpan.FromSeconds(60.0);

	private bool disposed;

	public Model Model { get; private set; }

	public TimeSpan TimeOut { get; set; } = DEFAULT_TIMEOUT;

	public DisposableModel(Model model)
	{
		if (!API.IsModelInCdimage(Model.op_Implicit(model)))
		{
			throw new ArgumentException($"The requested model (0x{Model.op_Implicit(model):X}) is not in the CD image.", "model");
		}
		Model = model;
	}

	public DisposableModel(uint modelHash)
		: this(Model.op_Implicit((int)modelHash))
	{
	}

	public async Task Load()
	{
		if (disposed)
		{
			throw new ObjectDisposedException($"0x{Model.Hash:X}");
		}
		API.RequestModel(Model.op_Implicit(Model));
		DateTime t = DateTime.UtcNow;
		while (!API.HasModelLoaded(Model.op_Implicit(Model)) && !Gtacnr.Utils.CheckTimePassed(t, TimeOut))
		{
			await BaseScript.Delay(0);
		}
		if (Gtacnr.Utils.CheckTimePassed(t, TimeOut))
		{
			throw new TimeoutException($"The requested model (0x{Model.op_Implicit(Model):X}) could not be loaded on time (timeout: {TimeOut.TotalSeconds} seconds).");
		}
	}

	public void Dispose()
	{
		if (disposed)
		{
			throw new ObjectDisposedException($"0x{Model.Hash:X}");
		}
		API.SetModelAsNoLongerNeeded(Model.op_Implicit(Model));
		disposed = true;
		GC.SuppressFinalize(this);
	}
}
