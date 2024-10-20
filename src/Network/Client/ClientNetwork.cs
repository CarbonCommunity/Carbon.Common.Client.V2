using System;
using System.Net;
using System.Net.Sockets;

namespace Carbon.Client;

public partial class ClientNetwork : BaseNetwork
{
	public static ClientNetwork ins = new();

	public TcpListener net;

	public CarbonServerConnection serverConnection;

	public bool IsConnected => net != null && net.Server != null && net.Server.Connected;

	public void Start()
	{
		if (IsConnected)
		{
			Console.WriteLine($"Attempted to start the server while it's already connected.");
			return;
		}

		net = new(IPAddress.Parse("127.0.0.1"), Port.VALUE);

		try
		{
			net.Start();
			Console.WriteLine($"Started C4C connection @ 127.0.0.1:{Port.VALUE}.. Ready for C4C server!");
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
			var conn = net.AcceptTcpClient();
			var stream = conn.GetStream();
			serverConnection = new()
			{
				Net = conn,
				Stream = stream,
				Write = new(stream),
				Read = new(stream)
			};
		}

		if(serverConnection == null || !serverConnection.IsCarbonConnected || !serverConnection.HasData)
		{
			return;
		}

		var read = serverConnection.Read;

		read.StartRead();

		if (!read.hasData)
		{
			return;
		}

		var message = read.Message();

		if (message == Messages.UNUSED)
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

		serverConnection.Read?.EndRead();
	}

	public virtual void OnData(Messages msg, CarbonServerConnection conn)
	{
		Console.WriteLine($"[ERRO] Unhandled MessageType received: {msg}");
	}
}
