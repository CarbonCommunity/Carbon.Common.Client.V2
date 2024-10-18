namespace Carbon.Client;

public partial class ServerNetwork
{
	public void Message_Approval(CarbonClient conn)
	{
		var passed = conn.Read.Bool();

		if (passed)
		{
			Logger.Log($"Successfully passed handshake with {conn}!");
		}
		else
		{
			Logger.Warn($"Failed handshake with {conn}.");
		}
	}
}
