using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;

namespace Carbon.Client;

public partial class ServerNetwork : BaseNetwork
{
	public static ServerNetwork ins = new();

	public List<Connection> connections = new();
	internal List<Connection> disconnections = new();

	public Connection Get(ulong playerId)
	{
		return connections.FirstOrDefault(x => x.userid == playerId);
	}

	public async void OnClientConnected(Network.Connection connection)
	{
		try
		{
			var client = new TcpClient();
			var ip = connection.IPAddressWithoutPort();
			Console.WriteLine($"Connecting to {ip}:{Port.VALUE}");
			await client.ConnectAsync(ip, Port.VALUE);

			var conn = Connection.Create(client);
			conn.userid = connection.userid;
			conn.username = connection.username;
			connections.Add(conn);
		}
		catch (SocketException)
		{
			Console.WriteLine($"Failed to create handshake with {connection}");
		}
	}
	public void OnClientDisconnected(Network.Connection connection)
	{
		var client = Get(connection.userid);

		if (client == null)
		{
			return;
		}

		OnClientDisconnected(client);
	}
	public void OnClientDisconnected(Connection connection)
	{
		connection.Disconnect();
		connections.Remove(connection);
	}

	public override void OnNetwork()
	{
		foreach(var connection in disconnections)
		{
			OnClientDisconnected(connection);
		}

		disconnections.Clear();

		foreach (var connection in connections)
		{
			try
			{
				if (!connection.HasData)
				{
					continue;
				}
			}
			catch (ObjectDisposedException)
			{
				disconnections.Add(connection);
				continue;
			}
			catch (Exception)
			{
				disconnections.Add(connection);
				continue;
			}

			connection.read.StartRead();

			if (!connection.read.hasData)
			{
				continue;
			}

			var message = connection.read.Message();

			if (message == MessageType.UNUSED)
			{
				continue;
			}
			try
			{
				OnData(message, connection);
			}
			catch(Exception ex)
			{
				Console.WriteLine($"[ERRO] Failed processing network message packet '{message}' ({ex.Message})\n{ex.StackTrace}");
			}

			connection?.read?.EndRead();
		}
	}
}
