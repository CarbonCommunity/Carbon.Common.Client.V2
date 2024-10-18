using System.Net.Sockets;

namespace Carbon.Client;

public class CarbonServer
{
	public TcpClient Net { get; set; }
	public NetworkStream Stream { get; set; }
	public NetRead Read { get; set; }
	public NetWrite Write { get; set; }

	public bool IsCarbonConnected => Net != null && Net.Connected;
	public bool HasData => Stream != null && Stream is { DataAvailable: true };
}
