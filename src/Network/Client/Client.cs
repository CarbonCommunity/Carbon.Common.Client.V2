using System;
using UnityEngine;

namespace Carbon.Client;

public class Client : ClientNetwork
{
	public static Client ins = new();

	public override void OnShutdown()
	{

		base.OnShutdown();
	}

	public override void NetworkUpdate()
	{
		base.NetworkUpdate();

		try
		{
			if (!HasData)
			{
				return;
			}
		}
		catch(ObjectDisposedException ex)
		{
			Shutdown($"Timed out");
			return;
		}
		catch(Exception ex)
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
				Debug.LogError($"Unhandled MessageType received: {message}");
				break;
		}

		connection?.read?.EndRead();
	}
}
