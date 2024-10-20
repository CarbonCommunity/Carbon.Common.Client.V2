using System;
using System.Collections.Generic;
using UnityEngine;

namespace Carbon.Client;

public partial class BaseCarbonEntity : MonoBehaviour
{
	public NetworkId netId;
	public bool isSpawned;
	public bool isServer;
	public GameManager.PrefabInfo prefab;
	public List<CarbonClientConnection> subscribers = [];
	public List<CarbonClientConnection> addedSubscribers = [];
	public List<CarbonClientConnection> removedSubscribers = [];

	public virtual void Init(bool isServer)
	{
		this.isServer = isServer;
	}

	public virtual void ServerInit()
	{
		InvokeRepeating(nameof(UpdateSubscribers), 1f, 1f);
	}
	public virtual void ClientInit()
	{

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

		GameManager.ins.spawnedEntities[netId.Value] = this;

		isSpawned = true;

		UpdateSubscribers();
		SendNetworkUpdate(position: true);
	}
	public virtual void Save(NetWrite write)
	{
	}
	public virtual void Load(NetRead read)
	{

	}

	public virtual void SendNetworkUpdate(bool position = false)
	{
		foreach(var subscriber in subscribers)
		{
			subscriber.Write.Start(Messages.EntityUpdate);
			subscriber.Write.NetworkId(netId);
			Save(subscriber.Write);
			subscriber.Write.Send();

			if (position)
			{
				subscriber.Write.Start(Messages.EntityPosition);
				subscriber.Write.NetworkId(netId);
				subscriber.Write.Vector3(transform.position);
				subscriber.Write.Quaternion(transform.rotation);
				subscriber.Write.Vector3(transform.localScale);
				subscriber.Write.Send();
			}
		}
	}
	public virtual void SendNetworkPosition()
	{
		foreach (var subscriber in subscribers)
		{
			subscriber.Write.Start(Messages.EntityPosition);
			subscriber.Write.NetworkId(netId);
			subscriber.Write.Vector3(transform.position);
			subscriber.Write.Quaternion(transform.rotation);
			subscriber.Write.Vector3(transform.localScale);
			subscriber.Write.Send();
		}
	}

	public virtual void UpdateSubscribers()
	{
		foreach(var client in CarbonClientManager.ins.Clients)
		{
			if (!subscribers.Contains(client.Value))
			{
				addedSubscribers.Add(client.Value);
			}
		}

		foreach (var subscriber in subscribers)
		{
			if (!CarbonClientManager.ins.Clients.ContainsValue(subscriber))
			{
				removedSubscribers.Add(subscriber);
			}
		}

		foreach (var subscriber in addedSubscribers)
		{
			OnCreated(subscriber.Write);
			subscribers.Add(subscriber);
		}

		foreach (var subscriber in removedSubscribers)
		{
			OnDestroyed(subscriber.Write);
			subscribers.Remove(subscriber);
		}

		addedSubscribers.Clear();
		removedSubscribers.Clear();
	}

	/*
		public virtual void UpdateNetworkGroup()
		{
			var existentGroup = group;
			var currentGroup = Network.Net.sv.visibility.GetGroup(transform.position);
			group = currentGroup.ID;

			if (existentGroup != group)
			{
				OnNetworkGroupChange(existentGroup == 0 ? null : Network.Net.sv.visibility.Get(existentGroup), currentGroup);
			}
		}

		public virtual void OnNetworkGroupChange(Group old, Group current)
		{
			if (old != null)
			{
				foreach (var subscriber in old.subscribers)
				{
					var client = CarbonClientManager.ins.Get(subscriber) as CarbonClient;
					OnDestroyed(client.Write);
				}
			}

			foreach (var subscriber in current.subscribers)
			{
				var client = CarbonClientManager.ins.Get(subscriber) as CarbonClient;
				OnCreated(client.Write);
			}
		}
	*/

	public virtual void OnCreated(NetWrite write)
	{
		write.Start(Messages.EntityCreate);
		write.Write(prefab.assetId);
		write.Write(netId);
		write.Send();
	}
	public virtual void OnDestroyed(NetWrite write)
	{
		write.Start(Messages.EntityDestroy);
		write.Write(netId);
		write.Send();
	}
}
