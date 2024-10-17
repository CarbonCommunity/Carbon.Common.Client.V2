using System;

namespace Carbon.Client;

public partial class ServerNetwork
{
	public void Message_Approval(Connection conn)
	{
		conn.username = conn.read.String();
		conn.userid = conn.read.UInt64();

		Console.WriteLine($"Connected {conn.username} {conn.ip} {conn.userid}");
	}
}
