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
        public ItemPos Src { get; set; }
        public ItemPos Dest { get; set; }
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

    public struct FallDownPos
    {
        public Point SrcPos { get; }
        public Point DestPos { get; }

        public FallDownPos(Point src, Point dest)
        {
            SrcPos = src;
            DestPos = dest;
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
