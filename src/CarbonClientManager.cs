using System;
using Carbon.Client.Assets;
using System.Collections.Generic;
using Carbon.Client.SDK;
using Steamworks.ServerList;
using System.Linq;
using Carbon.Extensions;
using System.Net;

namespace Carbon.Client;

public class CarbonClientManager : ICarbonClientManager
{
	public static CarbonClientManager ins;

	public Dictionary<Network.Connection, CarbonClient> Clients { get; } = [];

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
		ins = this;

		Community.Runtime.Core.timer.Every(2f, () =>
		{
			foreach (var client in Clients)
			{
				if (client.Value.IsCarbonConnected && client.Value.IsConnected && client.Value.IsDownloadingAddons && client.Value.Player != null)
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
			Logger.Warn($"Client {client.Connection?.username}[{client.Connection?.userid}] is not connected.");
			return;
		}

		if (client.IsCarbonConnected)
		{
			Logger.Warn($"Already connected with Carbon for client {client.Connection?.username}[{client.Connection?.userid}].");
			return;
		}

		client.OnConnected();
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

	public ICarbonConnection Get(Network.Connection connection)
	{
		if (connection == null)
		{
			return null;
		}

		if (!Clients.TryGetValue(connection, out var client))
		{
			Clients.Add(connection, client = Make(connection) as CarbonClient);
		}

		if (client.Player == null)
		{
			client.Player = BasePlayer.FindAwakeOrSleeping(client.Connection.userid.ToString());
		}

		return client;
	}
	public ICarbonConnection Get(BasePlayer player)
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

		return client.IsCarbonConnected;
	}
	public bool IsCarbonClient(Network.Connection connection)
	{
		var client = Get(connection);

		if (client == null)
		{
			return false;
		}

		return client.IsCarbonConnected;
	}

	public void SendRequestsToAllPlayers(bool uninstallAll = true)
	{
		foreach (var player in BasePlayer.activePlayerList)
		{
			SendRequestToPlayer(player.Connection, uninstallAll);
		}
	}
	public void SendRequestToPlayer(Network.Connection connection, bool uninstallAll = true)
	{
		if (connection == null || AddonManager.Instance.LoadedAddons.Count == 0)
		{
			return;
		}

		var client = connection.ToCarbonClient() as CarbonClient;

		if (!client.IsCarbonConnected || client.IsDownloadingAddons)
		{
			return;
		}

		client.Write.Start(Messages.AddonLoad);
		client.Write.Bool(uninstallAll);
		client.Write.Int32(AddonManager.Instance.LoadedAddons.Count);
		foreach (var addon in AddonManager.Instance.LoadedAddons)
		{
			var manifest = addon.Key.GetManifest();
			manifest.Save(client.Write);
		}
		client.Write.Send();
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

	public void DisposeClient(ICarbonConnection client)
	{
		if (Clients.ContainsKey(client.Connection))
		{
			Clients.Remove(client.Connection);
			// client.Dispose();
		}
	}

	internal static ICarbonConnection Make(Network.Connection connection)
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
