using System.Collections.Generic;
using UnityEngine;

namespace Carbon.Client;

public class CarbonGameManager : MonoBehaviour
{
	public static bool isServer;
	public static Dictionary<uint, SpawnablePrefab> spawnablePrefabs = [];

	public static void Initialize(bool isServer)
	{
		CarbonGameManager.isServer = isServer;
	}

	public static T CreateSpawnable<T>(uint assetId, Vector3 pos = default, Quaternion rot = default) where T : BaseCarbonEntity
	{
		if (spawnablePrefabs.TryGetValue(assetId, out var prefab))
		{
			var instance = Object.Instantiate(prefab.gameObject, pos, rot).GetComponent<T>();
			instance.Init(isServer);
			return instance;
		}

		return null;
	}

	public void Update()
	{
		if (ServerNetwork.ins != null)
		{
			ServerNetwork.ins.NetworkUpdate();
		}

		if (ClientNetwork.ins != null)
		{
			ClientNetwork.ins.NetworkUpdate();
		}
	}

	public struct SpawnablePrefab
	{
		public string assetPath;
		public GameObject gameObject;
	}
}
