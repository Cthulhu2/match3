using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

// ReSharper disable SuggestBaseTypeForParameter
// ReSharper disable InvertIf
// ReSharper disable ArrangeAccessorOwnerBody

namespace GameEngine
{
    public class MatchRes
    {
        public List<Point[]> MatchLines { get; set; }
        public List<Tuple<Point[], Point[]>> MatchCrosses { get; set; }

        public bool IsEmpty
        {
            get { return MatchLines.Count == 0 && MatchCrosses.Count == 0; }
        }
    }

    public class Board
    {
        private const int DefaultWidth = 8;

        private const int DefaultHeight = 8;

        private static readonly Item[] ItemTemplates =
        {
            new Item(1, ItemShape.Ball),
            new Item(2, ItemShape.Ball),
            new Item(3, ItemShape.Ball),
            new Item(1, ItemShape.Cube),
            new Item(2, ItemShape.Cube),
        };

        private readonly Random _rnd;

        public Item[,] Items { get; }

        public Board()
            : this(DefaultWidth, DefaultHeight)
        {
            //
        }

        private Board(int width, int height)
            : this(new Item[width, height])
        {
        }

        public Board(Item[,] items)
        {
            Items = items;
            _rnd = new Random();
        }

        private Item CreateRandomItem()
        {
            Item template = ItemTemplates[_rnd.Next(ItemTemplates.Length)];

            return new Item(template.Color, template.Shape, template.Score);
        }

        public void Reset()
        {
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    Items[x, y] = null;
                }
            }

