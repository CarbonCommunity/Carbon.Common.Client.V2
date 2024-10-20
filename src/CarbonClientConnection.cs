using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using Carbon.Client.SDK;

namespace Carbon.Client;

public class CarbonClientConnection : ICarbonConnection
{
	public ulong UserId { get; set; }
	public string Username { get; set; }
	public string Ip { get; set; }
	public ulong Connection { get; set; }

	public BasePlayer Player { get; set; }
	public TcpClient Net { get; set; }

	public NetworkStream Stream { get; set; }
	public NetRead Read { get; set; }
	public NetWrite Write { get; set; }

	public bool IsCarbonConnected => Net != null && Net.Connected;
	public bool HasData => Stream != null && Stream is { DataAvailable: true };

	public bool IsDownloadingAddons { get; set; }

	public bool IsValid()
	{
		return IsCarbonConnected;
	}

	public async void OnConnected(ulong userid, string username, string ip)
	{
		// OnCarbonClientJoined
		HookCaller.CallStaticHook(2138658231, this);

		if(await ConnectCarbon())
		{
			Write.Start(Messages.Approval);
			Write.Int32(Protocol.VERSION);
			Write.UInt64(userid);
			Write.String(username);
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
	}

	public async ValueTask<bool> ConnectCarbon()
	{
		Net = new TcpClient();

		try
		{
			Console.WriteLine($"Connecting {Username}[{UserId}] to {Ip}:{Port.VALUE}");
			await Net.ConnectAsync(Ip, Port.VALUE);
			Console.WriteLine($"Connected to {Ip}:{Port.VALUE} successfully! {Net == null}");

			Stream = Net.GetStream();
			Read = new NetRead(this);
			Write = new NetWrite(this);

			return true;
		}
		catch (SocketException)
		{
			Console.WriteLine($"Failed to connect to C4C: {Username}[{UserId}] ");
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
