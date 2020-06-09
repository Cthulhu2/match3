using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using NUnit.Framework;

// ReSharper disable CheckNamespace

namespace GameEngine
{
    [TestFixture]
    public class GameTests
    {
        private static Item Ball(int color)
        {
            return new Item(color, ItemShape.Ball);
        }

        private static Item Cube(int color)
        {
            return new Item(color, ItemShape.Cube);
        }
        
        private static Item Bomb(int color)
        {
            return new Item(color, ItemShape.Bomb);
        }

        private static Item[,] CreateBoard(int w, int h, Item[] items)
        {
            var aItems = new Item[w, h];

            int x = 0;
            int y = 0;
            foreach (Item it in items)
            {
                aItems[x, y] = it;
                x++;
                if (x == w)
                {
                    x = 0;
                    y++;
                }
            }

            return aItems;
        }

        [Test]
        public void Reset()
        {
            var game = new Game(new Board());
            //
            game.Reset();
            //
            Assert.AreEqual(0, game.Scores);
            foreach (Item item in game.Items)
            {
                Assert.NotNull(item);
            }

            Console.Out.WriteLine(game.Dump());
        }

        [Test]
        public void GameOver()
        {
            var game = new Game(new Board());
            game.Reset();
            for (int i = 0; i < Game.GameDurationSec; i++)
            {
                game.Tick();
            }

            Assert.False(game.IsOver);

            game.Tick();

            Assert.True(game.IsOver);
        }

        [Test]
        public void CanSwap()
        {
            var game = new Game(new Board());
            game.Reset();
            Assert.False(game.CanSwap(-1, 0, 0, 0));
            Assert.False(game.CanSwap(8, 0, 7, 0));

            Assert.False(game.CanSwap(0, 0, 1, 1));
            Assert.False(game.CanSwap(0, 0, 1, 1));

            Assert.True(game.CanSwap(0, 0, 0, 1));
            Assert.True(game.CanSwap(0, 0, 1, 0));
        }

        [Test]
        public void Match3Horizontal()
        {
            Item[,] items = CreateBoard(4, 4, new[]
            {
                // 0        1        2        3
                Ball(1), Ball(2), Ball(1), Ball(1), // 0
                Cube(1), Cube(2), Cube(3), Cube(4), // 1
                Cube(5), Cube(6), Cube(7), Cube(8), // 2
                Cube(9), Cube(1), Cube(2), Cube(3), // 3
            });

            var game = new Game(new Board(items));
            //
            IAction[] actions = game.Swap(new Point(0, 0), new Point(1, 0));
            //
            Assert.True(actions.Length >= 2);
            Assert.IsInstanceOf(typeof(SwapAction), actions[0]);
            Assert.IsInstanceOf(typeof(DestroyAction), actions[1]);
            var dAct = (DestroyAction) actions[1];
            var destroyed = new List<Point>
            {
                new Point(1, 0),
                new Point(2, 0),
                new Point(3, 0),
            };
            Assert.AreEqual(destroyed.Count, dAct.MatchDestroyedPos.Length);
            foreach (ItemPos dPos in dAct.MatchDestroyedPos)
            {
                Assert.True(destroyed.Contains(dPos.Pos));
            }
        }

        [Test]
        public void Match3Horizontal2()
        {
            Item[,] items = CreateBoard(4, 4, new[]
            {
                // 0        1        2        3
                Cube(1), Cube(2), Cube(3), Cube(4), // 0
                Cube(5), Cube(6), Cube(7), Cube(8), // 1
                Cube(9), Cube(1), Cube(2), Cube(3), // 2
                Ball(1), Ball(2), Ball(1), Ball(1), // 3
            });

            var game = new Game(new Board(items));
            //
            IAction[] actions = game.Swap(new Point(0, 3), new Point(1, 3));
            //
            Assert.IsInstanceOf(typeof(SwapAction), actions[0]);
            Assert.IsInstanceOf(typeof(DestroyAction), actions[1]);
            var dAct = (DestroyAction) actions[1];
            var destroyed = new List<Point>
            {
                new Point(1, 3),
                new Point(2, 3),
                new Point(3, 3),
            };
            Assert.AreEqual(destroyed.Count, dAct.MatchDestroyedPos.Length);
            foreach (ItemPos dPos in dAct.MatchDestroyedPos)
            {
                Assert.True(destroyed.Contains(dPos.Pos));
            }
        }

