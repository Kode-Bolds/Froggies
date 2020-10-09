using System;
using Unity.Entities;
using UnityEngine;

public enum BuildingType
{
	Store = 0
}

public class BuildingAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
	//TODO (DB): Custom inspector.
	public bool placementDataOnly;
	public BuildingType buildingType;
	public float storeDepositRadius;
	public int health;

	public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
	{
		dstManager.AddComponent<BuildingTag>(entity);

		if (placementDataOnly)
		{
			dstManager.AddComponent<Placing>(entity);
		}
		else
		{
			switch (buildingType)
			{
				case BuildingType.Store:
					dstManager.AddComponentData(entity, new Store { depositRadius = storeDepositRadius });
					break;
				default:
					throw new Exception("Not a valid building type");
			}

			dstManager.AddComponentData(entity, new Health { health = health });
		}
	}
}
