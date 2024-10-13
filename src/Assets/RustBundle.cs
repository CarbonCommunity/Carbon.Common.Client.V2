using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using UnityEngine;

namespace Carbon.Client
{
	public partial class RustBundle
	{
		public Dictionary<string, List<RustPrefab>> rustPrefabs = new Dictionary<string, List<RustPrefab>>();

		public byte[] Serialize()
		{
			using var memoryStream = new MemoryStream();
			using (var gzipStream = new GZipStream(memoryStream, CompressionMode.Compress))
			{
				using var writer = new BinaryWriter(gzipStream);

				writer.Write(rustPrefabs.Count);

				foreach(var prefab in rustPrefabs)
				{
					writer.Write(prefab.Key);
					writer.Write(prefab.Value.Count);

					foreach (var value in prefab.Value)
					{
						writer.Write(value.rustPath);
						writer.Write(value.parentPath);
						writer.Write(value.parent);
						writer.Write(value.position.x);
						writer.Write(value.position.y);
						writer.Write(value.position.z);
						writer.Write(value.rotation.x);
						writer.Write(value.rotation.y);
						writer.Write(value.rotation.z);
						writer.Write(value.scale.x);
						writer.Write(value.scale.y);
						writer.Write(value.scale.z);

						writer.Write(value.entity.enforcePrefab);
						writer.Write((int)value.entity.flags);
						writer.Write(value.entity.skin);
						writer.Write(value.entity.health);
						writer.Write(value.entity.maxHealth);
					}
				}
			}

			return memoryStream.ToArray();
		}

		public static RustBundle Deserialize(byte[] buffer)
		{
			var bundle = new RustBundle();

			using var memoryStream = new MemoryStream(buffer);
			using var gzipStream = new GZipStream(memoryStream, CompressionMode.Decompress);
			using var reader = new BinaryReader(gzipStream);

			var count = reader.ReadInt32();
			for(int i = 0; i < count; i++)
			{
				var list = new List<RustPrefab>();
				bundle.rustPrefabs.Add(reader.ReadString(), list);

				var listCount = reader.ReadInt32();
				for (int y = 0; y < listCount; y++)
				{
					var prefab = new RustPrefab();
					prefab.rustPath = reader.ReadString();
					prefab.parentPath = reader.ReadString();
					prefab.position = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
					prefab.rotation = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
					prefab.scale = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
					prefab.entity = new();
					prefab.entity.enforcePrefab = reader.ReadBoolean();
					prefab.entity.flags = (RustPrefab.EntityData.EntityFlags)reader.ReadInt32();
					prefab.entity.skin = reader.ReadUInt64();
					prefab.entity.health = reader.ReadSingle();
					prefab.entity.maxHealth = reader.ReadSingle();
				}
			}

			return bundle;
		}
	}
}