            SpawnItems();
            // Remove all existed matches
            List<Point> matches;
            do
            {
                matches = CalcMatchesInitial().ToList();
                foreach (Point m in matches)
                {
                    Items[m.X, m.Y] = null;
                }

                CalcFallDownPositions();
                SpawnItems();
            } while (matches.Any());
        }

        public List<SpawnPos> SpawnItems()
        {
            var spawnPos = new List<SpawnPos>();
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    if (Items[x, y] == null)
                    {
                        Item item = CreateRandomItem();
                        Items[x, y] = item;
                        spawnPos.Add(new SpawnPos(new Point(x, y), item));
                    }
                }
            }

            return spawnPos;
        }

        public string Dump()
        {
            var sb = new StringBuilder();

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    Item item = Items[x, y];
                    if (item != null)
                    {
                        sb.Append(item.Dump()).Append(" ");
                    }
                    else
                    {
                        sb.Append("   ").Append(" ");
                    }
                }

                sb.Append(Environment.NewLine);
            }

            return sb.ToString();
        }

        public void Swap(Point src, Point dest)
        {
            Item temp = Items[src.X, src.Y];
            Items[src.X, src.Y] = Items[dest.X, dest.Y];
            Items[dest.X, dest.Y] = temp;
        }

        private static Point[] GetHPosFrom(int x, int y, int count)
        {
            var pos = new Point[count];
            for (int i = 0; i < count; i++)
            {
                pos[i] = new Point(x + i, y);
            }

            return pos;
        }

        private static Point[] GetVPosFrom(int x, int y, int count)
        {
            var pos = new Point[count];
            for (int i = 0; i < count; i++)
            {
                pos[i] = new Point(x, y + i);
            }

            return pos;
        }

        private bool TestMatchLine(Point[] positions)
        {
            bool vertical = (positions[0].X == positions[1].X);

            var firstRegularShape = ItemShape.None;
            for (int i = 0; i < positions.Length - 1; i++)
            {
                Point pos = positions[i];

                Item it = Items[pos.X, pos.Y];
                if (it.IsRegularShape)
                {
                    firstRegularShape = it.Shape;
                    break;
                }
            }
            
            for (int i = 0; i < positions.Length - 1; i++)
            {
                Point pos1 = positions[i];
                Point pos2 = positions[i + 1];

                Item it1 = Items[pos1.X, pos1.Y];
                Item it2 = Items[pos2.X, pos2.Y];

                if (it1.Color != it2.Color)
                {
                    return false;
                }

                if (vertical && (it1.Shape == ItemShape.HLine
                                 || it2.Shape == ItemShape.HLine))
                {
                    return false;
                }

                if (!vertical && (it1.Shape == ItemShape.VLine
                                  || it2.Shape == ItemShape.VLine))
                {
                    return false;
                }

                if ((it1.IsRegularShape && it1.Shape != firstRegularShape)
                    || (it2.IsRegularShape && it2.Shape != firstRegularShape))
                {
                    return false;
                }
            }

            return true;
        }

        private List<Point> TestMatch3H(int x, int y)
        {
            Item type00 = Items[x, y];
            Item type10 = Items[x + 1, y];
            Item type20 = Items[x + 2, y];

            var matches = new List<Point>();
            if (Item.AreEquals(type00, type10, type20))
            {
                matches.AddRange(new[]
                {
                    new Point(x, y),
                    new Point(x + 1, y),
                    new Point(x + 2, y),
                });
            }

            return matches;
        }

        private List<Point> TestMatch3V(int x, int y)
        {
            Item type00 = Items[x, y];
            Item type01 = Items[x, y + 1];
            Item type02 = Items[x, y + 2];

            var matches = new List<Point>();
            if (Item.AreEquals(type00, type01, type02))
            {
                matches.AddRange(new[]
                {
                    new Point(x, y),
                    new Point(x, y + 1),
                    new Point(x, y + 2),
                });
            }

            return matches;
        }

        private List<Point[]> FindMatchLines(int count)
        {
            // ReSharper disable once TooWideLocalVariableScope
            Point[] test;
            var matchedLines = new List<Point[]>();
            
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    if (x < Width - (count - 1))
                    {
                        test = GetHPosFrom(x, y, count);
                        if (TestMatchLine(test))
                        {
                            matchedLines.Add(test);
                        }
                    }

                    if (y < Height - (count - 1))
                    {
                        test = GetVPosFrom(x, y, count);
                        if (TestMatchLine(test))
                        {
                            matchedLines.Add(test);
                        }
                    }
                }
            }

            return matchedLines;
        }
        
        public MatchRes CalcMatches()
        {
            // Match unique lines 5, 4, 3
            var matchLines = new List<Point[]>();

            matchLines.AddRange(FindMatchLines(5));
            
            IEnumerable<Point[]> match4 = FindMatchLines(4)
                .Where(m4 => !matchLines.Any(m => m.Intersect(m4).Count() > 1));
            
            matchLines.AddRange(match4);

            IEnumerable<Point[]> match3 = FindMatchLines(3)
                .Where(m3 => !matchLines.Any(m => m.Intersect(m3).Count() > 1));

            matchLines.AddRange(match3);
            // Extract cross matches
            var matchCross = new List<Tuple<Point[], Point[]>>();
            for (int i = 0; i < matchLines.Count - 1; i++)
            {
                for (int j = i; j < matchLines.Count; j++)
                {
                    Point[] l1 = matchLines[i];
                    Point[] l2 = matchLines[j];
                    if (l1.Intersect(l2).Count() == 1)
                    {
                        matchCross.Add(new Tuple<Point[], Point[]>(l1, l2));
                    }
                }
            }
            matchCross.ForEach(cross =>
            {
                (Point[] line1, Point[] line2) = cross;
                matchLines.Remove(line1);
                matchLines.Remove(line2);
            });
            //
            return new MatchRes
            {
                MatchLines = matchLines,
                MatchCrosses = matchCross,
            };
        }

        public IEnumerable<Point> CalcMatchesInitial()
        {
            var matches = new HashSet<Point>();

            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    if (x < Width - 2)
                    {
                        List<Point> match3 = TestMatch3H(x, y);
                        if (match3.Count > 0)
                        {
                            match3.ForEach(p => matches.Add(p));
                        }
                    }

                    if (y < Height - 2)
                    {
                        List<Point> match3 = TestMatch3V(x, y);
                        if (match3.Count > 0)
                        {
                            match3.ForEach(p => matches.Add(p));
                        }
                    }
                }
            }

            return matches;
        }

        private Point FindUpperFirst(int x, int y)
        {
            for (int testY = y; testY >= 0; testY--)
            {
                if (Items[x, testY] != null)
                {
                    return new Point(x, testY);
                }
            }

            return new Point(-1, -1);
        }

        public List<FallDownPos> CalcFallDownPositions()
        {
            var positions = new List<FallDownPos>();

            // From Down to Up
            for (int y = Height - 1; y > 0; y--)
            {
                for (int x = 0; x < Width; x++)
                {
                    if (Items[x, y] == null)
                    {
                        var emptyPos = new Point(x, y);

                        Point upPos = FindUpperFirst(x, y);
                        if (upPos != new Point(-1, -1))
                        {
                            Swap(upPos, emptyPos);
                            positions.Add(new FallDownPos(upPos, emptyPos));
                        }
                    }
                }
            }

            return positions;
        }

        public int Width
        {
            get { return Items.GetLength(0); }
        }

        public int Height
        {
            get { return Items.GetLength(1); }
        }
    }
}
