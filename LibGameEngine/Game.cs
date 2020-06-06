﻿using System;
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
    }

    public class Game : IDisposable
    {
        private readonly Board _board;

        public int Scores { get; private set; }

        public Game(Board board)
        {
            _board = board;
        }

        public Item[,] Items => _board.Items;

        public void Reset()
        {
            Scores = 0;
            _board.Reset();
        }

        public void NextTurn()
        {
        }

        public void Dispose()
        {
        }

        public string Dump()
        {
            return "Score: " + Scores + Environment.NewLine
                   + "Board: " + Environment.NewLine
                   + _board.Dump();
        }
    }
}