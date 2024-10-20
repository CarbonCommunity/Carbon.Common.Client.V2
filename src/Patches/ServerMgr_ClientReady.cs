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
	public static void Postfix(Message packet)
	{
		if (packet.connection != null)
		{
			var iclient = packet.connection.ToCarbonClient();
			var client = iclient as CarbonClientConnection;

			// IOnCarbonClientReady
			HookCaller.CallStaticHook(553692780, iclient);

			// OnCarbonClientReady
			HookCaller.CallStaticHook(4151784399, client);

			client.Write.Start(Messages.PlayerLoad);
			client.Write.Send();
		}
	}
}
