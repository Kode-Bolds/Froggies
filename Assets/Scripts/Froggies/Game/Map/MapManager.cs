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
        public MapData[] maps;
        public MapData currentMap;

        public NativeArray2D<MapNode> map;

        protected override GameState ActiveGameState => GameState.Updating;

        private void LoadMap(int mapIndex)
        {
            currentMap = maps[mapIndex];
            
            //Load map as NativeArray
            map = new NativeArray2D<MapNode>(currentMap.grid, currentMap.gridSize.x, currentMap.gridSize.y, Allocator.Persistent);
        }

        public override void GetBehaviourDependencies(Dependencies dependencies)
        {
        }

        public override void InitBehaviour()
        {
            LoadMap(0);
        }

        public override void UpdateBehaviour()
        {
        }

        public override void FreeBehaviour()
        {
        }
    }
}