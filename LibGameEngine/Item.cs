namespace GameEngine
{
    public enum ItemShape
    {
        Circle,
        Rect
    }

    public class Item
    {
        public int Color { get; }
        public ItemShape Shape { get; }
        public int Score { get; }

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
                case ItemShape.Circle:
                    return "(" + Color + ")";
                case ItemShape.Rect:
                    return "[" + Color + "]";
                default:
                    return "nil";
            }
        }

        public static bool AreEquals(Item i1, Item i2, Item i3)
        {
            return i1 != null && i2 != null && i3 != null
                   && i1.Shape == i2.Shape && i1.Color == i2.Color
                   && i1.Shape == i3.Shape && i1.Color == i3.Color;
        }
    }
}
