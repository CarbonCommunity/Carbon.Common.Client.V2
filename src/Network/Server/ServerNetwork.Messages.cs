namespace Carbon.Client;

public partial class ServerNetwork
{
	public void Message_Approval(NetRead read)
	{
		read.conn.username = read.String();
		read.conn.userid = read.UInt64();

		Logger.Log($"Connected {read.conn.username} {read.conn.ip} {read.conn.userid}");
	}
}
