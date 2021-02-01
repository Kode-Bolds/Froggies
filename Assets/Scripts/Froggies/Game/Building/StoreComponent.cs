using System;
using Unity.Entities;

namespace Froggies
{
    [Serializable]
    public struct Store : IComponentData
    {
        public float depositRadius;
        //Capacity?
        //Transfer multiplier?
    }
}