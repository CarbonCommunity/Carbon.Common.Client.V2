using System;
using UnityEngine;
using static Carbon.Client.GameManager;

namespace Carbon.Client;

public class BaseCarbonEntity : MonoBehaviour
{
	public NetworkId netId;
	public bool isSpawned;
	public bool isServer;
	public PrefabInfo prefab;

	public virtual void ServerInit()
	{

	}

	public virtual void ClientInit()
	{

	}

	public virtual void Init(bool isServer)
	{
		this.isServer = isServer;
	}

	public virtual void Spawn()
	{
		if (isSpawned)
		{
			Console.WriteLine("[WARN] Trying to spawn the entity twice!");
			return;
		}

		if (isServer)
		{
			netId = NetworkId.Next();
			ServerInit();
		}
		else
		{
			ClientInit();
		}

		isSpawned = true;
	}

	public virtual void Save(SaveInfo info)
	{

	}

	public virtual void Load(SaveInfo info)
	{

	}

	public struct SaveInfo
	{
		public Entity msg;
	}
}
