using System;

namespace Carbon.Client;

public partial class ClientNetwork
{
	public void Message_Approval()
	{
		serverConnection.username = serverConnection.read.String();
		serverConnection.userid = serverConnection.read.UInt64();

		Console.WriteLine($"Connected {serverConnection.username} {serverConnection.ip} {serverConnection.userid}");
	}
}
