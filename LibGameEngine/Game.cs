using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

// ReSharper disable ArrangeAccessorOwnerBody
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable MemberCanBePrivate.Global

namespace GameEngine
{
    public enum ItemShape
    {
        Circle,
        Rect
    }

    public interface IAction
    {
    }

    public class SwapAction : IAction
    {
        public Point SrcPos { get; set; }
        public Item SrcItem { get; set; }

        public Point DestPos { get; set; }
        public Item DestItem { get; set; }
    }

    public class DestroyAction : IAction
    {
        public Point[] Positions { get; set; }
    }

    public class FallDownPos
    {
        public Point SrcPos { get; }
        public Point DestPos { get; }

        public FallDownPos(Point src, Point dest)
        {
            SrcPos = src;
            DestPos = dest;
        }
        
        protected bool Equals(FallDownPos other)
        {
            return SrcPos.Equals(other.SrcPos) && DestPos.Equals(other.DestPos);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((FallDownPos) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (SrcPos.GetHashCode() * 397) ^ DestPos.GetHashCode();
            }
        }
    }

    public class FallDownAction : IAction
    {
        public FallDownPos[] Positions { get; set; }
    }

    public class ItemType
    {
        public int Color { get; set; }
        public ItemShape Shape { get; set; }

        public string Dump()
        {
            switch (Shape)
            {
                case ItemShape.Circle:
                    return "(" + Color + ")";
                case ItemShape.Rect:
                    return "[" + Color + "]";
                default:
                    return "nil";
            }
        }

        public static bool AreEquals(ItemType t1, ItemType t2, ItemType t3)
        {
            return t1 != null && t2 != null && t3 != null
                   && t1.Shape == t2.Shape && t1.Color == t2.Color
                   && t1.Shape == t3.Shape && t1.Color == t3.Color;
        }
    }

    public class Item
    {
        public ItemType ItemType { get; set; }

        public Item()
        {
            //
        }

        public Item(int color, ItemShape shape)
        {
            ItemType = new ItemType
            {
                Color = color,
                Shape = shape,
            };
        }

        public string Dump()
        {
            return ItemType.Dump();
        }
    }

    public class Board
    {
        public const int DefaultWidth = 8;

        public const int DefaultHeight = 8;

        private static readonly ItemType[] ItemTypes =
        {
            new ItemType {Color = 1, Shape = ItemShape.Circle},
            new ItemType {Color = 2, Shape = ItemShape.Circle},
            new ItemType {Color = 3, Shape = ItemShape.Circle},
            new ItemType {Color = 1, Shape = ItemShape.Rect},
            new ItemType {Color = 2, Shape = ItemShape.Rect},
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

        public Item CreateRandomItem()
        {
            return new Item
            {
                ItemType = ItemTypes[_rnd.Next(ItemTypes.Length)],
            };
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
            ItemType type00 = Items[x, y]?.ItemType;
            ItemType type10 = Items[x + 1, y]?.ItemType;
            ItemType type20 = Items[x + 2, y]?.ItemType;

            var matches = new List<Point>();
            if (ItemType.AreEquals(type00, type10, type20))
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
            ItemType type00 = Items[x, y]?.ItemType;
            ItemType type01 = Items[x, y + 1]?.ItemType;
            ItemType type02 = Items[x, y + 2]?.ItemType;

            var matches = new List<Point>();
            if (ItemType.AreEquals(type00, type01, type02))
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

    public class Game
    {
        public const int GameDurationSec = 60;

        private readonly Board _board;

        private int _timeLeftSec;

        public Game(Board board)
        {
            _board = board;
            _timeLeftSec = GameDurationSec;
        }

        public int TimeLeftSec => _timeLeftSec;

        public int Scores { get; private set; }

        public bool IsGameOver => _timeLeftSec < 0;

        public Item[,] Items => _board.Items;

        public int BoardWidth
        {
            get { return _board.Width; }
        }

        public int BoardHeight
        {
            get { return _board.Height; }
        }

        public void Reset()
        {
            Scores = 0;
            _board.Reset();
        }

        public void Tick()
        {
            _timeLeftSec--;
        }

        public string Dump()
        {
            return "Score: " + Scores + Environment.NewLine
                   + "Board: " + Environment.NewLine
                   + _board.Dump();
        }

        public bool CanSwap(Point src, Point dest)
        {
            return CanSwap(src.X, src.Y, dest.X, dest.Y);
        }

        public bool CanSwap(int srcX, int srcY, int destX, int destY)
        {
            // Can swap horizontal/vertical neighbours only
            return (0 <= srcX && srcX < _board.Width)
                   && (0 <= destX && destX < _board.Width)
                   && (0 <= srcY && destY < _board.Height)
                   && (0 <= destY && destY < _board.Height)
                   && ((Math.Abs(srcX - destX) == 1 && srcY == destY)
                       || (Math.Abs(srcY - destY) == 1 && srcX == destX));
        }

        // ReSharper disable once ReturnTypeCanBeEnumerable.Global
        public IAction[] Swap(Point src, Point dest)
        {
            var actions = new List<IAction>
            {
                new SwapAction
                {
                    SrcPos = src,
                    SrcItem = Items[src.X, src.Y],
                    DestPos = dest,
                    DestItem = Items[dest.X, dest.Y],
                }
            };

            _board.Swap(src, dest);

            HashSet<Point> matches = _board.CalcMatches();

            if (matches.Count == 0)
            {
                _board.Swap(src, dest); // swap back

                actions.Add(new SwapAction
                {
                    SrcPos = src,
                    SrcItem = Items[src.X, src.Y],
                    DestPos = dest,
                    DestItem = Items[dest.X, dest.Y],
                });
            }
            else
            {
                actions.Add(new DestroyAction
                {
                    Positions = matches.ToArray()
                });
                foreach (Point m in matches)
                {
                    Items[m.X, m.Y] = null;
                }

                List<FallDownPos> fallDown = _board.CalcFallDownPositions();
                if (fallDown.Count > 0)
                {
                    actions.Add(new FallDownAction
                    {
                        Positions = fallDown.ToArray()
                    });
                }
            }

            return actions.ToArray();
        }
    }
}
