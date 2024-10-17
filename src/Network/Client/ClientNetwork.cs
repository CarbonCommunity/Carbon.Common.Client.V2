using System;
using System.Net;
using System.Net.Sockets;

namespace Carbon.Client;

public partial class ClientNetwork : BaseNetwork
{
	public static ClientNetwork ins = new();

	public TcpListener net;

	public Connection serverConnection;

	public bool IsConnected => net != null;

	public void Start()
	{
		if (IsConnected)
		{
			Console.WriteLine($"Attempted to start the server while it's already connected.");
			return;
		}

		net = new(IPAddress.Parse("127.0.0.1"), Carbon.Client.Port.VALUE);

		try
		{
			net.Start();
		}
		catch (Exception ex)
		{
			Console.WriteLine($"Failed starting server", ex);
		}
	}

	public override void OnNetwork()
	{
		if (net != null && net.Pending())
		{
			serverConnection = Connection.Create(net.AcceptTcpClient());
		}

		if(serverConnection == null || !serverConnection.IsConnected || !serverConnection.HasData)
		{
			return;
		}

		var read = serverConnection.read;

		read.StartRead();

		if (!read.hasData)
		{
			return;
		}

		var message = read.Message();

		if (message == MessageType.UNUSED)
		{
			return;
		}

		try
		{
			OnData(message, serverConnection);
		}
		catch(Exception ex)
		{
			Console.WriteLine($"[ERRO] Failed processing network message packet '{message}' ({ex.Message})\n{ex.StackTrace}");
		}

		serverConnection.read?.EndRead();
	}
}
