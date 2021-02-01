using Unity.Entities;

namespace Froggies
{
	[GenerateAuthoringComponent]
	public struct Resources : IComponentData
	{
		public int buildingMaterial;
		public int food;
		public int rareResource;

		public void ModifyResource(ResourceType resourceType, int value)
		{
			switch (resourceType)
			{
				case ResourceType.Building:
					buildingMaterial += value;
					break;
				case ResourceType.Food:
					food += value;
					break;
				case ResourceType.Rare:
					rareResource += value;
					break;
			}
		}
	}
}