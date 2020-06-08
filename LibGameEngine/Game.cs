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

        public bool IsGameOver
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

        private Point[] GetBombNeighbour(Point bombPos)
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
        
        private SpawnPos CalcBonusSpawn(Point src, Point dest, Point[] line)
        {
            if (line.Length == 5)
            {
                Point targetPos = TargetBonusPos(src, dest, line);

                int color = Items[targetPos.X, targetPos.Y].Color;
                return new SpawnPos(targetPos, new Item(color, ItemShape.Bomb));
            }

            if (line.Length == 4)
            {
                Point targetPos = TargetBonusPos(src, dest, line);

                int color = Items[targetPos.X, targetPos.Y].Color;
                bool vertical = (line[0].X == line[1].X);
                ItemShape shape = vertical ? ItemShape.VLine : ItemShape.HLine;

                return new SpawnPos(targetPos, new Item(color, shape));
            }

            return null;
        }

        private SpawnPos CalcBonusSpawn(Point src,
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
            return new SpawnPos(targetPos, new Item(color, ItemShape.Bomb));
        }

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
            //
            var bonuses = new List<SpawnPos>();
            match.MatchLines.ForEach(line =>
            {
                SpawnPos bonus = CalcBonusSpawn(src, dest, line);
                if (bonus != null)
                {
                    bonuses.Add(bonus);
                }
            });
            match.MatchCrosses.ForEach(cross =>
            {
                SpawnPos bonus = CalcBonusSpawn(src, dest, cross);
                if (bonus != null)
                {
                    bonuses.Add(bonus);
                }
            });

            var exploded = new List<Point>();
            Point[] bombBonuses = regularDestroy
                .Where(p => Items[p.X, p.Y].IsBombShape)
                .ToArray();
            foreach (Point bomb in bombBonuses)
            {
                Point[] bombNeighbours = GetBombNeighbour(bomb)
                    .Except(regularDestroy)
                    .ToArray();
                exploded.AddRange(bombNeighbours);
            }

            var exterminated = new List<Point>();
            Point[] lineBonuses = regularDestroy
                .Where(p => Items[p.X, p.Y].IsLineShape)
                .ToArray();
            foreach (Point linePos in lineBonuses)
            {
                Item it = Items[linePos.X, linePos.Y];
                Point[] line = GetLine(linePos, it.Shape)
                    .Except(regularDestroy)
                    .ToArray();
                exterminated.AddRange(line);
            }

            Point[] destroyed = new List<Point>()
                .Union(regularDestroy)
                .Union(exploded)
                .Union(exterminated)
                .ToArray();

            // Update board
            foreach (Point m in destroyed)
            {
                Item item = Items[m.X, m.Y];
                Scores += item.Score;
                Items[m.X, m.Y] = null;
            }

            bonuses.ForEach(b => Items[b.Pos.X, b.Pos.Y] = b.Item);
            //
            actions.Add(new DestroyAction
            {
                RegularDestroyedPos = regularDestroy.ToArray(),
                SpawnBonuses = bonuses.ToArray(),
                BombDestroyedPos = exploded.ToArray(),
                LineDestroyedPos = exterminated.ToArray(),
            });

            FallDownPos[] fallen = _board.CalcFallDownPositions()
                .ToArray();
            actions.Add(new FallDownAction {Positions = fallen});

            SpawnPos[] spawned = _board.SpawnItems().ToArray();
            actions.Add(new SpawnAction {Positions = spawned});

            return actions;
        }
    }
}
