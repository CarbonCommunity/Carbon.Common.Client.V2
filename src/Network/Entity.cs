using Facepunch;

namespace Carbon.Client;

public class Entity : Pool.IPooled
{
	public BaseCarbonEntity baseCarbonEntity;

	public class BaseCarbonEntity : Pool.IPooled
	{
		public NetworkId netId;

		public void EnterPool()
		{
			netId = default;
		}

		public void LeavePool()
		{

		}
	}

	public void Serialize(NetWrite write)
	{
		if (baseCarbonEntity != null)
		{
			write.Int32(100);
			write.NetworkId(baseCarbonEntity.netId);
		}
	}

	public Entity Deserialize(NetRead read)
	{
		var entity = Pool.Get<Entity>();

		return entity;
	}

	public void EnterPool()
	{
		if (baseCarbonEntity != null)
		{
			Pool.Free(ref baseCarbonEntity);
		}
	}

	public void LeavePool()
	{

	}
}
