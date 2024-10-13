using System.Collections.Generic;
using UnityEngine;

namespace Carbon.Client;

public class GameManager : MonoBehaviour
{
	public static bool isServer;
	public static Dictionary<uint, PrefabInfo> spawnablePrefabs = [];

	public static void Initialize(bool isServer)
	{
		GameManager.isServer = isServer;
	}

	public static T CreateSpawnable<T>(uint assetId, Vector3 pos = default, Quaternion rot = default) where T : BaseCarbonEntity
	{
		if (spawnablePrefabs.TryGetValue(assetId, out var prefab))
		{
			var instance = Instantiate(prefab.gameObject, pos, rot).GetComponent<T>();
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

	public struct PrefabInfo
	{
		public string assetPath;
		public GameObject gameObject;
	}
}
