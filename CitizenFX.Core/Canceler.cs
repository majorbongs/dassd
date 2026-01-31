using System;

namespace CitizenFX.Core;

public class Canceler
{
	public bool IsCanceled { get; private set; }

	public bool ThrowOnCancelation { get; set; }

	public CancelerToken Token => new CancelerToken(this);

	public event Action OnCancel;

	public void Cancel()
	{
		IsCanceled = true;
		this.OnCancel?.Invoke();
		this.OnCancel = null;
	}

	public static implicit operator CancelerToken(Canceler canceler)
	{
		return canceler.Token;
	}
}
