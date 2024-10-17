using System.Collections.Generic;
using UnityEngine;

namespace Carbon.Client;

public class GameManager : MonoBehaviour
{
	public static GameManager ins;

	public bool isServer;
	public Dictionary<uint, PrefabInfo> spawnablePrefabs = [];

	public void Init(bool isServer)
	{
		ins = this;
		this.isServer = isServer;
	}

	public T CreateSpawnable<T>(uint assetId, Vector3 pos = default, Quaternion rot = default) where T : BaseCarbonEntity
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
			ServerNetwork.ins.OnNetwork();
		}

		if (ClientNetwork.ins != null)
		{
			ClientNetwork.ins.OnNetwork();
		}
	}

	public struct PrefabInfo
	{
		public string assetPath;
		public GameObject gameObject;
	}
}
