using System.Collections.Generic;

namespace Carbon.Client;

public struct NetworkId : IEqualityComparer<NetworkId>
{
	public ulong Value;

	public NetworkId(ulong value) => this.Value = value;

	public static bool operator ==(NetworkId id, NetworkId value)
	{
		return id.Equals(value);
	}

	public static bool operator !=(NetworkId id, NetworkId value)
	{
		return !id.Equals(value);
	}

	public static bool operator ==(NetworkId id, ulong value)
	{
		return id.Value == value;
	}

	public static bool operator !=(NetworkId id, ulong value)
	{
		return id.Value != value;
	}

	public override bool Equals(object obj)
	{
		if (obj is NetworkId id)
		{
			return Value == id.Value;
		}

		return false;
	}

	public override int GetHashCode()
	{
		return (Value).GetHashCode();
	}

	public bool Equals(NetworkId x, NetworkId y)
	{
		return x.Value == y.Value;
	}

	public int GetHashCode(NetworkId obj)
	{
		return obj.GetHashCode();
	}

	public override string ToString()
	{
		return Value.ToString();
	}
}
