using System;
using Unity.Entities;

namespace Froggies
{
    [Serializable]
    public struct Harvester : IComponentData
    {
        public int carryCapacity;
        public int harvestAmount;
        public int currentlyCarryingAmount;
        public ResourceType currentlyCarryingType;
        public float harvestTickTimer;
        public float harvestTickCooldown;
        public float harvestRange;
    }
}