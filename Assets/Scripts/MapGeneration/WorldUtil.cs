using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;

namespace WorldUtil
{

    public class World
    {
        // A list of rivers, which are themselves lists of hexes
        // Hexes are ordered from start to end within a river
        public readonly List<List<Hex>> Rivers;

        // A list of roads, which are themselves tuples of two hexes
        // Order of the tuple is arbitrary since roads are bidirectional
        public readonly List<Tuple<Hex, Hex>> Roads;

        public World(List<List<Hex>> rivers, List<Tuple<Hex, Hex>> roads)
        {
            Rivers = rivers;
            Roads = roads;
        }

        public List<Hex> FindRiverWithHex(Hex hex)
        {
            foreach (List<Hex> river in Rivers)
                if (river.Contains(hex)) return river;
            throw new Exception("Hex not along any river");
        }

        public List<Hex> UpstreamFrom(Hex target)
        {
            List<Hex> r = FindRiverWithHex(target);
            int hPos = r.IndexOf(target);
            if (hPos == 0) return new();
            return r.GetRange(0, hPos);
        }

        public List<Hex> DownstreamFrom(Hex target)
        {
            List<Hex> r = FindRiverWithHex(target);
            int hPos = r.IndexOf(target);
            if (hPos == r.Count - 1) return new();
            return r.GetRange(hPos + 1, r.Count - 1 - hPos);
        }
    }


    public class Hex
    {
        public readonly Vector2Int mapPosition;
        public readonly GameObject landMesh;
        public readonly GameObject waterMesh;
        public readonly BeaverDam exitDam;

        public Hex(Vector2Int mapPosition,
            GameObject landMesh, GameObject waterMesh, BeaverDam exitDam)
        {
            this.mapPosition = mapPosition;
            this.landMesh = landMesh;
            this.waterMesh = waterMesh;
            this.exitDam = exitDam;
        }
    }


    public static class Constructs
    {
        public enum Construct
        {
            HEX7_EMPTY, // 7-size hexagon with no rivers/roads
            HEX7_RIVER6 // 7-size hexagon with 6 rivers and 12 roads
        }

        private static Vector2Int[] VecsOf(params int[] vals)
        {
            Vector2Int[] vecs = new Vector2Int[vals.Length / 2];
            for (int i = 0; i < vals.Length; i += 2)
            {
                vecs[i / 2] = new(vals[i], vals[i + 1]);
            }
            return vecs;
        }

        private readonly static Vector2Int[][] HEX7_EMPTY_RIVERS = { };
        private readonly static Vector2Int[][] HEX7_EMPTY_ROADS = { };

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
            Construct.HEX7_EMPTY => HEX7_EMPTY_RIVERS,
            Construct.HEX7_RIVER6 => HEX7_RIVER6_RIVERS,
            _ => throw new NotImplementedException()
        };

        public static Vector2Int[][] RoadSets(Construct constr)
        => constr switch
        {
            Construct.HEX7_EMPTY => HEX7_EMPTY_ROADS,
            Construct.HEX7_RIVER6 => HEX7_RIVER6_ROADS,
            _ => throw new NotImplementedException()
        };

        public static HexSide DownstreamSideOf(
            Vector2Int srcPos, int mapSize, Construct constr)
        {
            foreach (Vector2Int[] river in RiverSets(constr))
                for (int i = 0; i < river.Length; i++)
                {
                    if (river[i].Equals(srcPos))
                    {
                        if (i == river.Length - 1)
                            return OutgoingEdge(srcPos, mapSize);
                        return EdgeFrom(srcPos, river[i + 1]);
                    }
                }
            return HexSide.NULL;
        }

        public static HexSide UpstreamSideOf(
            Vector2Int srcPos, int mapSize, Construct constr)
        {
            foreach (Vector2Int[] river in RiverSets(constr))
                for (int i = 0; i < river.Length; i++)
                {
                    if (river[i].Equals(srcPos))
                    {
                        if (i == 0) return OutgoingEdge(srcPos, mapSize);
                        return EdgeFrom(srcPos, river[i - 1]);
                    }
                }
            return HexSide.NULL;
        }

        private static HexSide OutgoingEdge(Vector2Int srcPos, int mapSize)
        {
            int x = srcPos.x;
            int y = srcPos.y;
            int n = mapSize - 1;
            if (x == 0 || y == 0)
            {
                if (x == 0 && y == 0) return HexSide.S;
                if (x == 0) return HexSide.SE;
                if (y == 0) return HexSide.SW;
            }
            else if (x == n || y == n)
            {
                if (x == n && y == n) return HexSide.N;
                if (x == n) return HexSide.NW;
                if (y == n) return HexSide.NE;
            }
            else
            {
                if (x - y == n / 2) return HexSide.SW;
                if (y - x == n / 2) return HexSide.SE;
            }
            return HexSide.NULL;
        }

        private static HexSide EdgeFrom(Vector2Int src, Vector2Int dst)
        {
            int dx = dst.x - src.x;
            int dy = dst.y - src.y;
            if (dx == dy)
            {
                if (dx > 0) return HexSide.N;
                if (dx < 0) return HexSide.S;
            }
            else if (dx == 0)
            {
                if (dy > 0) return HexSide.NE;
                if (dy < 0) return HexSide.SW;
            }
            else if (dy == 0)
            {
                if (dx > 0) return HexSide.NW;
                if (dx < 0) return HexSide.SE;
            }
            throw new Exception("No hex side direction between src and dest");
        }
    }


    public static class Geometry
    {
        // Equivalent hex mesh positions of a river node
        // width is the true (in-game) dimension between two opposite corners
        // output vector has (0,0) in the bottom-left corner vertex of the hex
        public static Vector2 EquivHexPos(HexSide side, float width)
        {
            float w = width;
            float w8 = w / 8f;
            float sq3 = (float)Math.Sqrt(3.0f);
            float h = sq3 * w / 2f;
            float xOff = w / 4f;
            return side switch
            {
                HexSide.N => new(w / 2f - xOff, h),
                HexSide.NW => new(w8 - xOff, h / 4f * 3f),
                HexSide.NE => new(w - w8 - xOff, h / 4f * 3f),
                HexSide.S => new(w / 2f - xOff, 0f),
                HexSide.SE => new(w - w8 - xOff, h / 4f),
                HexSide.SW => new(w8 - xOff, h / 4f),
                _ => new(w / 2f - xOff, h / 2f)
            };
        }

        // Distance of {x0,y0} from the line going through p,q
        // Shamelessly stolen from StackOverflow
        public static float DistanceToRiver(
            float px, float py, float qx, float qy,
            float x0, float y0)
        {
            float A = x0 - px;
            float B = y0 - py;
            float C = qx - px;
            float D = qy - py;

            float dot = A * C + B * D;
            float len_sq = C * C + D * D;
            float param = -1;
            if (len_sq != 0) param = dot / len_sq;

            float xx, yy;

            if (param < 0) { xx = px; yy = py; }
            else if (param > 1) { xx = qx; yy = qy; }
            else { xx = px + param * C; yy = py + param * D; }

            float dx = x0 - xx;
            float dy = y0 - yy;
            return (float)Math.Sqrt(dx * dx + dy * dy);
        }
    }


    public enum HexSide
    {
        // The negative of a direction is its opposite
        // i.e. negative north is south 
        N = 1,
        NW = 2,
        NE = 3,
        S = -1,
        SE = -2,
        SW = -3,
        NULL = 0
    }

}
