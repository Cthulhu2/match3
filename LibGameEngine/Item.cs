// ReSharper disable ArrangeAccessorOwnerBody

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

        public static bool AreEquals(Item i1, Item i2, Item i3)
        {
            return i1 != null && i2 != null && i3 != null
                   && i1.Shape == i2.Shape && i1.Color == i2.Color
                   && i1.Shape == i3.Shape && i1.Color == i3.Color;
        }

        private bool Equals(Item other)
        {
            return Color == other.Color
                   && Shape == other.Shape
                   && Score == other.Score;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Item) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Color;
                hashCode = (hashCode * 397) ^ (int) Shape;
                hashCode = (hashCode * 397) ^ Score;
                return hashCode;
            }
        }
    }
}
