using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

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
        public ItemPos[] MatchDestroyedPos { get; set; }
        public ItemPos[] SpawnBonuses { get; set; }
        public Dictionary<ItemPos, ItemPos[]> DestroyedBy { get; set; }

        public string Dump()
        {
            var sb = new StringBuilder();
            sb.Append("Match: ");
            foreach (ItemPos pos in MatchDestroyedPos)
            {
                sb.Append(pos.Dump()).Append(", ");
            }

            sb.Append(Environment.NewLine)
                .Append("Bonus: ");
            foreach (ItemPos pos in SpawnBonuses)
            {
                sb.Append(pos.Dump()).Append(", ");
            }

            sb.Append(Environment.NewLine)
                .Append("Destroy: ");

            foreach (ItemPos key in DestroyedBy.Keys)
            {
                sb.Append(Environment.NewLine)
                    .Append(key.Dump()).Append(": ");
                foreach (ItemPos pos in DestroyedBy[key])
                {
                    sb.Append(pos.Dump()).Append(", ");
                }
            }

            return sb.ToString();
        }
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

    public class ItemPos
    {
        public Point Pos { get; }
        public Item Item { get; }

        public ItemPos(Point pos, Item item)
        {
            Pos = pos;
            Item = item;
        }

        private bool Equals(ItemPos other)
        {
            return Pos.Equals(other.Pos) && Equals(Item, other.Item);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ItemPos) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Pos.GetHashCode() * 397)
                       ^ (Item != null ? Item.GetHashCode() : 0);
            }
        }

        public string Dump()
        {
            return $"{Pos} {Item.Dump()}";
        }
    }

    public class SpawnAction : IAction
    {
        public ItemPos[] Positions { get; set; }
    }
}