        [Test]
        public void Match3Vertical()
        {
            Item[,] items = CreateBoard(4, 4, new[]
            {
                // 0        1        2        3
                Ball(1), Cube(4), Cube(5), Cube(6), // 0
                Ball(1), Cube(7), Cube(8), Cube(9), // 1
                Ball(2), Cube(4), Cube(5), Cube(6), // 2
                Ball(1), Cube(7), Cube(8), Cube(9), // 3
            });

            var game = new Game(new Board(items));
            //
            IAction[] actions = game.Swap(new Point(0, 2), new Point(0, 3));
            //
            Assert.IsInstanceOf(typeof(SwapAction), actions[0]);
            Assert.IsInstanceOf(typeof(DestroyAction), actions[1]);
            var dAct = (DestroyAction) actions[1];
            var destroyed = new List<Point>
            {
                new Point(0, 0),
                new Point(0, 1),
                new Point(0, 2),
            };
            Assert.AreEqual(destroyed.Count, dAct.MatchDestroyedPos.Length);
            foreach (ItemPos dPos in dAct.MatchDestroyedPos)
            {
                Assert.True(destroyed.Contains(dPos.Pos));
            }
        }

        [Test]
        public void FallDownAction()
        {
            Item[,] items = CreateBoard(4, 2, new[]
            {
                // 0        1        2        3
                Cube(5), Cube(6), Cube(7), Cube(8), // 0
                Ball(1), Ball(1), Ball(2), Ball(1), // 1
            });

            var game = new Game(new Board(items));
            //
            IAction[] actions = game.Swap(new Point(2, 1), new Point(3, 1));
            //
            Assert.IsInstanceOf(typeof(SwapAction), actions[0]);
            Assert.IsInstanceOf(typeof(DestroyAction), actions[1]);
            Assert.IsInstanceOf(typeof(FallDownAction), actions[2]);
            var fallAct = (FallDownAction) actions[2];
            var expectFallPos = new List<FallDownPos>
            {
                new FallDownPos(new Point(0, 0), new Point(0, 1)),
                new FallDownPos(new Point(1, 0), new Point(1, 1)),
                new FallDownPos(new Point(2, 0), new Point(2, 1)),
            };
            Assert.AreEqual(expectFallPos.Count, fallAct.Positions.Length);
            foreach (FallDownPos fPos in fallAct.Positions)
            {
                Assert.True(expectFallPos.Contains(fPos));
            }
        }

        [Test]
        public void SpawnAction()
        {
            Item[,] items = CreateBoard(4, 2, new[]
            {
                // 0        1        2        3
                Cube(3), Cube(3), Cube(1), Cube(3), // 0
                Ball(1), Ball(1), Ball(2), Ball(1), // 1
            });

            var game = new Game(new Board(items));
            //
            IAction[] actions = game.Swap(new Point(2, 1), new Point(3, 1));
            //
            Assert.IsInstanceOf(typeof(SwapAction), actions[0]);
            Assert.IsInstanceOf(typeof(DestroyAction), actions[1]);
            Assert.IsInstanceOf(typeof(FallDownAction), actions[2]);
            Assert.IsInstanceOf(typeof(SpawnAction), actions[3]);
            var spawnAct = (SpawnAction) actions[3];
            var expectSpawnPos = new List<Point>
            {
                new Point(0, 0),
                new Point(1, 0),
                new Point(2, 0),
            };
            Assert.AreEqual(expectSpawnPos.Count, spawnAct.Positions.Length);
            foreach (ItemPos spPos in spawnAct.Positions)
            {
                Assert.True(expectSpawnPos.Contains(spPos.Pos));
            }
        }

        [Test]
        public void SpawnBonusLine()
        {
            Item[,] items = CreateBoard(4, 2, new[]
            {
                // 0        1        2        3
                Cube(3), Ball(1), Cube(3), Cube(3), // 0
                Ball(1), Ball(2), Ball(1), Ball(1), // 1
            });

            var game = new Game(new Board(items));
            //
            IAction[] actions = game.Swap(new Point(1, 0), new Point(1, 1));
            //
            var dAct = (DestroyAction) actions[1];
            var hLine = new Point(1, 1);
            Assert.AreEqual(hLine, dAct.SpawnBonuses[0].Pos);
            Assert.True(game.Items[hLine.X, hLine.Y].Shape == ItemShape.HLine);
        }

