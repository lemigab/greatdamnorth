using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace WorldUtil
{
    public static class Constructs
    {
        public enum Construct
        {
            HEX7_RIVER6 // 7-size hexagon with 6 rivers
        }

        private static Vector2Int[] VecsOf(params int[] vals)
        {
            Vector2Int[] vecs = new Vector2Int[vals.Length / 2];
            for (int i = 0; i < vals.Length; i += 2)
            {
                vecs[i] = new(vals[i], vals[i + 1]);
            }
            return vecs;
        }

        private readonly static Vector2Int[][] HEX7_RIVER6_RIVERS =
        {
            // River 0
            VecsOf(0, 0, 1, 0, 1, 1, 2, 2, 1, 2, 0, 1), 
            // River 1
            VecsOf(0, 3, 0, 2, 1, 3, 2, 3, 2, 4, 1, 4),
            // River 2
            VecsOf(3, 6, 2, 5, 3, 5, 3, 4, 4, 5, 4, 6),
            // River 3
            VecsOf(6, 6, 5, 6, 5, 5, 4, 4, 5, 4, 6, 5),
            // River 4
            VecsOf(6, 3, 6, 4, 5, 3, 4, 3, 4, 2, 5, 2),
            // River 5
            VecsOf(3, 0, 4, 1, 3, 1, 3, 2, 2, 1, 2, 0)
        };

        private readonly static Vector2Int[][] HEX7_RIVER6_ROADS =
        {
            // Roads from R0-1
            VecsOf(0, 1, 0, 2), VecsOf(2, 2, 2, 3),
            // Roads from R1-2
            VecsOf(1, 4, 2, 5), VecsOf(2, 3, 3, 4),
            // Roads from R2-3
            VecsOf(4, 6, 5, 6), VecsOf(3, 4, 4, 4),
            // Roads from R3-4
            VecsOf(6, 5, 6, 4), VecsOf(4, 4, 4, 3),
            // Roads from R4-5
            VecsOf(5, 2, 4, 1), VecsOf(4, 3, 3, 2),
            // Roads from R5-0
            VecsOf(2, 0, 1, 0), VecsOf(3, 2, 2, 2),
        };

        public static Vector2Int[][] RiverSets(Construct constr)
        => constr switch
        {
            Construct.HEX7_RIVER6 => HEX7_RIVER6_RIVERS,
            _ => throw new NotImplementedException()
        };

        public static Vector2Int[][] RoadSets(Construct constr)
        => constr switch
        {
            Construct.HEX7_RIVER6 => HEX7_RIVER6_ROADS,
            _ => throw new NotImplementedException()
        };
    }


    public class World
    {
        public readonly Hex[][] rivers;
        public readonly Hex[][] roads;

        public World(Hex[][] rivers, Hex[][] roads)
        {
            this.rivers = rivers;
            this.roads = roads;
        }
    }


    public class Hex
    {
        public readonly Vector2Int mapPosition;
        public readonly GameObject landMesh;
        public readonly GameObject waterMesh;

        public Hex(Vector2Int mapPosition,
            GameObject landMesh, GameObject waterMesh)
        {
            this.mapPosition = mapPosition;
            this.landMesh = landMesh;
            this.waterMesh = waterMesh;
        }
    }

}
