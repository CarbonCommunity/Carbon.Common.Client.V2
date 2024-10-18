using System;
using Carbon.Client.Assets;
using System.Collections.Generic;
using Carbon.Client.SDK;
using Steamworks.ServerList;
using System.Linq;
using Carbon.Extensions;

namespace Carbon.Client;

public class CarbonClientManager : ICarbonClientManager
{
	public Dictionary<Network.Connection, ICarbonClient> Clients { get; } = new();

	internal const string _PATCH_NAME = "com.carbon.clientpatch";
	internal HarmonyLib.Harmony _PATCH;

	public int AddonCount => AddonManager.Instance.LoadedAddons.Count;
	public int AssetCount => AddonManager.Instance.LoadedAddons.Sum(x => x.Key.assets.Count);
	public int SpawnablePrefabsCount => AddonManager.Instance.Prefabs.Count;
	public int PrefabsCount => AddonManager.Instance.CreatedPrefabs.Count;
	public int RustPrefabsCount => AddonManager.Instance.CreatedRustPrefabs.Count;
	public int EntityCount => AddonManager.Instance.CreatedEntities.Count;

	public void Init()
	{
		Community.Runtime.Core.timer.Every(2f, () =>
		{
			foreach (var client in Clients)
			{
				if (client.Value.HasCarbonClient && client.Value.IsConnected && client.Value.IsDownloadingAddons && client.Value.Player != null)
				{
					client.Value.Player.ClientKeepConnectionAlive(default);
				}
			}
		});
	}
	public void ApplyPatch()
	{
		_PATCH?.UnpatchAll(_PATCH_NAME);
		_PATCH = new HarmonyLib.Harmony(_PATCH_NAME);

		try
		{
			_PATCH.PatchAll(typeof(CarbonClientManager).Assembly);
		}
		catch (Exception ex)
		{
			Logger.Error($"Failed patching Client Manager", ex);
		}
	}

	public void OnConnected(Network.Connection connection)
	{
		var client = Get(connection);

		if (client == null)
		{
			return;
		}

		if (!client.IsConnected)
		{
			Logger.Warn($"Client {client.Connection?.username}[{client.Connection?.userid}] is not connected to deliver ping.");
			return;
		}

		if (client.HasCarbonClient)
		{
			Logger.Warn($"Already connected with Carbon for client {client.Connection?.username}[{client.Connection?.userid}].");
			return;
		}

		// Ping
	}
	public void OnDisconnected(Network.Connection connection)
	{
		var client = Get(connection);

		if (client != null)
		{
			client.OnDisconnect();

			DisposeClient(client);
		}
	}

	public ICarbonClient Get(Network.Connection connection)
	{
		if (connection == null)
		{
			return null;
		}

		if (!Clients.TryGetValue(connection, out var client))
		{
			Clients.Add(connection, client = Make(connection));
		}

		if (client.Player == null)
		{
			client.Player = BasePlayer.FindAwakeOrSleeping(client.Connection.userid.ToString());
		}

		return client;
	}
	public ICarbonClient Get(BasePlayer player)
	{
		var client = Get(player?.Connection);
		client.Player = player;
		return client;
	}

	public bool IsCarbonClient(BasePlayer player)
	{
		var client = Get(player);

		if (client == null)
		{
			return false;
		}

		return client.HasCarbonClient;
	}
	public bool IsCarbonClient(Network.Connection connection)
	{
		var client = Get(connection);

		if (client == null)
		{
			return false;
		}

		return client.HasCarbonClient;
	}

	public void SendRequestsToAllPlayers(bool uninstallAll = true, bool loadingScreen = true)
	{
		foreach (var player in BasePlayer.activePlayerList)
		{
			SendRequestToPlayer(player.Connection, uninstallAll, loadingScreen);
		}
	}
	public void SendRequestToPlayer(Network.Connection connection, bool uninstallAll = true, bool loadingScreen = true)
	{
		if (connection == null ||
			AddonManager.Instance.LoadedAddons.Count == 0)
		{
			return;
		}

		var client = connection.ToCarbonClient() as CarbonClient;

		if (!client.HasCarbonClient || client.IsDownloadingAddons)
		{
			return;
		}

		// asd
	}

	public async void InstallAddons(string[] urls)
	{
		Logger.Warn($" C4C: Downloading {urls.Length:n0} URLs synchronously...");

		var addons = await AddonManager.Instance.LoadAddons(urls, async: false);

		AddonManager.Instance.Install(addons);

		SendRequestsToAllPlayers();
	}
	public async void InstallAddonsAsync(string[] urls)
	{
		Logger.Warn($" C4C: Downloading {urls.Length:n0} URLs asynchronously...");

		var addons = await AddonManager.Instance.LoadAddons(urls, async: true);
		Community.Runtime.Core.persistence.StartCoroutine(AddonManager.Instance.InstallAsync(addons, () =>
		{
			SendRequestsToAllPlayers();
		}));
	}
	public void UninstallAddons()
	{
		AddonManager.Instance.Uninstall();
	}

	public void DisposeClient(ICarbonClient client)
	{
		if (Clients.ContainsKey(client.Connection))
		{
			Clients.Remove(client.Connection);
			// client.Dispose();
		}
	}

	internal static ICarbonClient Make(Network.Connection connection)
	{
		if (connection == null)
		{
			return null;
		}

		return new CarbonClient
		{
			Connection = connection,
			Player = connection.player as BasePlayer
		};
	}
}
