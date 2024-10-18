using Carbon;
using Carbon.Extensions;
using HarmonyLib;
using JetBrains.Annotations;
using UnityEngine;

[HarmonyPatch(typeof(TerrainHeightMap), nameof(TerrainHeightMap.GetHeight), [typeof(Vector3)])]
[UsedImplicitly]
public class TerrainHeightMap_GetHeight
{
	public static void Postfix(Vector3 worldPos, ref float __result)
	{
		if (Community.Runtime.ClientConfig.Environment.NoMap)
		{
			__result = __result.Clamp(0f, float.MaxValue);
		}
	}
}
