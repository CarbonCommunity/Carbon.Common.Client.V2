/*
*
* Copyright (c) 2022-2024 Carbon Community
* All rights reserved.
*
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Carbon.Extensions;
using UnityEngine;

namespace Carbon.Client.Assets;

public class AddonManager
{
	public static AddonManager Instance { get; internal set; } = new();

	public FacepunchBehaviour Persistence => Community.Runtime.Core.persistence;

	public Dictionary<Addon, CacheAddon> LoadedAddons { get; } = new();
	public Dictionary<string, CachePrefab> Prefabs { get; } = new();

	public List<GameObject> CreatedPrefabs { get; } = new();
	public List<GameObject> CreatedRustPrefabs { get; } = new();
	public List<BaseEntity> CreatedEntities { get; } = new();

	public struct CacheAddon
	{
		public string Url;
		public Asset Scene;
		public Asset Models;
		public string[] ScenePrefabs;

		public bool HasScene()
		{
			return Scene != null && Scene != Models;
		}
	}
	public struct CachePrefab
	{
		public string Path;
		public GameObject Object;
		public List<RustPrefab> RustPrefabs;
	}

	internal void FixName(GameObject gameObject)
	{
		gameObject.name = gameObject.name.Replace("(Clone)", string.Empty);
	}
	internal void ProcessEntity(BaseEntity entity, RustPrefab source)
	{
		entity.Spawn();
		entity.EnableSaving(false);
		entity.skinID = source.entity.skin;

		if (source.entity.flags != 0)
		{
			entity.SetFlag((BaseEntity.Flags)source.entity.flags, true);
		}

		if (entity is BaseCombatEntity combatEntity)
		{
			if (source.entity.maxHealth != -1)
			{
				combatEntity.SetMaxHealth(source.entity.maxHealth);
			}

			if (source.entity.health != -1)
			{
				combatEntity.SetHealth(source.entity.health);
			}
		}
	}

	public Transform LookupParent(Transform origin, string parent)
	{
		return origin == null ? null : origin.Find(parent);
	}

	public GameObject CreateFromAsset(string path, Asset asset)
	{
		if (asset == null)
		{
			Logger.Warn($"Couldn't find '{path}' as the asset provided is null. (CreateFromAsset)");
			return null;
		}

		if (string.IsNullOrEmpty(path))
		{
			Logger.Warn($"Couldn't find prefab from asset '{asset.name}' as it's an empty string. (CreateFromAsset)");
			return null;
		}

		var prefab = asset.LoadPrefab<GameObject>(path);

		if (prefab != null)
		{
			var prefabInstance = CreateBasedOnImpl(prefab);

			if (asset.cachedRustBundle.rustPrefabs.TryGetValue(path, out var rustPrefabs))
			{
				CreateRustPrefabs(prefabInstance.transform, rustPrefabs);
			}

			return prefabInstance;
		}
		else
		{
			Logger.Warn($"Couldn't find '{path}' in any addons or assets. (CreateFromAsset)");
		}

		return null;
	}
	public GameObject CreateFromCache(string path)
	{
		if (Prefabs.TryGetValue(path, out var prefab))
		{
			var prefabInstance = CreateBasedOnImpl(prefab.Object);

			CreateRustPrefabs(prefabInstance.transform, prefab.RustPrefabs);
			OnInstanceCreated(prefabInstance, path, null);
			return prefabInstance;
		}

		return null;
	}
	public GameObject CreateRustPrefab(Transform target, RustPrefab prefab)
	{
		var lookup = prefab.Lookup();

		if (lookup == null)
		{
			Logger.Warn($"Couldn't find '{prefab.rustPath}' as the asset provided is null. (CreateRustPrefab)");
			return null;
		}

		var entity = lookup.GetComponent<BaseEntity>();
		var isEntity = entity != null;

		if (isEntity && !prefab.entity.enforcePrefab)
		{
			var entityInstance = global::GameManager.server.CreateEntity(prefab.rustPath, prefab.position, Quaternion.Euler(prefab.rotation));

			if (entityInstance == null) return null;

			ProcessEntity(entityInstance, prefab);

			if (prefab.parent)
			{
				var parent = LookupParent(target, prefab.parentPath);

				if (parent != null)
				{
					entityInstance.transform.SetParent(parent, true);
				}
			}

			CreatedEntities.Add(entityInstance);

			OnInstanceCreated(entityInstance.gameObject, prefab.rustPath, prefab.parentPath);
			return entityInstance.gameObject;
		}
		else
		{
			var instance = global::GameManager.server.CreatePrefab(prefab.rustPath, prefab.position, Quaternion.Euler(prefab.rotation), prefab.scale);

			if (instance == null) return null;

			CreatedRustPrefabs.Add(instance);

			if (prefab.parent)
			{
				var parent = LookupParent(target, prefab.parentPath);

				if (parent != null)
				{
					instance.transform.SetParent(parent, true);
				}
			}

			OnInstanceCreated(instance, prefab.rustPath, prefab.parentPath);
			return instance;
		}
	}
	public void CreateRustPrefabs(Transform target, IEnumerable<RustPrefab> prefabs)
	{
		if (prefabs == null)
		{
			return;
		}

		foreach (var prefab in prefabs)
		{
			CreateRustPrefab(target, prefab);
		}
	}

	public void CreateFromCacheAsync(string path, Action<GameObject> callback = null)
	{
		if (string.IsNullOrEmpty(path))
		{
			Logger.Warn($"Couldn't find '{path}' as it's an empty string. (CreateFromCacheAsync)");
			callback?.Invoke(null);
			return;
		}

		if (Prefabs.TryGetValue(path, out var prefab))
		{
			callback += go =>
			{
				CreateRustPrefabsAsync(go.transform, prefab.RustPrefabs);
			};

			Persistence.StartCoroutine(CreateBasedOnAsyncImpl(prefab.Object, callback));
		}
		else
		{
			Logger.Warn($"Couldn't find '{path}' as it hasn't been cached yet. Use 'CreateFromAssetAsync'? (CreateFromCacheAsync)");
			callback?.Invoke(null);
		}
	}
	public void CreateFromAssetAsync(string path, Asset asset, Action<GameObject> callback = null)
	{
		if (asset == null)
		{
			Logger.Warn($"Couldn't find '{path}' as the asset provided is null. (CreateFromAssetAsync)");
			callback?.Invoke(null);
			return;
		}

		if (string.IsNullOrEmpty(path))
		{
			Logger.Warn($"Couldn't find '{path}' as it's an empty string. (CreateFromAssetAsync)");
			callback?.Invoke(null);
			return;
		}

		var prefab = asset.LoadPrefab<GameObject>(path);

		if (prefab != null)
		{
			callback += go =>
			{
				if (asset.cachedRustBundle.rustPrefabs.TryGetValue(path, out var rustPrefabs))
				{
					CreateRustPrefabsAsync(go.transform, rustPrefabs);
				}
			};
			Persistence.StartCoroutine(CreateBasedOnAsyncImpl(prefab, callback));
		}
		else
		{
			Logger.Warn($"Couldn't find '{path}' in any addons or assets. (CreateFromAssetAsync)");
			callback?.Invoke(null);
		}
	}
	public void CreateRustPrefabAsync(Transform target, RustPrefab prefab)
	{
		var lookup = prefab.Lookup();

		if (lookup == null)
		{
			Logger.Warn($"Couldn't find '{prefab.rustPath}' as the asset provided is null. (CreateRustPrefabAsync)");
			return;
		}

		var entity = lookup.GetComponent<BaseEntity>();
		var isEntity = entity != null;

		if (isEntity && !prefab.entity.enforcePrefab)
		{
			var entityInstance = global::GameManager.server.CreateEntity(prefab.rustPath, prefab.position, Quaternion.Euler(prefab.rotation));
			ProcessEntity(entityInstance, prefab);

			if (prefab.parent)
			{
				var parent = LookupParent(target, prefab.parentPath);

				if (parent != null)
				{
					entityInstance.transform.SetParent(parent, true);
				}
			}

			CreatedEntities.Add(entityInstance);
		}
		else
		{
			Persistence.StartCoroutine(CreateBasedOnAsyncImpl(lookup, instance =>
			{
				prefab.Apply(instance);

				if (prefab.parent)
				{
					var parent = LookupParent(target, prefab.parentPath);

					if (parent != null)
					{
						instance.transform.SetParent(parent, true);
					}
				}

				// prefab.ApplyModel(go, go.GetComponent<Model>() ?? go.GetComponentInChildren<Model>());
			}));
		}
	}
	public void CreateRustPrefabsAsync(Transform target, IEnumerable<RustPrefab> prefabs)
	{
		if (prefabs == null)
		{
			return;
		}

		Persistence.StartCoroutine(CreateBasedOnPrefabsAsyncImpl(target, prefabs));
	}

	#region Helpers

	internal GameObject CreateBasedOnImpl(GameObject source)
	{
		if (source == null)
		{
			return null;
		}

		var result = UnityEngine.Object.Instantiate(source);
		CreatedPrefabs.Add(result);

		FixName(result);

		return result;
	}
	internal IEnumerator CreateBasedOnAsyncImpl(GameObject gameObject, Action<GameObject> callback = null)
	{
		var result = (GameObject)null;

		yield return result = UnityEngine.Object.Instantiate(gameObject);
		CreatedPrefabs.Add(result);

		FixName(result);

		var context = Prefabs.FirstOrDefault(x => x.Value.Object == gameObject);
		OnInstanceCreated(result, context.Key, null);

		callback?.Invoke(result);
	}
	internal IEnumerator CreateBasedOnPrefabsAsyncImpl(Transform target, IEnumerable<RustPrefab> prefabs)
	{
		foreach (var prefab in prefabs)
		{
			var lookup = prefab.Lookup();

			if (lookup == null)
			{
				Logger.Warn($"Couldn't find '{prefab.rustPath}' as the asset provided is null. (CreateBasedOnPrefabsAsyncImpl)");
				continue;
			}

			var entity = lookup.GetComponent<BaseEntity>();
			var isEntity = entity != null;

			if (isEntity && !prefab.entity.enforcePrefab)
			{
				var entityInstance = global::GameManager.server.CreateEntity(prefab.rustPath, prefab.position, Quaternion.Euler(prefab.rotation));
				ProcessEntity(entityInstance, prefab);

				if (prefab.parent)
				{
					var parent = LookupParent(target, prefab.parentPath);

					if (parent != null)
					{
						entityInstance.transform.SetParent(parent, true);
					}
				}

				OnInstanceCreated(entityInstance.gameObject, prefab.rustPath, prefab.parentPath);
				CreatedEntities.Add(entityInstance);
			}
			else
			{
				var instance = (GameObject)null;

				yield return instance = global::GameManager.server.CreatePrefab(prefab.rustPath, prefab.position, Quaternion.Euler(prefab.rotation), prefab.scale);

				if (prefab.parent)
				{
					var parent = LookupParent(target, prefab.parentPath);

					if (parent != null)
					{
						instance.transform.SetParent(parent, true);
					}
				}

				OnInstanceCreated(instance, prefab.rustPath, prefab.parentPath);
				CreatedRustPrefabs.Add(instance);
			}
		}
	}
	internal IEnumerator CreateBasedOnEnumerableAsyncImpl(IEnumerable<GameObject> gameObjects, Action<GameObject> callback = null)
	{
		foreach (var gameObject in gameObjects)
		{
			yield return CreateBasedOnAsyncImpl(gameObject, callback);
		}
	}

	internal void OnInstanceCreated(GameObject instance, string mainAssetPath, string secondAssetPath)
	{
		// OnCustomPrefabInstance
		HookCaller.CallStaticHook(1792976062, instance, mainAssetPath, secondAssetPath);
	}

	#endregion

	public void Install(List<Addon> addons)
	{
		foreach (var addon in addons)
		{
			foreach (var asset in addon.assets)
			{
				asset.Value.UnpackBundle();
			}

			if (!LoadedAddons.ContainsKey(addon))
			{
				Logger.Log($" C4C: Installed addon '{addon.name} v{addon.version}' by {addon.author}");
				LoadedAddons.Add(addon, GetAddonCache(addon));
			}
		}

		CreateScenePrefabs(false);
	}
	public IEnumerator InstallAsync(List<Addon> addons, Action callback = null)
	{
		foreach (var addon in addons)
		{
			foreach (var asset in addon.assets)
			{
				yield return asset.Value.UnpackBundleAsync();
			}

			if (!LoadedAddons.ContainsKey(addon))
			{
				Logger.Log($" C4C: Installed addon '{addon.name} v{addon.version}' by {addon.author}");
				LoadedAddons.Add(addon, GetAddonCache(addon));
			}
		}

		CreateScenePrefabs(true);

		callback?.Invoke();
	}
	public void Uninstall(bool prefabs = true, bool rustPrefabs = true, bool customPrefabs = true, bool entities = true)
	{
		if (rustPrefabs)
		{
			if (CreatedRustPrefabs.Count != 0)
			{
				Console.WriteLine($" C4C: Cleared {CreatedRustPrefabs.Count:n0} Rust {CreatedRustPrefabs.Count.Plural("prefab", "prefabs")}");
			}

			ClearRustPrefabs();
		}
		if (prefabs)
		{
			if (CreatedPrefabs.Count != 0)
			{
				Console.WriteLine($" C4C: Cleared {CreatedPrefabs.Count:n0} {CreatedPrefabs.Count.Plural("prefab", "prefabs")}");
			}

			ClearPrefabs();
		}
		if (customPrefabs)
		{
			if (Prefabs.Count != 0)
			{
				Console.WriteLine($" C4C: Cleared {Prefabs.Count:n0} custom prefab cache {Prefabs.Count.Plural("element", "elements")}");
			}

			ClearCustomPrefabs();
		}
		if (entities)
		{
			if (CreatedEntities.Count != 0)
			{
				Console.WriteLine($" C4C: Cleared {CreatedEntities.Count:n0} {CreatedEntities.Count.Plural("entity", "entities")}");
			}

			ClearEntities();
		}

		if (LoadedAddons.Count != 0)
		{
			Console.WriteLine($" C4C: Done disposing total of {LoadedAddons.Count:n0} {LoadedAddons.Count.Plural("addon", "addons")} with {LoadedAddons.Sum(x => x.Key.assets.Count):n0} assets from memory");
		}

		foreach (var addon in LoadedAddons)
		{
			foreach (var asset in addon.Key.assets)
			{
				try
				{
					asset.Value.Dispose();
				}
				catch (Exception ex)
				{
					Console.WriteLine($" C4C: Failed disposing asset '{asset.Key}' of addon {addon.Key.name} ({ex.Message})\n{ex.StackTrace}");
				}
			}
		}

		LoadedAddons.Clear();
	}

	public void CreateScenePrefabs(bool async)
	{
		foreach (var addon in LoadedAddons)
		{
			if (addon.Value.ScenePrefabs == null) continue;

			foreach (var prefab in addon.Value.ScenePrefabs)
			{
				if (async)
				{
					CreateFromCacheAsync(prefab, prefabInstance =>
					{
						OnPrefabInstanceCreated(prefabInstance);
					});
				}
				else
				{
					OnPrefabInstanceCreated(CreateFromCache(prefab));
				}

				void OnPrefabInstanceCreated(GameObject prefabInstance)
				{
					if (prefabInstance == null)
					{
						return;
					}

					// OnCustomScenePrefab
					HookCaller.CallStaticHook(37923964, prefabInstance, prefab, addon.Value, addon.Key);

					Logger.Debug($" C4C: Created prefab '{prefab}'");
				}
			}
		}
	}

	internal WebClient _client = new();

	public async Task<List<Addon>> LoadAddons(string[] addons, bool async = true)
	{
		var addonResults = new List<Addon>();

		foreach (var addon in addons)
		{
			if (addon.StartsWith("http"))
			{
				if (async)
				{
					await Community.Runtime.Core.webrequest.EnqueueDataAsync(addon, null, (code, data) =>
					{
						OnData(data);
					}, Community.Runtime.Core);
				}
				else
				{
					var data = _client.DownloadData(addon);
					OnData(data);
				}

				void OnData(byte[] data)
				{
					Logger.Warn($" C4C: Content downloaded '{Path.GetFileName(addon)}' ({ByteEx.Format(data.Length, stringFormat: "{0}{1}").ToLower()})");

					try
					{
						var instance = Addon.Deserialize(data);
						instance.url = addon;

						addonResults.Add(instance);
					}
					catch (Exception ex)
					{
						Logger.Error($" C4C: Addon file protocol out of date or invalid.", ex);
					}
				}
			}
			else
			{
				if (OsEx.File.Exists(addon))
				{
					try
					{
						var data = OsEx.File.ReadBytes(addon);
						Logger.Warn($" C4C: Content loaded locally '{Path.GetFileName(addon)}' ({ByteEx.Format(data.Length, stringFormat: "{0}{1}").ToLower()})");

						var instance = Addon.Deserialize(data);
						instance.url = addon;
						addonResults.Add(instance);
					}
					catch (Exception ex)
					{
						Logger.Error($" C4C: Addon file protocol out of date or invalid.", ex);
					}
				}
				else
				{
					Logger.Warn($" C4C: Couldn't find Addon file at path: {addon}");
				}
			}
		}

		return addonResults;
	}

	public CacheAddon GetAddonCache(Addon addon)
	{
		CacheAddon cache = default;
		cache.Url = addon.url;
		cache.Scene = addon.assets.FirstOrDefault(x => x.Key == "scene").Value;
		cache.Models = addon.assets.FirstOrDefault(x => x.Key == "models").Value;

		if (cache.Scene != null)
		{
			cache.ScenePrefabs = cache.Scene.cachedBundle.GetAllAssetNames();
		}

		return cache;
	}

	public void ClearPrefabs()
	{
		foreach (var prefab in CreatedPrefabs)
		{
			try
			{
				UnityEngine.Object.Destroy(prefab);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Failed disposing a prefab ({ex.Message})\n{ex.StackTrace}");
			}
		}

		CreatedPrefabs.Clear();
	}
	public void ClearRustPrefabs()
	{
		foreach (var prefab in CreatedRustPrefabs)
		{
			try
			{
				UnityEngine.Object.Destroy(prefab);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Failed disposing a Rust prefab ({ex.Message})\n{ex.StackTrace}");
			}
		}

		CreatedRustPrefabs.Clear();
	}
	public void ClearCustomPrefabs()
	{
		foreach (var prefab in Prefabs)
		{
			try
			{
				UnityEngine.Object.Destroy(prefab.Value.Object);
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Failed disposing asset '{prefab.Key}' ({ex.Message})\n{ex.StackTrace}");
			}
		}

		Prefabs.Clear();

		GameManager.ins.UnregisterAllPrefabs();
	}
	public void ClearEntities()
	{
		foreach (var entity in CreatedEntities)
		{
			try
			{
				if (entity.isServer && !entity.IsDestroyed)
				{
					entity.Kill();
				}
			}
			catch (Exception ex)
			{
				Console.WriteLine($"Failed disposing a prefab ({ex.Message})\n{ex.StackTrace}");
			}
		}

		CreatedEntities.Clear();
	}
}
