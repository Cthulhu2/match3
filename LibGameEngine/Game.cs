using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

// ReSharper disable ArrangeAccessorOwnerBody

namespace GameEngine
{
    public class Game
    {
        public const int GameDurationSec = 60;

        private readonly Board _board;

        public int TimeLeftSec { get; private set; }

        public int Scores { get; private set; }

        public bool IsOver
        {
            get { return TimeLeftSec < 0; }
        }

        public Item[,] Items
        {
            get { return _board.Items; }
        }

        public int BoardWidth
        {
            get { return _board.Width; }
        }

        public int BoardHeight
        {
            get { return _board.Height; }
        }

        public Game(Board board)
        {
            _board = board;
            TimeLeftSec = GameDurationSec;
        }

        public void Reset()
        {
            Scores = 0;
            _board.Reset();
        }

        public void Tick()
        {
            TimeLeftSec--;
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
            // ReSharper disable ArrangeRedundantParentheses
            return (0 <= srcX && srcX < _board.Width)
                   && (0 <= destX && destX < _board.Width)
                   && (0 <= srcY && destY < _board.Height)
                   && (0 <= destY && destY < _board.Height)
                   && ((Math.Abs(srcX - destX) == 1 && srcY == destY)
                       || (Math.Abs(srcY - destY) == 1 && srcX == destX));
            // ReSharper restore ArrangeRedundantParentheses
        }

        // ReSharper disable once ReturnTypeCanBeEnumerable.Global
        public IAction[] Swap(Point src, Point dest)
        {
            var actions = new List<IAction>
            {
                new SwapAction
                {
                    Src = new ItemPos(src, Items[src.X, src.Y]),
                    Dest = new ItemPos(dest, Items[dest.X, dest.Y]),
                }
            };
            _board.Swap(src, dest);

            MatchRes match = _board.CalcMatches();

            if (match.IsEmpty)
            {
                actions.Add(new SwapAction
                {
                    Src = new ItemPos(src, Items[src.X, src.Y]),
                    Dest = new ItemPos(dest, Items[dest.X, dest.Y]),
                });
                _board.Swap(src, dest); // swap back
            }
            else
            {
                while (!match.IsEmpty)
                {
                    IEnumerable<IAction> processed =
                        ProcessMatch(src, dest, match);

                    actions.AddRange(processed);

                    match = _board.CalcMatches();
                }
            }

            return actions.ToArray();
        }

        private IEnumerable<Point> GetLine(Point linePos, ItemShape shape)
        {
            var pos = new List<Point>();

            if (shape == ItemShape.HLine)
            {
                for (int x = 0; x < BoardWidth; x++)
                {
                    pos.Add(new Point(x, linePos.Y));
                }
            }
            else if (shape == ItemShape.VLine)
            {
                for (int y = 0; y < BoardHeight; y++)
                {
                    pos.Add(new Point(linePos.X, y));
                }
            }

            return pos;
        }

        private IEnumerable<Point> GetBombNeighbour(Point bombPos)
        {
            var pos = new List<Point>
            {
                new Point(bombPos.X - 1, bombPos.Y - 1),
                new Point(bombPos.X, bombPos.Y - 1),
                new Point(bombPos.X + 1, bombPos.Y - 1),
                new Point(bombPos.X - 1, bombPos.Y),
                new Point(bombPos.X + 1, bombPos.Y),
                new Point(bombPos.X - 1, bombPos.Y + 1),
                new Point(bombPos.X, bombPos.Y + 1),
                new Point(bombPos.X + 1, bombPos.Y + 1)
            };
            // ReSharper disable ArrangeRedundantParentheses
            pos.RemoveAll(p => (p.X < 0 || BoardWidth <= p.X)
                               || (p.Y < 0 || BoardHeight <= p.Y));
            // ReSharper restore ArrangeRedundantParentheses
            return pos;
        }

