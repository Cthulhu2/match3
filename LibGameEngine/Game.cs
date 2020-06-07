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

            Point[] matches = _board.CalcMatches().ToArray();

            if (matches.Length == 0)
            {
                _board.Swap(src, dest); // swap back

                actions.Add(new SwapAction
                {
                    SrcPos = src,
                    SrcItem = Items[src.X, src.Y],
                    DestPos = dest,
                    DestItem = Items[dest.X, dest.Y],
                });
            }
            else
            {
                actions.Add(new DestroyAction {Positions = matches});
                foreach (Point m in matches)
                {
                    Item item = Items[m.X, m.Y];
                    Scores += item.Score;
                    Items[m.X, m.Y] = null;
                }

                FallDownPos[] fallen = _board.CalcFallDownPositions().ToArray();
                actions.Add(new FallDownAction {Positions = fallen});

                Point[] spawned = _board.SpawnItems().ToArray();
                actions.Add(new SpawnAction {Positions = spawned});
            }

            return actions.ToArray();
        }
    }
}
