using Epic.OnlineServices.Platform;
using Org.BouncyCastle.Crypto.Operators;
using System.IO;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System;
using UnityEngine;

namespace Carbon.Client;

public enum MessageType
{
	LAST = -1,
	UNUSED = 0,

	Rpc,

	EntityUpdate_Full,
	EntityUpdate_Position
}

public class GenericsEx
{
	public static Target Cast<Source, Target>(Source obj)
	{
		CastImpl<Source, Target>.Value = obj;
		return CastImpl<Target, Source>.Value;
	}

	public static void Swap<T>(ref T a, ref T b)
	{
		(a, b) = (b, a);
	}

	public static class CastImpl<Source, Target>
	{
		[ThreadStatic] public static Source Value;

		static CastImpl()
		{
			if (typeof(Source) != typeof(Target))
				throw new InvalidCastException();
		}
	}
}

public class NetWrite : BinaryWriter
{
	public NetWrite() { }
	public NetWrite(Stream stream) : base(stream) { }
	public NetWrite(CarbonConnection conn) : base(conn.stream)
	{
		this.conn = conn;
	}

	public static MemoryStream stringBuffer = new();

	public CarbonConnection conn;
	public byte[] data;

	public int length
	{
		get => _length;
		set => _length = value;
	}
	public MessageType type;

	private int _position;
	private int _length;

	public void EnsureCapacity(int spaceRequired)
	{
		if (data == null)
		{
			var num = spaceRequired <= 2048 ? 2048 : spaceRequired;
			var minSize = Mathf.NextPowerOfTwo(num);
			data = minSize <= 4194304
				? BaseNetwork.BufferPool.Rent(minSize)
				: throw new Exception(
					string.Format("Preventing NetWrite buffer from growing too large (requiredLength={0})", num));
		}
		else
		{
			if (data.Length - _position >= spaceRequired)
				return;

			var val1 = _position + spaceRequired;
			var minSize = Mathf.NextPowerOfTwo(Math.Max(val1, data.Length));
			var dst = minSize <= 4194304
				? BaseNetwork.BufferPool.Rent(minSize)
				: throw new Exception(
					string.Format("Preventing NetWrite buffer from growing too large (requiredLength={0})", val1));
			Buffer.BlockCopy(data, 0, dst, 0, _length);
			BaseNetwork.BufferPool.Return(data);
			data = dst;
		}
	}

	public void Message(MessageType msg)
	{
		Write((int)msg);
	}
	public void NetworkId(NetworkId id)
	{
		Write(id.Value);
	}
	public void UInt8(byte val) => WriteUnmanaged(in val);
	public void UInt16(ushort val) => WriteUnmanaged(in val);
	public void UInt32(uint val) => WriteUnmanaged(in val);
	public void UInt64(ulong val) => WriteUnmanaged(in val);
	public void Int8(sbyte val) => WriteUnmanaged(in val);
	public void Int16(short val) => WriteUnmanaged(in val);
	public void Int32(int val) => WriteUnmanaged(in val);
	public void Int64(long val) => WriteUnmanaged(in val);
	public void Bool(bool val) => WriteUnmanaged(val ? (byte)1 : (byte)0);
	public void Float(float val) => WriteUnmanaged(in val);
	public void Double(double val) => WriteUnmanaged(in val);
	public void Vector3(in Vector3 obj)
	{
		Float(obj.x);
		Float(obj.y);
		Float(obj.z);
	}
	public void Vector4(in Vector4 obj)
	{
		Float(obj.x);
		Float(obj.y);
		Float(obj.z);
		Float(obj.w);
	}
	public void Quaternion(in Quaternion obj)
	{
		Vector3(obj.eulerAngles);
	}
	public void Ray(in Ray obj)
	{
		Vector3(obj.origin);
		Vector3(obj.direction);
	}
	public void Color(in Color obj)
	{
		Float(obj.r);
		Float(obj.g);
		Float(obj.b);
		Float(obj.a);
	}
	public void Color32(in Color32 obj)
	{
		UInt8(obj.r);
		UInt8(obj.g);
		UInt8(obj.b);
		UInt8(obj.a);
	}
	public void String(string val)
	{
		if (string.IsNullOrEmpty(val))
		{
			Stream(null);
		}
		else
		{
			if (stringBuffer.Capacity < val.Length * 8)
				stringBuffer.Capacity = val.Length * 8;
			stringBuffer.Position = 0L;
			stringBuffer.SetLength(stringBuffer.Capacity);
			var bytes = Encoding.UTF8.GetBytes(val, 0, val.Length, stringBuffer.GetBuffer(), 0);
			stringBuffer.SetLength(bytes);
			Stream(stringBuffer);
		}
	}
	public void Bytes(byte[] buffer)
	{
		Bytes(buffer, buffer.Length);
	}
	public void Bytes(byte[] buffer, int length)
	{
		if (buffer == null || buffer.Length == 0 || length == 0)
			WriteUnmanaged(0U);

		else if ((uint)length > 10485760U)
		{
			WriteUnmanaged(0U);
			Console.WriteLine("[ERRO] BytesWithSize: Too big " + length);
		}
		else
		{
			WriteUnmanaged((uint)length);
			Bytes(buffer, 0, length);
		}
	}
	public void Bytes(byte[] buffer, int offset, int count)
	{
		EnsureCapacity(count);
		Buffer.BlockCopy(buffer, offset, data, _position, count);

		_position += count;

		if (_position <= _length)
			return;
		_length = _position;
	}
	public void Stream(MemoryStream val)
	{
		if (val == null || val.Length == 0L)
			WriteUnmanaged(0U);
		else
			Bytes(val.GetBuffer(), (int)val.Length);
	}

