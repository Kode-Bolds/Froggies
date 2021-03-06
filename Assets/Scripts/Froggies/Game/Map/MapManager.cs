using System;
using Kodebolds.Core;
using Unity.Collections;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

namespace Froggies
{
    public class MapManager : KodeboldBehaviour
    {
        public MapData mapSO;

        public NativeArray2D<MapNode> map;
        

        
        
        private void LoadMap()
        {
            //Load map as NativeArray
            map = new NativeArray2D<MapNode>(mapSO.grid, Allocator.Persistent);
        }

        



        public override void GetBehaviourDependencies(Dependencies dependencies)
        {
            throw new NotImplementedException();
        }

        public override void InitBehaviour()
        {
            throw new NotImplementedException();
        }

        public override void UpdateBehaviour()
        {
            throw new NotImplementedException();
        }

        public override void FreeBehaviour()
        {
            throw new NotImplementedException();
        }
    }
}