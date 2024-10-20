using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace Carbon.Client;

public class GameManager : MonoBehaviour
{
	public static GameManager ins = new();

	public bool isServer;
	public Dictionary<uint, PrefabInfo> spawnablePrefabs = [];
	public Dictionary<ulong, BaseCarbonEntity> spawnedEntities = [];

	public void Init(bool isServer)
	{
		ins = this;
		this.isServer = isServer;

		if (!isServer)
		{
			ClientNetwork.ins.Start();
		}
	}

	public void RegisterPrefab(string assetPath, GameObject gameObject)
	{
		if (gameObject.GetComponent<BaseCarbonEntity>() == null)
		{
			return;
		}

		var info = new PrefabInfo(assetPath, gameObject);
		spawnablePrefabs[info.assetId] = info;
	}

	public void UnregisterAllPrefabs()
	{
		spawnablePrefabs.Clear();
	}

	public BaseCarbonEntity CreateSpawnable(uint assetId, Vector3 pos = default, Quaternion rot = default)
	{
		if (spawnablePrefabs.TryGetValue(assetId, out var prefab))
		{
			var instance = Instantiate(prefab.gameObject, pos, rot).GetComponent<BaseCarbonEntity>();
			instance.Init(isServer);
			return instance;
		}

		return null;
	}

	public void Update()
	{
		if (isServer && ServerNetwork.ins != null)
		{
			ServerNetwork.ins.OnNetwork();
		}

		if (!isServer && ClientNetwork.ins != null)
		{
			ClientNetwork.ins.OnNetwork();
		}
	}

	public struct PrefabInfo
	{
		public uint assetId;
		public string assetPath;
		public GameObject gameObject;

		public PrefabInfo(string assetPath, GameObject go = null)
		{
			this.assetPath = assetPath;
			assetId = ManifestHash(assetPath);
			gameObject = go;
		}

		private static uint ManifestHash(string str)
		{
			return string.IsNullOrEmpty(str) ? 0 : BitConverter.ToUInt32(new MD5CryptoServiceProvider().ComputeHash(Encoding.UTF8.GetBytes(str)), 0);
		}
	}
}
