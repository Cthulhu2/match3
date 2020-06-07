using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

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
    
    public class ItemType
    {
        public int Color { get; internal set; }
        public ItemShape Shape { get; internal set; }

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
    }

    public class Item
    {
        public ItemType ItemType { get; internal set; }

        public string Dump()
        {
            return ItemType.Dump();
        }
    }

    public class Board
    {
        public const int Width = 8;

        public const int Height = 8;

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
        {
            Items = new Item[Width, Height];
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
                    sb.Append(Items[x, y].Dump()).Append(" ");
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

        public bool HasMatch3
        {
            get { return false; }
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

        public static bool CanSwap(Point src, Point dest)
        {
            return CanSwap(src.X, src.Y, dest.X, dest.Y);
        }
        
        public static bool CanSwap(int srcX, int srcY, int destX, int destY)
        {
            // Can swap horizontal/vertical neighbours only
            return (0 <= srcX && srcX < Board.Width)
                   && (0 <= destX && destX < Board.Width)
                   && (0 <= srcY && destY < Board.Height)
                   && (0 <= destY && destY < Board.Height)
                   && ((Math.Abs(srcX - destX) == 1 && srcY == destY)
                       || (Math.Abs(srcY - destY) == 1 && srcX == destX));
        }

        public IAction[] Swap(Point src, Point dest)
        {
            var actions = new List<IAction>();
            
            actions.Add(new SwapAction()
            {
                SrcPos = src,
                SrcItem = Items[src.X, src.Y],
                DestPos = dest,
                DestItem = Items[dest.X, dest.Y],
            });
            
            _board.Swap(src, dest);

            if (!_board.HasMatch3)
            {
                // Swap back 
                _board.Swap(src, dest);
                
                actions.Add(new SwapAction()
                {
                    SrcPos = src,
                    SrcItem = Items[src.X, src.Y],
                    DestPos = dest,
                    DestItem = Items[dest.X, dest.Y],
                });                
            }
  
            return actions.ToArray();
        }
    }
}
