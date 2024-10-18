using System;
using ProtoBuf;
using UnityEngine;

namespace Carbon.Client
{
	public partial class RustComponent : MonoBehaviour
	{
		public PostProcessMode Server = PostProcessMode.Active;
		public PostProcessMode Client = PostProcessMode.Active;
		public ComponentInfo Component = new ComponentInfo();
		public BehaviorInfo Behavior = new BehaviorInfo();

		public enum PostProcessMode
		{
			Active,
			Disabled,
			Destroyed
		}

		public class Member
		{
			public string Name;
			public string Value;
		}

		public class Platform
		{
			public bool Server;
			public bool Client;
		}

		public class ComponentInfo
		{
			public Platform CreateOn = new();
			public string Type;
			public Member[] Members;
		}

		public class BehaviorInfo
		{
			public float AutoDisableTimer = 0;
			public float AutoDestroyTimer = 0;
		}
	}
}
