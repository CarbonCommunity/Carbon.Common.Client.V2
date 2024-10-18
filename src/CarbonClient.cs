using System;
using System.Diagnostics;
using Carbon.Client.SDK;
using Network;
using ProtoBuf;
using UnityEngine;

/*
 *
 * Copyright (c) 2022-2024 Carbon Community
 * All rights reserved.
 *
 */

namespace Carbon.Client;

public class CarbonClient : ICarbonClient
{
	public BasePlayer Player { get; set; }
	public Network.Connection Connection { get; set; }

	public bool IsConnected => Connection != null && Connection.active;
	public bool HasCarbonClient { get; set; }

	public bool IsDownloadingAddons { get; set; }

	public bool IsValid()
	{
		return IsConnected && HasCarbonClient;
	}

	public void OnConnected()
	{
		// OnCarbonClientJoined
		HookCaller.CallStaticHook(2138658231, this);
	}
	public void OnDisconnect()
	{
		IsDownloadingAddons = false;

		// OnCarbonClientLeft
		HookCaller.CallStaticHook(689036326, this);
	}
	public void Dispose()
	{
		IsDownloadingAddons = false;
		Player = null;
		Connection = null;
	}
}
