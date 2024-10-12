using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using UnityEngine;

namespace Carbon.Client;

public class ClientNetwork : BaseNetwork
{
	public TcpClient net = new();

	public string ip { get; private set; }
	public int port { get; private set; }

	public Connection connection;

	public bool IsConnected => net != null && net.Connected;
	public bool HasData => connection != null && connection.stream != null && connection.stream.DataAvailable;

	public async ValueTask<bool> Connect(string ip, int port)
	{
		net = new();

		try
		{
			if (ip == "localhost")
			{
				ip = "127.0.0.1";
			}

			this.ip = ip;
			this.port = port;
			await net.ConnectAsync(IPAddress.Parse(ip), port);
		}
		catch (SocketException exception)
		{
			OnConnectFail(exception.SocketErrorCode);
			return false;
		}

		if (net != null && net.Connected)
		{
			connection = Connection.Create(net, true);
			OnConnect();
		}
		else
		{
			OnConnectFail(SocketError.NotConnected);
			return false;
		}

		return net.Connected;
	}

	public void Shutdown(string reason)
	{
		if (net == null)
		{
			return;
		}

		connection?.Disconnect();

		Debug.Log($"Client shutdown: {reason}");
		OnShutdown();

		connection = null;
		net = null;
	}

	#region Hooks

	public virtual void OnConnect()
	{
	}

	public virtual void OnConnectFail(SocketError socket)
	{
		Debug.LogError($"Couldn't connect ({socket})");
	}

	public virtual void OnShutdown()
	{

	}

	public virtual void NetworkUpdate()
	{
		if (net == null || connection == null)
		{
			return;
		}

	}

	#endregion
}
