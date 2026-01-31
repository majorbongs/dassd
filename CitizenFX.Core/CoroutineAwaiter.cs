using System;
using System.Runtime.CompilerServices;

namespace CitizenFX.Core;

public readonly struct CoroutineAwaiter<T> : ICriticalNotifyCompletion, INotifyCompletion, ICoroutineAwaiter
{
	private readonly Coroutine<T> m_coroutine;

	public bool IsCompleted => m_coroutine.IsCompleted;

	internal CoroutineAwaiter(Coroutine<T> coroutine)
	{
		m_coroutine = coroutine;
	}

	public T GetResult()
	{
		return m_coroutine.GetResult();
	}

	public void SetResult(T result)
	{
		m_coroutine.Complete(result);
	}

	public void SetException(Exception exception)
	{
		m_coroutine.SetException(exception);
	}

	public void OnCompleted(Action continuation)
	{
		m_coroutine.ContinueWith(continuation);
	}

	public void UnsafeOnCompleted(Action continuation)
	{
		m_coroutine.ContinueWith(continuation);
	}
}
public readonly struct CoroutineAwaiter : ICriticalNotifyCompletion, INotifyCompletion, ICoroutineAwaiter
{
	private readonly Coroutine m_coroutine;

	public bool IsCompleted => m_coroutine.IsCompleted;

	internal CoroutineAwaiter(Coroutine coroutine)
	{
		m_coroutine = coroutine;
	}

	public void GetResult()
	{
		m_coroutine.GetResult();
	}

	public void SetResult()
	{
		m_coroutine.Complete();
	}

	public void SetException(Exception exception)
	{
		m_coroutine.SetException(exception);
	}

	public void OnCompleted(Action continuation)
	{
		m_coroutine.ContinueWith(continuation);
	}

	public void UnsafeOnCompleted(Action continuation)
	{
		m_coroutine.ContinueWith(continuation);
	}
}
