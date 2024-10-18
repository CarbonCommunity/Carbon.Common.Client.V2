using Carbon.Client;
using Carbon;
using HarmonyLib;
using JetBrains.Annotations;
using Network;
using Carbon.Extensions;

[HarmonyPatch(typeof(ServerMgr), nameof(ServerMgr.ClientReady), [typeof(Message)])]
[UsedImplicitly]
public class ServerMgr_ClientReady
{
	public static void Prefix(Message packet)
	{
		if (packet.connection != null)
		{
			var client = packet.connection.ToCarbonClient();

			// IOnCarbonClientReady
			HookCaller.CallStaticHook(553692780, client);

			// OnCarbonClientReady
			HookCaller.CallStaticHook(4151784399, client as CarbonClient);
		}
	}
}
