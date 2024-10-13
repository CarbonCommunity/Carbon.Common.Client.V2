using System;
using System.Collections.Generic;
using System.Linq;
using Carbon.Client.Assets;
using Carbon.Extensions;
using Facepunch;
using Network;
using UnityEngine;
using UnityEngine.Serialization;
using static DeployVolume;

namespace Carbon.Client
{
	public partial class RustPrefab
	{
		public GameObject Lookup()
		{
			return global::GameManager.server.FindPrefab(rustPath);
		}

		public void Apply(GameObject target)
		{
			target.transform.SetPositionAndRotation(position, Quaternion.Euler(rotation));
			target.transform.localScale = scale;
		}
	}
}