        [Test]
        public void SpawnBonusBombLine()
        {
            Item[,] items = CreateBoard(5, 2, new[]
            {
                // 0        1        2        3        4
                Cube(3), Cube(3), Ball(1), Cube(3), Cube(3), // 0
                Ball(1), Ball(1), Ball(2), Ball(1), Ball(1), // 1
            });

            var game = new Game(new Board(items));
            //
            IAction[] actions = game.Swap(new Point(2, 0), new Point(2, 1));
            //
            var dAct = (DestroyAction) actions[1];
            var bomb = new Point(2, 1);
            Assert.AreEqual(bomb, dAct.SpawnBonuses[0].Pos);
            Assert.True(game.Items[bomb.X, bomb.Y].IsBombShape);
        }

        [Test]
        public void SpawnBonusBombCross()
        {
            Item[,] items = CreateBoard(4, 3, new[]
            {
                // 0        1        2        3
                Cube(1), Cube(1), Ball(1), Cube(1), // 0
                Cube(1), Cube(1), Ball(1), Cube(1), // 1
                Ball(1), Ball(1), Ball(2), Ball(1), // 2
            });

            var game = new Game(new Board(items));
            //
            IAction[] actions = game.Swap(new Point(2, 2), new Point(3, 2));
            //
            Console.Out.WriteLine(game.Dump());
            var dAct = (DestroyAction) actions[1];
            var bomb = new Point(2, 2);
            Assert.AreEqual(bomb, dAct.SpawnBonuses[0].Pos);
            Assert.True(dAct.SpawnBonuses[0].Item.IsBombShape);
        }
        
        [Test]
        public void ChainedExplosions()
        {
            Item[,] items = CreateBoard(4, 3, new[]
            {
                // 0        1        2        3
                Cube(1), Cube(1), Cube(3), Cube(1), // 0
                Cube(1), Cube(1), Bomb(2), Cube(1), // 1
                Ball(1), Ball(1), Cube(3), Bomb(1), // 2
            });

            var game = new Game(new Board(items));
            // 
            IAction[] actions = game.Swap(new Point(2, 2), new Point(3, 2));
            //
            Console.Out.WriteLine(game.Dump());
            var dAct = (DestroyAction) actions[1];
            var matchBomb = new Point(2, 2);
            var chainBomb = new Point(2, 1);
            //
            Assert.False(dAct.MatchDestroyedPos.Any(p => p.Pos == chainBomb));
            Assert.AreEqual(3, dAct.MatchDestroyedPos.Length);
            Assert.True(dAct.MatchDestroyedPos.Any(p => p.Pos == new Point(0, 2)));
            Assert.True(dAct.MatchDestroyedPos.Any(p => p.Pos == new Point(1, 2)));
            Assert.True(dAct.MatchDestroyedPos.Any(p => p.Pos == matchBomb));
            
            Assert.True(dAct.DestroyedBy.Keys.Any(p => p.Pos == matchBomb));
            Assert.True(dAct.DestroyedBy.Keys.Any(p => p.Pos == chainBomb));
            // Chain bomb explosion
            ItemPos matchBombPos = dAct.DestroyedBy
                .Keys
                .First(p => p.Pos == matchBomb);
            ItemPos chainBombPos = dAct.DestroyedBy
                .Keys
                .First(p => p.Pos == chainBomb);
            ItemPos[] byMatchBombPos = dAct.DestroyedBy[matchBombPos];
            ItemPos[] byChainBombPos = dAct.DestroyedBy[chainBombPos];
            //
            Assert.AreEqual(4, byMatchBombPos.Length);
            Assert.True(byMatchBombPos.Any(p => p.Pos == new Point(1, 1)));
            Assert.True(byMatchBombPos.Any(p => p.Pos == new Point(2, 1)));
            Assert.True(byMatchBombPos.Any(p => p.Pos == new Point(3, 1)));
            Assert.True(byMatchBombPos.Any(p => p.Pos == new Point(3, 2)));
            
            Assert.AreEqual(3, byChainBombPos.Length);
            Assert.True(byChainBombPos.Any(p => p.Pos == new Point(1, 0)));
            Assert.True(byChainBombPos.Any(p => p.Pos == new Point(2, 0)));
            Assert.True(byChainBombPos.Any(p => p.Pos == new Point(3, 0)));
        }
    }
}
