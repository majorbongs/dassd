using System;
using System.Drawing;
using CitizenFX.Core;

namespace NativeUI;

public static class MiscExtensions
{
	public static Random SharedRandom = new Random();

	public static Point AddPoints(this Point left, Point right)
	{
		return new Point(left.X + right.X, left.Y + right.Y);
	}

	public static Point SubtractPoints(this Point left, Point right)
	{
		return new Point(left.X - right.X, left.Y - right.Y);
	}

	public static PointF AddPoints(this PointF left, PointF right)
	{
		return new PointF(left.X + right.X, left.Y + right.Y);
	}

	public static PointF SubtractPoints(this PointF left, PointF right)
	{
		return new PointF(left.X - right.X, left.Y - right.Y);
	}

	public static float Clamp(this float val, float min, float max)
	{
		if (val > max)
		{
			return max;
		}
		if (val < min)
		{
			return min;
		}
		return val;
	}

	public static Vector3 LinearVectorLerp(Vector3 start, Vector3 end, int currentTime, int duration)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_000a: Unknown result type (might be due to invalid IL or missing references)
		//IL_0010: Unknown result type (might be due to invalid IL or missing references)
		//IL_0024: Unknown result type (might be due to invalid IL or missing references)
		//IL_002a: Unknown result type (might be due to invalid IL or missing references)
		//IL_003e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_0056: Unknown result type (might be due to invalid IL or missing references)
		return new Vector3
		{
			X = LinearFloatLerp(start.X, end.X, currentTime, duration),
			Y = LinearFloatLerp(start.Y, end.Y, currentTime, duration),
			Z = LinearFloatLerp(start.Z, end.Z, currentTime, duration)
		};
	}

	public static Vector3 VectorLerp(Vector3 start, Vector3 end, int currentTime, int duration, Func<float, float, int, int, float> easingFunc)
	{
		//IL_0002: Unknown result type (might be due to invalid IL or missing references)
		//IL_000c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0012: Unknown result type (might be due to invalid IL or missing references)
		//IL_0028: Unknown result type (might be due to invalid IL or missing references)
		//IL_002e: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		//IL_004a: Unknown result type (might be due to invalid IL or missing references)
		//IL_005c: Unknown result type (might be due to invalid IL or missing references)
		return new Vector3
		{
			X = easingFunc(start.X, end.X, currentTime, duration),
			Y = easingFunc(start.Y, end.Y, currentTime, duration),
			Z = easingFunc(start.Z, end.Z, currentTime, duration)
		};
	}

	public static float LinearFloatLerp(float start, float end, int currentTime, int duration)
	{
		return (end - start) * (float)currentTime / (float)duration + start;
	}

	public static float QuadraticEasingLerp(float start, float end, int currentTime, int duration)
	{
		float num = currentTime;
		float num2 = duration;
		float num3 = end - start;
		num /= num2 / 2f;
		if (num < 1f)
		{
			return num3 / 2f * num * num + start;
		}
		num -= 1f;
		return (0f - num3) / 2f * (num * (num - 2f) - 1f) + start;
	}
}
