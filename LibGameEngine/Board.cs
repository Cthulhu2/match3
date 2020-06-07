using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace GameEngine
{
    public class Board
    {
        public const int DefaultWidth = 8;

        public const int DefaultHeight = 8;

        private static readonly Item[] ItemTemplates =
        {
            new Item(1, ItemShape.Circle),
            new Item(2, ItemShape.Circle),
            new Item(3, ItemShape.Circle),
            new Item(1, ItemShape.Rect),
            new Item(2, ItemShape.Rect),
        };

        private readonly Random _rnd;

        public Item[,] Items { get; }

        public Board()
            : this(DefaultWidth, DefaultHeight)
        {
            //
        }

        public Board(int width, int height)
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
                    Items[x, y] = CreateRandomItem();
                }
            }
        }

        public void ColFallOneDown(Point p)
        {
            for (int y = p.Y; y > 0; y--)
            {
                Items[p.X, y] = Items[p.X, y - 1];
            }

            Items[p.X, 0] = CreateRandomItem();
        }

        public Item RemoveAt(Point p)
        {
            Item item = Items[p.X, p.Y];

            Items[p.X, p.Y] = null;

            return item;
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

        public List<Point> TestMatch3H(int x, int y)
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

        public List<Point> TestMatch3V(int x, int y)
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

        public HashSet<Point> CalcMatches()
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
