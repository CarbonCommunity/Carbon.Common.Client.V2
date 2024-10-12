using Network;
using System.Net.Sockets;
using UnityEngine;

namespace Carbon.Client;

public class Connection
{
	public TcpClient net;
	public BasePlayer player;

	[Header("User")]
	public string username;
	public string ip;

	[Header("Local")]
	public bool isLocal;

	public NetworkStream stream;
	public NetRead read;
	public NetWrite write;

	internal float connectionStart;

	public bool IsConnected => net != null && net.Connected;
	public bool HasData => stream != null && stream is { DataAvailable: true };

	public float ConnectionTime => Time.realtimeSinceStartup - connectionStart;

	public void Disconnect()
	{
		if (net == null)
		{
			return;
		}

		net?.Close();
		net?.Dispose();
		net = null;
	}

	public static Connection Create(TcpClient client, bool isLocal = false)
	{
		var nc = new Connection();
		nc.net = client;
		nc.username = "Monaco";
		nc.ip = client.Client.RemoteEndPoint.ToString();
		nc.stream = client.GetStream();
		nc.read = new NetRead(nc);
		nc.write = new NetWrite(nc);
		nc.isLocal = isLocal;
		nc.connectionStart = Time.realtimeSinceStartup;
		return nc;
	}

	public override string ToString() => $"{username}[{ip}]";
}
