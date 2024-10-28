using Carbon.Client.SDK;

namespace Carbon.Client;

public partial class ServerNetwork
{
	public void Message_Approval(CarbonClientConnection conn)
	{
		var passed = conn.Read.Bool();

		if (passed)
		{
			Logger.Log($"Successfully passed handshake with {conn}!");
		}
		else
		{
			Logger.Warn($"Failed handshake with {conn}.");
		}
	}

	public void Message_AddonsLoaded(CarbonClientConnection conn)
	{
		// OnClientAddonsFinalized
		HookCaller.CallStaticHook(1317696776, conn);
	}
}
