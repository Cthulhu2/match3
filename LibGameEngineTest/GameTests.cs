using System;
using System.Drawing;
using NUnit.Framework;

// ReSharper disable CheckNamespace

namespace GameEngine
{
    [TestFixture]
    public class GameTests
    {
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
        public void ColFallDown()
        {
            var board = new Board();
            board.Reset();
            Item it10 = board.Items[1, 0];
            Item it11 = board.Items[1, 1];
            //Item it12 = board.Items[1, 2];
            Item it13 = board.Items[1, 3];
            Item it14 = board.Items[1, 4];
            //
            board.ColFallOneDown(new Point(1, 2));
            //
            Assert.AreNotEqual(board.Items[1, 0], it10); // new
            Assert.AreEqual(board.Items[1, 1], it10); // fall
            Assert.AreEqual(board.Items[1, 2], it11); // fall
            Assert.AreEqual(board.Items[1, 3], it13); // old
            Assert.AreEqual(board.Items[1, 4], it14); // old
        }

        [Test]
        public void RemoveAt()
        {
            var board = new Board();
            board.Reset();
            var p = new Point(3, 4);
            //
            Item it = board.RemoveAt(p);
            //
            Assert.NotNull(it);
            for (int y = 0; y < Board.Height; y++)
            {
                for (int x = 0; x < Board.Width; x++)
                {
                    if (x == p.X && y == p.Y)
                    {
                        Assert.Null(board.Items[x, y]);
                    }
                    else
                    {
                        Assert.NotNull(board.Items[x, y]);
                    }
                }
            }
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

            Assert.False(game.IsGameOver);

            game.Tick();

            Assert.True(game.IsGameOver);
        }
        
        [Test]
        public void CanSwap()
        {
            Assert.False(Game.CanSwap(-1, 0, 0, 0));
            Assert.False(Game.CanSwap(8, 0, 7, 0));
            
            Assert.False(Game.CanSwap(0, 0, 1, 1));
            Assert.False(Game.CanSwap(0, 0, 1, 1));
            
            Assert.True(Game.CanSwap(0, 0, 0, 1));
            Assert.True(Game.CanSwap(0, 0, 1, 0));
        }
    }
}