        private static Point TargetBonusPos(Point src, Point dest, Point[] line)
        {
            Point targetPos;
            if (line.Contains(src))
            {
                targetPos = src;
            }
            else if (line.Contains(dest))
            {
                targetPos = dest;
            }
            else
            {
                targetPos = line[2];
            }

            return targetPos;
        }

        private ItemPos CalcBonusSpawn(Point src, Point dest, Point[] line)
        {
            if (line.Length == 5)
            {
                Point targetPos = TargetBonusPos(src, dest, line);

                int color = Items[targetPos.X, targetPos.Y].Color;
                return new ItemPos(targetPos, new Item(color, ItemShape.Bomb));
            }

            if (line.Length == 4)
            {
                Point targetPos = TargetBonusPos(src, dest, line);

                int color = Items[targetPos.X, targetPos.Y].Color;
                bool vertical = (line[0].X == line[1].X);
                ItemShape shape = vertical ? ItemShape.VLine : ItemShape.HLine;

                return new ItemPos(targetPos, new Item(color, shape));
            }

            return null;
        }

        private ItemPos CalcBonusSpawn(Point src,
                                       Point dest,
                                       Tuple<Point[], Point[]> cross)
        {
            Point targetPos;
            (Point[] line1, Point[] line2) = cross;

            if (line1.Contains(src) || line2.Contains(src))
            {
                targetPos = src;
            }
            else if (line1.Contains(dest) || line2.Contains(dest))
            {
                targetPos = dest;
            }
            else
            {
                targetPos = line1.Intersect(line2).First();
            }

            int color = Items[targetPos.X, targetPos.Y].Color;
            return new ItemPos(targetPos, new Item(color, ItemShape.Bomb));
        }

        private void CollectBonusDestroy(
            ItemPos item,
            IDictionary<ItemPos, ItemPos[]> destroyedBy,
            ISet<Point> except)
        {
            var destroyed = new List<Point>();
            if (item.Item.IsBombShape)
            {
                destroyed = GetBombNeighbour(item.Pos)
                    .Except(except)
                    .ToList();
            }
            else if (item.Item.IsLineShape)
            {
                destroyed = GetLine(item.Pos, item.Item.Shape)
                    .Except(except)
                    .ToList();
            }

            except.UnionWith(destroyed); // no more destroying for this

            ItemPos[] destroyedPositions = destroyed
                .Select(p => new ItemPos(p, Items[p.X, p.Y]))
                .ToArray();

            destroyedBy[item] = destroyedPositions;
            // Recursively bonus destroyed
            foreach (ItemPos pos in destroyedPositions)
            {
                if (!pos.Item.IsRegularShape)
                {
                    CollectBonusDestroy(pos, destroyedBy, except);
                }
            }
        }

        private IEnumerable<IAction> ProcessMatch(Point src,
                                                  Point dest,
                                                  MatchRes match)
        {
            var actions = new List<IAction>();
            //
            var regularDestroy = new HashSet<Point>();
            match.MatchLines.ForEach(l => regularDestroy.UnionWith(l));
            match.MatchCrosses.ForEach(cr =>
            {
                (Point[] line1, Point[] line2) = cr;
                regularDestroy.UnionWith(line1);
                regularDestroy.UnionWith(line2);
            });
            ItemPos[] regularDestroyPos = regularDestroy
                .Select(p => new ItemPos(p, Items[p.X, p.Y]))
                .ToArray();
            //
            var bonuses = new List<ItemPos>();
            match.MatchLines.ForEach(line =>
            {
                ItemPos bonus = CalcBonusSpawn(src, dest, line);
                if (bonus != null && bonuses.All(b => b.Pos != bonus.Pos))
                {
                    bonuses.Add(bonus);
                }
            });
            match.MatchCrosses.ForEach(cross =>
            {
                ItemPos bonus = CalcBonusSpawn(src, dest, cross);
                if (bonus != null && bonuses.All(b => b.Pos != bonus.Pos))
                {
                    bonuses.Add(bonus);
                }
            });

            var except = new HashSet<Point>(regularDestroy); // New modified set
            var destroyedBy = new Dictionary<ItemPos, ItemPos[]>();
            regularDestroyPos
                .Where(p => !p.Item.IsRegularShape).ToList()
                .ForEach(p => CollectBonusDestroy(p, destroyedBy, except));
            //
            UpdateBoardOnDestroy(regularDestroyPos, destroyedBy, bonuses);
            //
            actions.Add(new DestroyAction
            {
                MatchDestroyedPos = regularDestroyPos,
                SpawnBonuses = bonuses.ToArray(),
                DestroyedBy = destroyedBy,
            });

            FallDownPos[] fallen = _board.CalcFallDownPositions().ToArray();
            actions.Add(new FallDownAction {Positions = fallen});

            ItemPos[] spawned = _board.SpawnItems().ToArray();
            actions.Add(new SpawnAction {Positions = spawned});

            return actions;
        }

