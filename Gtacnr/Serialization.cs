using System;
using CitizenFX.Core;

namespace Gtacnr;

public static class Serialization
{
	public static byte[] ToByteArray(this Vector2 vector)
	{
		//IL_0007: Unknown result type (might be due to invalid IL or missing references)
		//IL_001b: Unknown result type (might be due to invalid IL or missing references)
		byte[] array = new byte[8];
		Array.Copy(BitConverter.GetBytes(vector.X), 0, array, 0, 4);
		Array.Copy(BitConverter.GetBytes(vector.Y), 0, array, 4, 4);
		return array;
	}

	public static string ToHexString(this Vector2 vector)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		return BitConverter.ToString(vector.ToByteArray()).Replace("-", string.Empty);
	}

	public static Vector2 ToVector2(this byte[] array)
	{
		//IL_0037: Unknown result type (might be due to invalid IL or missing references)
		if (array.Length != 8)
		{
			throw new ArgumentException("Invalid array size.", "array");
		}
		float[] array2 = new float[2]
		{
			BitConverter.ToSingle(array, 0),
			BitConverter.ToSingle(array, 4)
		};
		return new Vector2(array2[0], array2[1]);
	}

	public static byte[] ToByteArray(this Vector3 vector)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		byte[] array = new byte[12];
		Array.Copy(BitConverter.GetBytes(vector.X), 0, array, 0, 4);
		Array.Copy(BitConverter.GetBytes(vector.Y), 0, array, 4, 4);
		Array.Copy(BitConverter.GetBytes(vector.Z), 0, array, 8, 4);
		return array;
	}

	public static string ToHexString(this Vector3 vector)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		return BitConverter.ToString(vector.ToByteArray()).Replace("-", string.Empty);
	}

	public static Vector3 ToVector3(this byte[] array)
	{
		//IL_0045: Unknown result type (might be due to invalid IL or missing references)
		if (array.Length != 12)
		{
			throw new ArgumentException("Invalid array size.", "array");
		}
		float[] array2 = new float[3]
		{
			BitConverter.ToSingle(array, 0),
			BitConverter.ToSingle(array, 4),
			BitConverter.ToSingle(array, 8)
		};
		return new Vector3(array2[0], array2[1], array2[2]);
	}

	public static byte[] ToByteArray(this Vector4 vector)
	{
		//IL_0008: Unknown result type (might be due to invalid IL or missing references)
		//IL_001c: Unknown result type (might be due to invalid IL or missing references)
		//IL_0030: Unknown result type (might be due to invalid IL or missing references)
		//IL_0044: Unknown result type (might be due to invalid IL or missing references)
		byte[] array = new byte[16];
		Array.Copy(BitConverter.GetBytes(vector.X), 0, array, 0, 4);
		Array.Copy(BitConverter.GetBytes(vector.Y), 0, array, 4, 4);
		Array.Copy(BitConverter.GetBytes(vector.Z), 0, array, 8, 4);
		Array.Copy(BitConverter.GetBytes(vector.W), 0, array, 12, 4);
		return array;
	}

	public static string ToHexString(this Vector4 vector)
	{
		//IL_0000: Unknown result type (might be due to invalid IL or missing references)
		return BitConverter.ToString(vector.ToByteArray()).Replace("-", string.Empty);
	}

	public static Vector4 ToVector4(this byte[] array)
	{
		//IL_0053: Unknown result type (might be due to invalid IL or missing references)
		if (array.Length != 16)
		{
			throw new ArgumentException("Invalid array size.", "array");
		}
		float[] array2 = new float[4]
		{
			BitConverter.ToSingle(array, 0),
			BitConverter.ToSingle(array, 4),
			BitConverter.ToSingle(array, 8),
			BitConverter.ToSingle(array, 12)
		};
		return new Vector4(array2[0], array2[1], array2[2], array2[3]);
	}
}
