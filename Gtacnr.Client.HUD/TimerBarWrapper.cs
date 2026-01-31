using NativeUI;

namespace Gtacnr.Client.HUD;

public struct TimerBarWrapper<T> where T : TimerBarBase
{
	public T _value;

	public T Value
	{
		get
		{
			return _value;
		}
		set
		{
			if (!object.Equals(value, _value) && _value != null)
			{
				TimerBarScript.RemoveTimerBar(_value);
			}
			_value = value;
		}
	}
}