	public void Init(byte[] data, int length)
	{
		this.data = data;
		this.length = length;
	}

	public void Start(MessageType msg)
	{
		Clear();
		Message(type = msg);
	}
	public void Write<T>(in T val)
	{
		if (typeof(T) == typeof(Vector3))
			Vector3(GenericsEx.Cast<T, Vector3>(val));
		else if (typeof(T) == typeof(Vector4))
			Vector4(GenericsEx.Cast<T, Vector4>(val));
		else if (typeof(T) == typeof(Quaternion))
			Quaternion(GenericsEx.Cast<T, Quaternion>(val));
		else if (typeof(T) == typeof(Ray))
			Ray(GenericsEx.Cast<T, Ray>(val));
		else if (typeof(T) == typeof(float))
			Float(GenericsEx.Cast<T, float>(val));
		else if (typeof(T) == typeof(short))
			Int16(GenericsEx.Cast<T, short>(val));
		else if (typeof(T) == typeof(ushort))
			UInt16(GenericsEx.Cast<T, ushort>(val));
		else if (typeof(T) == typeof(int))
			Int32(GenericsEx.Cast<T, int>(val));
		else if (typeof(T) == typeof(uint))
			UInt32(GenericsEx.Cast<T, uint>(val));
		else if (typeof(T) == typeof(byte[]))
			Bytes(GenericsEx.Cast<T, byte[]>(val));
		else if (typeof(T) == typeof(long))
			Int64(GenericsEx.Cast<T, long>(val));
		else if (typeof(T) == typeof(ulong))
			UInt64(GenericsEx.Cast<T, ulong>(val));
		else if (typeof(T) == typeof(string))
			String(GenericsEx.Cast<T, string>(val));
		else if (typeof(T) == typeof(sbyte))
			Int8(GenericsEx.Cast<T, sbyte>(val));
		else if (typeof(T) == typeof(byte))
			UInt8(GenericsEx.Cast<T, byte>(val));
		else if (typeof(T) == typeof(bool))
			Bool(GenericsEx.Cast<T, bool>(val));
		else if (typeof(T) == typeof(Color))
			Color(GenericsEx.Cast<T, Color>(val));
		else if (typeof(T) == typeof(Color32))
			Color32(GenericsEx.Cast<T, Color32>(val));
		else if (typeof(T) == typeof(MessageType))
			Message(GenericsEx.Cast<T, MessageType>(val));
		else if (typeof(T) == typeof(NetworkId))
			NetworkId(GenericsEx.Cast<T, NetworkId>(val));
		else
			Console.WriteLine($"[ERRO] NetworkData.Write - no handler to write {val} -> {val.GetType()}");
	}
	public void End() => Int32((int)MessageType.LAST);
	public void Send(bool clear = true)
	{
		if (!conn.IsConnected)
		{
			conn.Disconnect();

			if (clear)
			{
				Clear();
			}
			return;
		}

		try
		{
			base.Write(length);
			base.Write(data, 0, length);
		}
		catch (IOException)
		{
			conn.Disconnect();
		}
		catch (SocketException)
		{
			conn.Disconnect();
		}
		finally
		{
			if (clear)
			{
				Clear();
			}
		}
	}
	public void Clear()
	{
		if (data == null)
			return;

		BaseNetwork.BufferPool.Return(data);
		data = null;

		_position = 0;
		_length = 0;
	}

	#region Helpers

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private unsafe void WriteUnsafe<T>(byte[] buffer, in T value, int iOffset = 0) where T : unmanaged
	{
		fixed (byte* numPtr = buffer)
			*(T*)(numPtr + iOffset) = value;
	}

