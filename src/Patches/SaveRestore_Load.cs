using Carbon.Core;
using Carbon;
using HarmonyLib;
using JetBrains.Annotations;

[HarmonyPatch(typeof(SaveRestore), nameof(SaveRestore.Load), [typeof(string), typeof(bool)])]
[UsedImplicitly]
public class SaveRestore_Load
{
	public static void Postfix(string strFilename, bool allowOutOfDateSaves, ref bool __result)
	{
		if (Community.Runtime.ClientConfig.Enabled)
		{
			Carbon.Client.GameManager.ins = ServerMgr.Instance.gameObject.AddComponent<Carbon.Client.GameManager>();
			Carbon.Client.GameManager.ins.Init(true);

			CorePlugin.ReloadCarbonClientAddons(false);
		}
	}
}