        private void UpdateBoardOnDestroy(
            IEnumerable<ItemPos> regularDestroyItemPos,
            Dictionary<ItemPos, ItemPos[]> destroyedBy,
            List<ItemPos> bonuses)
        {
            var allDestroyed = new List<ItemPos>(regularDestroyItemPos);
            foreach (ItemPos[] dBy in destroyedBy.Values)
            {
                allDestroyed.AddRange(dBy);
            }

            foreach (ItemPos m in allDestroyed)
            {
                Item item = Items[m.Pos.X, m.Pos.Y];
                Scores += item.Score;
                _board.ClearItem(m.Pos);
            }

            bonuses.ForEach(b => Items[b.Pos.X, b.Pos.Y] = b.Item);
        }

        private static Point[] PointsFrom(int x, int y, bool vertical, int size)
        {
            var points = new Point[size];
            for (int i = 0; i < size; i++)
            {
                points[i] = vertical
                    ? new Point(x, y + i)
                    : new Point(x + i, y);
            }

            return points;
        }

        public IAction[] CheatBonus(ItemShape bonus)
        {
            var rnd = new Random();

            bool vertical = rnd.Next(2) > 0;
            ItemShape shape = rnd.Next(2) > 0
                ? ItemShape.Ball
                : ItemShape.Cube;
            int color = rnd.Next(shape == ItemShape.Ball ? 3 : 2) + 1;
            int x = vertical
                ? rnd.Next(BoardWidth)
                : rnd.Next(BoardWidth - 3);
            int y = vertical
                ? rnd.Next(BoardHeight - 3)
                : rnd.Next(BoardHeight);

            Point[] place = PointsFrom(x, y, vertical, 3);
            // Needs to avoid NullReferenceException while destroy old bonuses
            var destroyedBy = new Dictionary<ItemPos, ItemPos[]>();
            List<ItemPos> oldBonuses = place
                .Select(p => new ItemPos(p, Items[p.X, p.Y]))
                .Where(ip => ip.Item.IsBombShape || ip.Item.IsLineShape)
                .ToList();
            oldBonuses.ForEach(b => destroyedBy[b] = new ItemPos[0]);
            //
            var dAct = new DestroyAction
            {
                MatchDestroyedPos = place
                    .Select(p => new ItemPos(p, Items[p.X, p.Y]))
                    .ToArray(),
                SpawnBonuses = new ItemPos[0],
                DestroyedBy = destroyedBy,
            };

            Items[place[0].X, place[0].Y] = new Item(color, shape);
            Items[place[1].X, place[1].Y] = new Item(color, bonus);
            Items[place[2].X, place[2].Y] = new Item(color, shape);

            var spAct = new SpawnAction
            {
                Positions = place
                    .Select(p => new ItemPos(p, Items[p.X, p.Y]))
                    .ToArray(),
            };
            return new IAction[] {dAct, spAct};
        }
    }
}
