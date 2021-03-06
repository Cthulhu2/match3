﻿// ReSharper disable ArrangeAccessorOwnerBody

namespace GameEngine
{
    public enum ItemShape
    {
        None,
        Ball,
        Cube,
        HLine,
        VLine,
        Bomb,
    }

    public class Item
    {
        public int Color { get; }
        public ItemShape Shape { get; }
        public int Score { get; }

        public bool IsRegularShape
        {
            get { return Shape == ItemShape.Ball || Shape == ItemShape.Cube; }
        }

        public bool IsBombShape
        {
            get { return Shape == ItemShape.Bomb; }
        }

        public bool IsLineShape
        {
            get { return Shape == ItemShape.HLine || Shape == ItemShape.VLine; }
        }

        public Item(int color, ItemShape shape, int score = 20)
        {
            Color = color;
            Shape = shape;
            Score = score;
        }

        public string Dump()
        {
            switch (Shape)
            {
                case ItemShape.Ball:
                    return "(" + Color + ")";
                case ItemShape.Cube:
                    return "[" + Color + "]";
                case ItemShape.Bomb:
                    return "B" + Color + "B";
                case ItemShape.HLine:
                    return "-" + Color + "-";
                case ItemShape.VLine:
                    return "|" + Color + "|";
                default:
                    return "nil";
            }
        }
    }
}