	private unsafe void WriteUnmanaged<T>(in T val) where T : unmanaged
	{
		var spaceRequired = sizeof(T);

		EnsureCapacity(spaceRequired);
		WriteUnsafe(data, in val, _position);

		_position += spaceRequired;

		if (_position <= _length)
			return;

		_length = _position;
	}

	#endregion
}

public class NetRead : BinaryReader
{
	public static char[] charBuffer = new char[8388608];
	public static byte[] byteBuffer = new byte[8388608];

	public NetRead(Stream stream) : base(stream) { }
	public NetRead(CarbonConnection conn) : base(conn.stream)
	{
		this.conn = conn;
	}

	public CarbonConnection conn;
	public MessageType Type;
	public byte[] data = new byte[8388608];
	public int length
	{
		get => _length;
		set => _length = value;
	}
	public int position
	{
		get => _position;
		set => _position = value;
	}
	public bool hasData => length > 0;
	public bool readAll => position >= length - 1;
	public int unread => _length - _position;

	private int _position;
	private int _length;

	public MessageType Message() => Type = (MessageType)Int32();
	public NetworkId NetworkId() => new(UInt64());
	public byte UInt8() => ReadUnmanaged<byte>();
	public ushort UInt16() => ReadUnmanaged<ushort>();
	public uint UInt32() => ReadUnmanaged<uint>();
	public ulong UInt64() => ReadUnmanaged<ulong>();
	public sbyte Int8() => ReadUnmanaged<sbyte>();
	public short Int16() => ReadUnmanaged<short>();
	public int Int32() => ReadUnmanaged<int>();
	public long Int64() => ReadUnmanaged<long>();
	public bool Bool() => ReadUnmanaged<bool>();
	public float Float() => ReadUnmanaged<float>();
	public double Double() => ReadUnmanaged<double>();
	public uint Byte() => ReadUnmanaged<byte>();
	public Vector3 Vector3() => new(Float(), Float(), Float());
	public Vector4 Vector4() => new(Float(), Float(), Float(), Float());
	public Quaternion Quaternion() => new(Float(), Float(), Float(), Float());
	public Ray Ray() => new(Vector3(), Vector3());
	public Color Color() => new(Float(), Float(), Float(), Float());
	public Color32 Color32() => new(UInt8(), UInt8(), UInt8(), UInt8());
	public string String(int maxLength = 256)
	{
		return StringInternal(maxLength, false);
	}
	public string StringMultiline(int maxLength = 2048)
	{
		return StringInternal(maxLength, true);
	}
	public unsafe int Bytes(byte[] buffer, int offset, int count)
	{
		if (_position + count > _length)
			count = _length - _position;
		fixed (byte* numPtr1 = data)
		fixed (byte* numPtr2 = buffer)
			Buffer.MemoryCopy(numPtr1 + _position, numPtr2 + offset, count, count);
		_position += count;
		return count;
	}
	public int Bytes(byte[] buffer, uint maxLength = 4294967295)
	{
		var count = ReadUnmanaged<uint>();
		if (count == 0U)
			return 0;
		return count > buffer.Length || count > maxLength || Bytes(buffer, 0, (int)count) != count ? -1 : (int)count;
	}
	public byte[] Bytes(uint maxSize = 10485760)
	{
		var count = ReadUnmanaged<uint>();

		if (count == 0U)
			return null;

		if (count > maxSize)
			return null;

		var buffer = new byte[(int)count];
		return Bytes(buffer, 0, (int)count) != count ? null : buffer;
	}

	public void StartRead()
	{
		_position = 0;
		_length = ReadInt32();
		base.Read(data, 0, _length);
	}
	public void EndRead()
	{
		_position = 0;
		_length = 0;
	}

	#region Helpers

	private unsafe T ReadUnmanaged<T>() where T : unmanaged
	{
		if (unread < sizeof(T))
			return default;

		var obj = ReadUnsafe<T>(data, _position);
		_position += sizeof(T);
		return obj;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static unsafe T ReadUnsafe<T>(byte[] buffer, int iOffset = 0) where T : unmanaged
	{
		fixed (byte* numPtr = buffer)
			return *(T*)(numPtr + iOffset);
	}

	private string StringInternal(int maxLength, bool allowNewLine)
	{
		var byteCount = Bytes(byteBuffer, 8388608U);

		if (byteCount <= 0)
			return string.Empty;

		var length = Encoding.UTF8.GetChars(byteBuffer, 0, byteCount, charBuffer, 0);

		if (length > maxLength)
			length = maxLength;

		for (var index = 0; index < length; ++index)
		{
			var c = charBuffer[index];
			if (char.IsControl(c) && (!allowNewLine || c != '\n'))
				charBuffer[index] = ' ';
		}
		return new string(charBuffer, 0, length);
	}

	#endregion
}
