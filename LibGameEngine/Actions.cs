using System.Drawing;

namespace GameEngine
{
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
        
        private bool Equals(FallDownPos other)
        {
            return SrcPos.Equals(other.SrcPos) && DestPos.Equals(other.DestPos);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)){ return false;}
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
}
