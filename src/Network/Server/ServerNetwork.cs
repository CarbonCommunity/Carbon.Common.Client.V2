using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Carbon.Client;

public partial class ServerNetwork : BaseNetwork
{
	public static ServerNetwork ins = new();

	public override void OnNetwork()
	{
		foreach (var client in CarbonClientManager.ins.Clients)
		{
			var conn = client.Value;

			try
			{
				if (!conn.HasData)
				{
					continue;
				}
			}
			catch (ObjectDisposedException)
			{
				conn.DisconnectCarbon("Obj disposed");
				continue;
			}
			catch (Exception)
			{
				continue;
			}

			conn.Read.StartRead();

			if (!conn.Read.hasData)
			{
				continue;
			}

			var message = conn.Read.Message();

			if (message == Messages.UNUSED)
			{
				continue;
			}
			try
			{
				OnData(message, conn);
			}
			catch(Exception ex)
			{
				Console.WriteLine($"[ERRO] Failed processing network message packet '{message}' ({ex.Message})\n{ex.StackTrace}");
			}

			conn.Read.EndRead();
		}
	}
	public virtual void OnData(Messages msg, CarbonClient conn)
	{
		switch (msg)
		{
			case Messages.Approval:
				Message_Approval(conn);
				break;

			default:
				Console.WriteLine($"[ERRO] Unhandled MessageType received: {msg}");
				break;
		}
	}
}
