using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Facepunch;

namespace Carbon.Client;

public class ServerNetwork : BaseNetwork
{
    public List<Connection> connections = new();

    public string ip { get; private set; }
    public int port { get; private set; }

    internal TcpListener net;

    public bool IsConnected => net != null;

	#region Hooks

    public virtual void OnConnect()
    {
        var split = net.LocalEndpoint.ToString().Split(':');
        ip = split[0];
        port = int.Parse(split[1]);
    }

    public virtual void OnClientConnected(Connection connection)
    {
        connections.Add(connection);
    }

    public virtual void OnClientDisconnected(Connection connection)
    {
        connection.Disconnect();
        connections.Remove(connection);
    }

    public virtual void OnShutdown()
    {

    }

    public virtual void OnData(Connection connection)
    {
    }

    public virtual void NetworkUpdate()
    {
        if (net.Pending())
        {
            OnClientConnected(Connection.Create(net.AcceptTcpClient()));
        }

        foreach (var connection in connections)
        {
            if (!connection.IsConnected || !connection.HasData)
            {
                continue;
            }

            OnData(connection);
        }
    }

    public virtual void Send(Connection connection, BaseNetworkable.SaveInfo data, MessageType msg)
    {
        // connection.write.Write(msg);
        // data.msg.Serialize(connection.write);
        // connection.write.Send();
        // Pool.Free(ref data.msg);
    }

    public virtual void Send(List<Connection> connections, BaseNetworkable.SaveInfo data, MessageType msg)
    {
        // foreach (var client in connections)
        // {
        //     client.write.Write(msg);
        //     data.msg.Serialize(client.write);
        //     client.write.Send();
        // }
		// 
        // Pool.Free(ref data.msg);
    }

	#endregion

    public void Start(string ip, int port)
    {
        if (IsConnected)
        {
            Logger.Warn($"Attempted to start the server while it's already connected.");
            return;
        }

        if (ip == "localhost")
        {
            ip = "127.0.0.1";
        }

        net = new TcpListener(IPAddress.Parse(ip), port);

        try
        {
	        net.Start();

	        OnConnect();
        }
        catch (Exception ex)
        {
	        Logger.Error($"Failed starting server", ex);
        }
    }

    public virtual void Shutdown()
    {
        if (!IsConnected)
        {
			Logger.Warn($"Attempted to shut the server down while it's offline.");
            return;
        }

        net?.Stop();
        net = null;

        Logger.Log($"Shutting down [server]");
        OnShutdown();
    }
}
