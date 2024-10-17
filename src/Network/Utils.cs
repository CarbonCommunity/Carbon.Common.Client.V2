using System;

namespace Carbon.Client;

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
