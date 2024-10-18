/*
*
* Copyright (c) 2022-2024 Carbon Community
* All rights reserved.
*
*/

using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Video;

namespace Carbon.Client.Assets;

public partial class Asset : IDisposable
{
	public IEnumerator UnpackBundleAsync()
	{
		if (isUnpacked)
		{
			Logger.Log($"Already unpacked Asset '{name}'");
			yield break;
		}

		var request = (AssetBundleCreateRequest)null;
		using var stream = new MemoryStream(data);
		yield return request = AssetBundle.LoadFromStreamAsync(stream);

		cachedBundle = request.assetBundle;
		Logger.Debug($"Unpacked bundle '{name}'", 2);

		cachedRustBundle = RustBundle.Deserialize(additionalData);
		cachedRustBundle.ProcessComponents(this);

		CacheAssets();
	}
	public void UnpackBundle()
	{
		if (isUnpacked)
		{
			Logger.Log($" Already unpacked '{name}'");
			return;
		}

		if (cachedBundle == null)
		{
			using var stream = new MemoryStream(data);
			cachedBundle = AssetBundle.LoadFromStream(stream);
		}

		Logger.Debug($"Unpacked bundle '{name}'", 2);
		cachedRustBundle = RustBundle.Deserialize(additionalData);
		cachedRustBundle.ProcessComponents(this);

		CacheAssets();
	}

	public void CacheAssets()
	{
		foreach (var asset in cachedBundle.GetAllAssetNames())
		{
			var processedAssetPath = asset.ToLower();

			if (!AddonManager.Instance.Prefabs.ContainsKey(processedAssetPath))
			{
				AddonManager.CachePrefab cache = default;
				cache.Path = asset;
				cache.Object = cachedBundle.LoadAsset<GameObject>(asset);

				ProcessClientObjects(cache.Object.transform);

				if (cachedRustBundle.rustPrefabs.TryGetValue(processedAssetPath, out var rustPrefabs))
				{
					cache.RustPrefabs = rustPrefabs;
				}

				AddonManager.Instance.Prefabs.Add(processedAssetPath, cache);
			}
		}
	}

	public T LoadPrefab<T>(string path) where T : UnityEngine.Object
	{
		if (!isUnpacked)
		{
			UnpackBundle();
		}

		return cachedBundle.LoadAsset<T>(path);
	}
	public T[] LoadAllPrefabs<T>() where T : UnityEngine.Object
	{
		if (!isUnpacked)
		{
			UnpackBundle();
		}

		return cachedBundle.LoadAllAssets<T>();
	}

	public void ProcessClientObjects(Transform transform)
	{
		void ClearComponent<T>() where T : Component
		{
			var component = transform.GetComponent<T>();

			if (component != null)
			{
				UnityEngine.Object.Destroy(component);
			}
		}

		ClearComponent<MeshRenderer>();
		ClearComponent<SkinnedMeshRenderer>();
		ClearComponent<AudioSource>();
		ClearComponent<VideoPlayer>();

		foreach (Transform child in transform)
		{
			ProcessClientObjects(child);
		}
	}
}
