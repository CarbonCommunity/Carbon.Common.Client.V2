using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using UnityEngine;

namespace Carbon.Client;

public class ClientNetwork : BaseNetwork
{
	public static ClientNetwork ins = new();

	public TcpClient net = new();

	public string ip { get; private set; }
	public int port { get; private set; }

	public CarbonConnection connection;

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
			connection = CarbonConnection.Create(net, true);
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

		Console.WriteLine($"Client shutdown: {reason}");
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
		Console.WriteLine($"[ERRO] Couldn't connect ({socket})");
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

		try
		{
			if (!HasData)
			{
				return;
			}
		}
		catch (ObjectDisposedException)
		{
			Shutdown($"Timed out");
			return;
		}
		catch (Exception ex)
		{
			Shutdown($"{ex.Message}\n{ex.StackTrace}");
			return;
		}

		connection.read.StartRead();

		if (!connection.read.hasData)
		{
			return;
		}

		var message = connection.read.Message();

		if (message == MessageType.UNUSED)
		{
			return;
		}

		switch (message)
		{

			default:
				Console.WriteLine($"Unhandled MessageType received: {message}");
				break;
		}

		connection?.read?.EndRead();
	}

	#endregion
}
