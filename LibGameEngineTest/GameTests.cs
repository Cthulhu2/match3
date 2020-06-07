using System;
using System.Collections.Generic;
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
            var items = new Item[4, 4];
            items[0, 0] = new Item(1, ItemShape.Circle);
            items[1, 0] = new Item(2, ItemShape.Circle); //
            items[2, 0] = new Item(1, ItemShape.Circle);
            items[3, 0] = new Item(1, ItemShape.Circle);

            items[0, 1] = new Item(1, ItemShape.Rect);
            items[1, 1] = new Item(2, ItemShape.Rect);
            items[2, 1] = new Item(3, ItemShape.Rect);
            items[3, 1] = new Item(4, ItemShape.Rect);

            items[0, 2] = new Item(5, ItemShape.Rect);
            items[1, 2] = new Item(6, ItemShape.Rect);
            items[2, 2] = new Item(7, ItemShape.Rect);
            items[3, 2] = new Item(8, ItemShape.Rect);

            items[0, 3] = new Item(9, ItemShape.Rect);
            items[1, 3] = new Item(10, ItemShape.Rect);
            items[2, 3] = new Item(11, ItemShape.Rect);
            items[3, 3] = new Item(12, ItemShape.Rect);

            var game = new Game(new Board(items));
            //
            IAction[] actions = game.Swap(new Point(0, 0), new Point(1, 0));
            //
            Assert.AreEqual(2, actions.Length);
            Assert.IsInstanceOf(typeof(SwapAction), actions[0]);
            Assert.IsInstanceOf(typeof(DestroyAction), actions[1]);
            var destroyAct = (DestroyAction) actions[1];
            var expectDestroyed = new List<Point>
            {
                new Point(1, 0),
                new Point(2, 0),
                new Point(3, 0),
            };
            Assert.AreEqual(expectDestroyed.Count, destroyAct.Positions.Length);
            foreach (Point dPos in destroyAct.Positions)
            {
                Assert.True(expectDestroyed.Contains(dPos));
            }
        }

        [Test]
        public void Match3Horizontal2()
        {
            var items = new Item[4, 4];

            items[0, 0] = new Item(9, ItemShape.Rect);
            items[1, 0] = new Item(10, ItemShape.Rect);
            items[2, 0] = new Item(11, ItemShape.Rect);
            items[3, 0] = new Item(12, ItemShape.Rect);

            items[0, 1] = new Item(1, ItemShape.Rect);
            items[1, 1] = new Item(2, ItemShape.Rect);
            items[2, 1] = new Item(3, ItemShape.Rect);
            items[3, 1] = new Item(4, ItemShape.Rect);

            items[0, 2] = new Item(5, ItemShape.Rect);
            items[1, 2] = new Item(6, ItemShape.Rect);
            items[2, 2] = new Item(7, ItemShape.Rect);
            items[3, 2] = new Item(8, ItemShape.Rect);

            items[0, 3] = new Item(1, ItemShape.Circle);
            items[1, 3] = new Item(2, ItemShape.Circle); //
            items[2, 3] = new Item(1, ItemShape.Circle);
            items[3, 3] = new Item(1, ItemShape.Circle);

            var game = new Game(new Board(items));
            //
            IAction[] actions = game.Swap(new Point(0, 3), new Point(1, 3));
            //
            Assert.IsInstanceOf(typeof(SwapAction), actions[0]);
            Assert.IsInstanceOf(typeof(DestroyAction), actions[1]);
            var destroyAct = (DestroyAction) actions[1];
            var expectDestroyed = new List<Point>
            {
                new Point(1, 3),
                new Point(2, 3),
                new Point(3, 3),
            };
            Assert.AreEqual(expectDestroyed.Count, destroyAct.Positions.Length);
            foreach (Point dPos in destroyAct.Positions)
            {
                Assert.True(expectDestroyed.Contains(dPos));
            }
        }

        [Test]
        public void Match3Vertical()
        {
            var items = new Item[4, 4];
            items[0, 0] = new Item(1, ItemShape.Circle);
            items[1, 0] = new Item(10, ItemShape.Rect);
            items[2, 0] = new Item(11, ItemShape.Rect);
            items[3, 0] = new Item(12, ItemShape.Rect);

            items[0, 1] = new Item(1, ItemShape.Circle);
            items[1, 1] = new Item(13, ItemShape.Rect);
            items[2, 1] = new Item(14, ItemShape.Rect);
            items[3, 1] = new Item(15, ItemShape.Rect);

            items[0, 2] = new Item(2, ItemShape.Circle); //
            items[1, 2] = new Item(16, ItemShape.Rect);
            items[2, 2] = new Item(17, ItemShape.Rect);
            items[3, 2] = new Item(18, ItemShape.Rect);

            items[0, 3] = new Item(1, ItemShape.Circle);
            items[1, 3] = new Item(19, ItemShape.Rect);
            items[2, 3] = new Item(20, ItemShape.Rect);
            items[3, 3] = new Item(21, ItemShape.Rect);

            var game = new Game(new Board(items));
            //
            IAction[] actions = game.Swap(new Point(0, 2), new Point(0, 3));
            //
            Assert.IsInstanceOf(typeof(SwapAction), actions[0]);
            Assert.IsInstanceOf(typeof(DestroyAction), actions[1]);
            var destroyAct = (DestroyAction) actions[1];
            var expectDestroyed = new List<Point>
            {
                new Point(0, 0),
                new Point(0, 1),
                new Point(0, 2),
            };
            Assert.AreEqual(expectDestroyed.Count, destroyAct.Positions.Length);
            foreach (Point dPos in destroyAct.Positions)
            {
                Assert.True(expectDestroyed.Contains(dPos));
            }
        }

        [Test]
        public void FallDown()
        {
            var items = new Item[4, 2];

            items[0, 0] = new Item(9, ItemShape.Rect);
            items[1, 0] = new Item(10, ItemShape.Rect);
            items[2, 0] = new Item(11, ItemShape.Rect);
            items[3, 0] = new Item(12, ItemShape.Rect);

            items[0, 1] = new Item(1, ItemShape.Circle);
            items[1, 1] = new Item(1, ItemShape.Circle);
            items[2, 1] = new Item(2, ItemShape.Circle); //
            items[3, 1] = new Item(1, ItemShape.Circle);

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
    }
}
