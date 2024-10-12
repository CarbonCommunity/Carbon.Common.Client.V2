using UnityEngine;

namespace Carbon.Client;

internal class Server : ServerNetwork
{
	public static Server ins = new();

	#region Hooks

	public override async void OnConnect()
	{
		base.OnConnect();

	}

	public override void Shutdown()
	{

		base.Shutdown();
	}

	public override void OnClientConnected(Connection connection)
	{
		base.OnClientConnected(connection);

	}

	public override void OnClientDisconnected(Connection connection)
	{

		base.OnClientDisconnected(connection);
	}

	public override void OnData(Connection connection)
	{
		base.OnData(connection);

		var read = connection.read;

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

		switch (message)
		{


			default:
				Debug.LogError($"Unhandled MessageType received: {message}");
				break;
		}

		connection.read?.EndRead();
	}

	public override void NetworkUpdate()
	{
		base.NetworkUpdate();

	}

	#endregion
}
