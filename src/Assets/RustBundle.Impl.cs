﻿using System.Collections.Generic;
using Carbon.Client.Assets;
using UnityEngine;

namespace Carbon.Client
{
	public partial class RustBundle
	{
		public void ProcessComponents(Asset asset)
		{
			foreach (var path in asset.cachedBundle.GetAllAssetNames())
			{
				var unityAsset = asset.cachedBundle.LoadAsset<Object>(path);

				if (unityAsset is GameObject go)
				{
					Recurse(go.transform);

					void Recurse(Transform transform)
					{
						foreach (Transform subTransform in transform)
						{
							Recurse(subTransform);
						}

						if (this.components.TryGetValue(transform.GetRecursiveName().ToLower(), out var components))
						{
							foreach (var component in components)
							{
								if (!component.Apply(transform.gameObject))
								{
									break;
								}
							}
						}
					}
				}
			}
		}

		public List<RustComponent> GetRustComponents(string prefab)
		{
			return this.components.TryGetValue(prefab, out var components) ? components : null;
		}
	}
}
