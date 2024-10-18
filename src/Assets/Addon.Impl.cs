using System;

namespace Carbon.Client.Assets;

public partial class Addon : IStore<Addon, Asset>, IDisposable
{
	public partial class Manifest
	{
		public void Save(NetWrite write)
		{
			write.String(info.name);
			write.String(info.author);
			write.String(info.description);
			write.String(info.version);
			write.String(info.thumbnail);
			write.Int32(assets.Length);
			foreach (var asset in assets)
			{
				write.String(asset.name);
				write.Int32(asset.bufferLength);
			}
			write.Int64(creationTime);
			write.String(url);
			write.String(checksum);
		}

		public void Load(NetRead read)
		{
			info = default;
			info.name = read.String();
			info.author = read.String();
			info.description = read.String();
			info.version = read.String();
			info.thumbnail = read.String();

			var assetCount = read.Int32();

			if (assets == null)
			{
				assets = new Asset.Manifest[assetCount];
			}
			else
			{
				Array.Resize(ref assets, assetCount);
			}

			for (int i = 0; i < assetCount; i++)
			{
				var manifest = assets[i] ??= new Asset.Manifest();
				manifest.name = read.String();
				manifest.bufferLength = read.Int32();
			}

			creationTime = read.Int64();
			url = read.String();
			checksum = read.String();
		}
	}
}
