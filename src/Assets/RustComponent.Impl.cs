﻿using Carbon.Components;
using Carbon.Extensions;
using System.Reflection;
using System;
using UnityEngine;

namespace Carbon.Client
{
	public partial class RustComponent
	{
		public Component _instance;

		public static readonly char[] LayerSplitter = new char[] { '|' };

		public bool Apply(GameObject go)
		{
			if (!PreHandleDisabled(go))
			{
				if (PreHandleDestroy(go))
				{
					return false;
				}
			}

			return PreHandleComponents(go);
		}

		internal bool PreHandleComponents(GameObject go)
		{
			if (!Component.CreateOn.Server || Server == PostProcessMode.Destroyed || _instance != null)
			{
				return false;
			}

			var type = AccessToolsEx.TypeByName(Component.Type);
			_instance = go.AddComponent(type);

			const BindingFlags _monoFlags = BindingFlags.Instance | BindingFlags.Public;

			if (Component.Members != null && Component.Members.Length > 0)
			{
				foreach (var member in Component.Members)
				{
					try
					{
						var field = type.GetField(member.Name, _monoFlags);
						var memberType = field.FieldType;
						var value = (object)null;

						if (memberType == typeof(LayerMask))
						{
							value = new LayerMask { value = member.Value.ToInt() };
						}
						else
							switch (memberType.Name)
							{
								case "Vector2":
								{
									using var temp = TempArray<string>.New(member.Value.Split(','));
									field.SetValue(_instance,
										new Vector2(temp.Get(0, "0").ToFloat(), temp.Get(1, "0").ToFloat()));
									break;
								}
								case "Vector3":
								{
									using var temp = TempArray<string>.New(member.Value.Split(','));
									field.SetValue(_instance,
										new Vector3(temp.Get(0, "0").ToFloat(), temp.Get(1, "0").ToFloat(),
											temp.Get(2, "0").ToFloat()));
									break;
								}
								default:
								{
									if (memberType.IsEnum)
									{
										value = Enum.Parse(memberType, member.Value);
									}
									else
									{
										value = Convert.ChangeType(member.Value, memberType);
									}

									break;
								}
							}

						if (field != null)
						{
							field?.SetValue(_instance, value);
						}
						else
						{
							Logger.Error(
								$" Couldn't find member '{member.Name}' for '{Component.Type}' on '{go.transform.GetRecursiveName()}'");
						}
					}
					catch (Exception ex)
					{
						Logger.Error(
							$"Failed assigning Rust component member '{member.Name}' to {go.transform.GetRecursiveName()}",
							ex);
					}
				}
			}

			switch (_instance)
			{
				case TriggerTemperature temperature:
					temperature.OnValidate();
					break;
			}

			return true;
		}

		internal bool PreHandleDisabled(GameObject go)
		{
			if (Server != PostProcessMode.Disabled)
			{
				return false;
			}

			go.SetActive(false);
			return true;
		}

		internal bool PreHandleDestroy(GameObject go)
		{
			if (Server != PostProcessMode.Destroyed)
			{
				return false;
			}

			go.SetActive(false);
			return true;
		}

		internal bool PostHandleCreation(GameObject go)
		{
			if (Server != PostProcessMode.Active)
			{
				return false;
			}

			if (Behavior == null)
			{
				return false;
			}

			if (Behavior.AutoDisableTimer != 0)
			{
				Community.Runtime.Core.timer.In(Behavior.AutoDisableTimer, () => go.SetActive(false));
			}
			else if (Behavior.AutoDestroyTimer != 0)
			{
				Community.Runtime.Core.timer.In(Behavior.AutoDestroyTimer, () => go.SetActive(false));
			}

			return true;
		}
	}
}
