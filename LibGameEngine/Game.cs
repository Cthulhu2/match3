using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

// ReSharper disable ArrangeAccessorOwnerBody
// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable MemberCanBePrivate.Global

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

            MatchRes match = _board.CalcMatches();

            if (match.IsEmpty)
            {
                actions.Add(new SwapAction
                {
                    SrcPos = src,
                    SrcItem = Items[src.X, src.Y],
                    DestPos = dest,
                    DestItem = Items[dest.X, dest.Y],
                });
                _board.Swap(src, dest); // swap back
            }
            else
            {
                while (!match.IsEmpty)
                {
                    List<IAction> processed = ProcessMatch(src, dest, match);

                    actions.AddRange(processed);

                    match = _board.CalcMatches();
                }
            }

            return actions.ToArray();
        }

        private Point[] GetLine(Point linePos, ItemShape shape)
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

            return pos.ToArray();
        }

        public Point[] GetBombNeighbour(Point bombPos)
        {
            var pos = new List<Point>();

            pos.Add(new Point(bombPos.X - 1, bombPos.Y - 1));
            pos.Add(new Point(bombPos.X, bombPos.Y - 1));
            pos.Add(new Point(bombPos.X + 1, bombPos.Y - 1));

            pos.Add(new Point(bombPos.X - 1, bombPos.Y));
            pos.Add(new Point(bombPos.X + 1, bombPos.Y));

            pos.Add(new Point(bombPos.X - 1, bombPos.Y + 1));
            pos.Add(new Point(bombPos.X, bombPos.Y + 1));
            pos.Add(new Point(bombPos.X + 1, bombPos.Y + 1));

            pos.RemoveAll(p => (p.X < 0 || BoardWidth <= p.X)
                               || (p.Y < 0 || BoardHeight <= p.Y));

            return pos.ToArray();
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
            Dictionary<ItemPos, ItemPos[]> destroyedBy,
            HashSet<Point> except)
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

            // no more destroying for this
            destroyed.ForEach(p => except.Add(p));

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

        // ReSharper disable once ReturnTypeCanBeEnumerable.Local
        private List<IAction> ProcessMatch(Point src, Point dest, MatchRes match)
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

            FallDownPos[] fallen = _board.CalcFallDownPositions()
                .ToArray();
            actions.Add(new FallDownAction {Positions = fallen});

            ItemPos[] spawned = _board.SpawnItems().ToArray();
            actions.Add(new SpawnAction {Positions = spawned});

            return actions;
        }

        private void UpdateBoardOnDestroy(
            ItemPos[] regularDestroyItemPos,
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
                Items[m.Pos.X, m.Pos.Y] = null;
            }

            bonuses.ForEach(b => Items[b.Pos.X, b.Pos.Y] = b.Item);
        }

        public IAction[] CheatBomb()
        {
            var rnd = new Random();
            ItemShape shape = rnd.Next(2) > 0 ? ItemShape.Ball : ItemShape.Cube;
            int color = rnd.Next(shape == ItemShape.Ball ? 3 : 2) + 1;
            int x = rnd.Next(BoardWidth - 3);
            int y = rnd.Next(BoardHeight);
            // Needs for correct destroying old bombs
            var destroyedBy = new Dictionary<ItemPos, ItemPos[]>();
            var oldBombs = new List<ItemPos>();
            for (int i = 0; i < 3; i++)
            {
                Item item = Items[x + i, y];
                if (item.IsBombShape)
                {
                    oldBombs.Add(new ItemPos(new Point(x + i, y), item));
                }
            }
            oldBombs.ForEach(b => destroyedBy[b] = new ItemPos[0]);
            //
            var dAct = new DestroyAction
            {
                MatchDestroyedPos = new[]
                {
                    new ItemPos(new Point(x, y), Items[x, y]),
                    new ItemPos(new Point(x + 1, y), Items[x + 1, y]),
                    new ItemPos(new Point(x + 2, y), Items[x + 2, y]),
                },
                SpawnBonuses = new ItemPos[0],
                DestroyedBy = destroyedBy,
            };

            Items[x, y] = new Item(color, shape);
            Items[x + 1, y] = new Item(color, ItemShape.Bomb);
            Items[x + 2, y] = new Item(color, shape);

            var spAct = new SpawnAction
            {
                Positions = new[]
                {
                    new ItemPos(new Point(x, y), Items[x, y]),
                    new ItemPos(new Point(x + 1, y), Items[x + 1, y]),
                    new ItemPos(new Point(x + 2, y), Items[x + 2, y]),
                }
            };
            return new IAction[] {dAct, spAct};
        }

        public IAction[] CheatHLine()
        {
            var rnd = new Random();
            ItemShape shape = rnd.Next(2) > 0 ? ItemShape.Ball : ItemShape.Cube;
            int color = rnd.Next(shape == ItemShape.Ball ? 3 : 2) + 1;
            int x = rnd.Next(BoardWidth - 3);
            int y = rnd.Next(BoardHeight);
            // Needs to avoid NullReferenceException while destroy old bonuses
            var destroyedBy = new Dictionary<ItemPos, ItemPos[]>();
            var oldBonuses = new List<ItemPos>();
            for (int i = 0; i < 3; i++)
            {
                Item item = Items[x + i, y];
                if (item.IsBombShape || item.IsLineShape)
                {
                    oldBonuses.Add(new ItemPos(new Point(x + i, y), item));
                }
            }
            oldBonuses.ForEach(b => destroyedBy[b] = new ItemPos[0]);
            //
            var dAct = new DestroyAction
            {
                MatchDestroyedPos = new[]
                {
                    new ItemPos(new Point(x, y), Items[x, y]),
                    new ItemPos(new Point(x + 1, y), Items[x + 1, y]),
                    new ItemPos(new Point(x + 2, y), Items[x + 2, y]),
                },
                SpawnBonuses = new ItemPos[0],
                DestroyedBy = destroyedBy,
            };

            Items[x, y] = new Item(color, shape);
            Items[x + 1, y] = new Item(color, ItemShape.HLine);
            Items[x + 2, y] = new Item(color, shape);

            var spAct = new SpawnAction
            {
                Positions = new[]
                {
                    new ItemPos(new Point(x, y), Items[x, y]),
                    new ItemPos(new Point(x + 1, y), Items[x + 1, y]),
                    new ItemPos(new Point(x + 2, y), Items[x + 2, y]),
                }
            };
            return new IAction[] {dAct, spAct};
        }
        
        public IAction[] CheatVLine()
        {
            var rnd = new Random();
            ItemShape shape = rnd.Next(2) > 0 ? ItemShape.Ball : ItemShape.Cube;
            int color = rnd.Next(shape == ItemShape.Ball ? 3 : 2) + 1;
            int x = rnd.Next(BoardWidth);
            int y = rnd.Next(BoardHeight - 3);
            // Needs to avoid NullReferenceException while destroy old bonuses
            var destroyedBy = new Dictionary<ItemPos, ItemPos[]>();
            var oldBonuses = new List<ItemPos>();
            for (int i = 0; i < 3; i++)
            {
                Item item = Items[x, y + i];
                if (item.IsBombShape || item.IsLineShape)
                {
                    oldBonuses.Add(new ItemPos(new Point(x, y + i), item));
                }
            }
            oldBonuses.ForEach(b => destroyedBy[b] = new ItemPos[0]);
            //
            var dAct = new DestroyAction
            {
                MatchDestroyedPos = new[]
                {
                    new ItemPos(new Point(x, y), Items[x, y]),
                    new ItemPos(new Point(x, y + 1), Items[x, y + 1]),
                    new ItemPos(new Point(x, y + 2), Items[x, y + 2]),
                },
                SpawnBonuses = new ItemPos[0],
                DestroyedBy = destroyedBy,
            };

            Items[x, y] = new Item(color, shape);
            Items[x, y + 1] = new Item(color, ItemShape.VLine);
            Items[x, y + 2] = new Item(color, shape);

            var spAct = new SpawnAction
            {
                Positions = new[]
                {
                    new ItemPos(new Point(x, y), Items[x, y]),
                    new ItemPos(new Point(x, y + 1), Items[x, y + 1]),
                    new ItemPos(new Point(x, y + 2), Items[x, y + 2]),
                }
            };
            return new IAction[] {dAct, spAct};
        }
    }
}
