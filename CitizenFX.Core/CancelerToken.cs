using System;

namespace CitizenFX.Core;

public readonly struct CancelerToken
{
	private readonly Canceler m_canceler;

	public bool IsCanceled => m_canceler?.IsCanceled ?? false;

	public bool ThrowOnCancelation => m_canceler?.ThrowOnCancelation ?? false;

	public event Action OnCancel
	{
		add
		{
			if (m_canceler != null)
			{
				m_canceler.OnCancel += value;
			}
		}
		remove
		{
			if (m_canceler != null)
			{
				m_canceler.OnCancel -= value;
			}
		}
	}

	public CancelerToken(Canceler canceler)
	{
		m_canceler = canceler;
	}

	internal bool CancelOrThrowIfRequested()
	{
		Canceler canceler = m_canceler;
		if (canceler != null && canceler.IsCanceled)
		{
			if (m_canceler.ThrowOnCancelation)
			{
				throw new CoroutineCanceledException("Coroutine execution was canceled by a Canceler");
			}
			return true;
		}
		return false;
	}

	public void AddOnCancelAction(Action action)
	{
		m_canceler.OnCancel += action;
	}
}
