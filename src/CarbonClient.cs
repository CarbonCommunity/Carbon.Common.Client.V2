using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using Carbon.Client.SDK;

namespace Carbon.Client;

public class CarbonClient : ICarbonConnection
{
	public BasePlayer Player { get; set; }
	public Network.Connection Connection { get; set; }
	public TcpClient Net { get; set; }

	public NetworkStream Stream { get; set; }
	public NetRead Read { get; set; }
	public NetWrite Write { get; set; }

	public bool IsCarbonConnected => Net != null && Net.Connected;
	public bool IsConnected => Connection != null && Connection.active;
	public bool HasData => Stream != null && Stream is { DataAvailable: true };

	public bool IsDownloadingAddons { get; set; }

	public bool IsValid()
	{
		return IsConnected && IsCarbonConnected;
	}

	public async void OnConnected()
	{
		// OnCarbonClientJoined
		HookCaller.CallStaticHook(2138658231, this);

		if(await ConnectCarbon())
		{
			Write.Start(Messages.Approval);
			Write.Int32(Protocol.VERSION);
			Write.UInt64(Connection.userid);
			Write.String(Connection.username);
			Write.Send();
		}
	}
	public void OnDisconnect()
	{
		IsDownloadingAddons = false;

		// OnCarbonClientLeft
		HookCaller.CallStaticHook(689036326, this);

		DisconnectCarbon("Disconnected");
	}
	public void Dispose()
	{
		IsDownloadingAddons = false;
		Player = null;
		Connection = null;
	}

	public async ValueTask<bool> ConnectCarbon()
	{
		Net = new TcpClient();

		try
		{
			var ip = Connection.IPAddressWithoutPort();
			Console.WriteLine($"Connecting {Connection} to {ip}:{Port.VALUE}");
			await Net.ConnectAsync(ip, Port.VALUE);
			Console.WriteLine($"Connected to {ip}:{Port.VALUE} successfully! {Net == null}");

			Stream = Net.GetStream();
			Read = new NetRead(this);
			Write = new NetWrite(this);

			return true;
		}
		catch (SocketException)
		{
			Console.WriteLine($"Failed to connect to C4C: {Connection}");
			Net = null;
		}

		return false;
	}
	public void DisconnectCarbon(string reason)
	{
		if (Net == null)
		{
			return;
		}

		Net.Close();
		Net.Dispose();
		Net = null;

		Console.WriteLine($"DisconnectCarbon: {reason}");
	}
}
