using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UIElements;
using static WorldUtil.Constructs;

namespace WorldUtil
{

    public class World
    {
        // An unordered list of all hexes in the world
        public readonly List<Hex> all;

        // A list of rivers, which are themselves lists of hexes
        // Hexes are ordered from start to end within a river
        public readonly List<List<Hex>> rivers;

        // A list of roads, which are themselves tuples of two hexes
        // Order of the tuple is arbitrary since roads are bidirectional
        public readonly List<Tuple<Hex, Hex>> roads;

        public World(List<Hex> all, List<List<Hex>> rivers,
            List<Tuple<Hex, Hex>> roads)
        {
            this.all = all;
            this.rivers = rivers;
            this.roads = roads;
        }

        public Hex FindHexWithDam(BeaverDam dam)
        {
            foreach (Hex h in all) if (h.exitDam == dam) return h;
            throw new Exception("Dam not found in world");
        }

        public List<Hex> FindRiverWithHex(Hex hex)
        {
            foreach (List<Hex> river in rivers)
                if (river.Contains(hex)) return river;
            throw new Exception("Hex not along any river");
        }

        // Hexes are ordered inverse to river direction
        public List<Hex> UpstreamFrom(Hex target)
        {
            List<Hex> r = FindRiverWithHex(target);
            int hPos = r.IndexOf(target);
            if (hPos == 0) return new();
            r = r.GetRange(0, hPos);
            r.Reverse();
            return r;
        }

        // Hexes are ordered according to river direction
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

        private int _waterLevel = 0;

        public Hex(Vector2Int mapPosition,
            GameObject landMesh, GameObject waterMesh, BeaverDam exitDam)
        {
            this.mapPosition = mapPosition;
            this.landMesh = landMesh;
            this.waterMesh = waterMesh;
            this.exitDam = exitDam;
        }

        public bool HasDam() => exitDam != null;

        public int WaterLevel() => _waterLevel;

        public bool SetWaterLevel(int level)
        {
            if (level > BeaverDam.MAX_LVL) return false;
            if (level < 0) return false;
            else _waterLevel = level; return true;
        }
    }


    public static class Constructs
    {
        public enum Construct
        {
            EMPTY, // 7-size hexagon with no rivers/roads
            HEX7_RIVER6, // 7-size hexagon with 6 rivers and 12 roads
            HEX9_RIVER6 // 9-size hexagon with outer mountains
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

        private readonly static Vector2Int[][] EMPTY_RIVERS = { };
        private readonly static Vector2Int[][] EMPTY_ROADS = { };

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

        private readonly static Vector2Int[][] HEX9_RIVER6_RIVERS =
        {
            // River 0
            VecsOf(0, 0, 1, 1, 2, 1, 2, 2, 3, 3, 2, 3, 1, 2, 0, 1), 
            // River 1
            VecsOf(0, 4, 1, 4, 1, 3, 2, 4, 3, 4, 3, 5, 2, 5, 1, 5),
            // River 2
            VecsOf(4, 8, 4, 7, 3, 6, 4, 6, 4, 5, 5, 6, 5, 7, 5, 8),
            // River 3
            VecsOf(8, 8, 7, 7, 6, 7, 6, 6, 5, 5, 6, 5, 7, 6, 8, 7),
            // River 4
            VecsOf(8, 4, 7, 4, 7, 5, 6, 4, 5, 4, 5, 3, 6, 3, 7, 3),
            // River 5
            VecsOf(4, 0, 4, 1, 5, 2, 4, 2, 4, 3, 3, 2, 3, 1, 3, 0)
        };
        private readonly static Vector2Int[][] HEX9_RIVER6_ROADS =
        {
            // Roads from R0-1
            VecsOf(1, 2, 1, 3), VecsOf(3, 3, 3, 4),
            // Roads from R1-2
            VecsOf(2, 5, 3, 6), VecsOf(3, 4, 4, 5),
            // Roads from R2-3
            VecsOf(5, 7, 6, 7), VecsOf(4, 5, 5, 5),
            // Roads from R3-4
            VecsOf(7, 6, 7, 5), VecsOf(5, 5, 5, 4),
            // Roads from R4-5
            VecsOf(6, 3, 5, 2), VecsOf(5, 4, 4, 3),
            // Roads from R5-0
            VecsOf(3, 1, 2, 1), VecsOf(4, 3, 3, 3),
        };

        public static Vector2Int[][] RiverSets(Construct constr)
        => constr switch
        {
            Construct.EMPTY => EMPTY_RIVERS,
            Construct.HEX7_RIVER6 => HEX7_RIVER6_RIVERS,
            Construct.HEX9_RIVER6 => HEX9_RIVER6_RIVERS,
            _ => throw new NotImplementedException()
        };

        public static Vector2Int[][] RoadSets(Construct constr)
        => constr switch
        {
            Construct.EMPTY => EMPTY_ROADS,
            Construct.HEX7_RIVER6 => HEX7_RIVER6_ROADS,
            Construct.HEX9_RIVER6 => HEX9_RIVER6_ROADS,
            _ => throw new NotImplementedException()
        };
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
        public static float DistFromLine(
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

        // Distance of a point from any of a series of lines
        // Each of these lines goes from an edge point to the center point
        public static float DistFromAny(
            Vector2 queryPoint, Vector2 center, params Vector2[] edges)
        {
            float[] lookup = new float[edges.Length];
            for (int i = 0; i < edges.Length; i++)
            {
                lookup[i] = DistFromLine(
                    center.x, center.y, edges[i].x, edges[i].y,
                    queryPoint.x, queryPoint.y);
            }
            float min = lookup[0];
            foreach (float f in lookup) if (f < min) min = f;
            return min;
        }

        public static float AngleToAlign(HexSide side)
        {
            return side switch
            {
                HexSide.NW => 120f,
                HexSide.SE => -60f,
                HexSide.NE => 60f,
                HexSide.SW => -120f,
                HexSide.S => 180f,
                _ => 0f
            };
        }

        public static float Lerp(float a, float b, float t)
            => a + (b - a) * t;

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

        public static HexSide[] RoadSidesOf(
            Vector2Int srcPos, Construct constr)
        {
            List<HexSide> all = new();
            foreach (Vector2Int[] road in RoadSets(constr))
            {
                if (road[0].Equals(srcPos))
                    all.Add(EdgeFrom(srcPos, road[1]));
                else if (road[1].Equals(srcPos))
                    all.Add(EdgeFrom(srcPos, road[0]));
            }
            return all.ToArray();
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
