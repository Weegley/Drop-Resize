using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using DropResize;

internal static class CardInteractionTests
{
    private sealed class TestWindow : Form1
    {
        public TestWindow()
        {
            // Exercise real WinForms handles and scrolling without displaying or activating a window.
            Opacity = 0;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            WindowState = FormWindowState.Normal;
            Bounds = new Rectangle(-20000, -20000, 1200, 700);
            Show();
        }

        protected override void OnShown(EventArgs e) { } // Do not check for updates during UI tests.
        protected override bool ShowWithoutActivation { get { return true; } }

        public SelectableFlowLayoutPanel Panel { get { return Field<SelectableFlowLayoutPanel>("cardsPanel"); } }

        public T Field<T>(string name)
        {
            return (T)typeof(Form1).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(this);
        }

        public object Call(string method, params object[] arguments)
        {
            return typeof(Form1).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(this, arguments);
        }

        public bool PressKey(Keys keys)
        {
            var message = Message.Create(Handle, 0x100, IntPtr.Zero, IntPtr.Zero);
            return ProcessCmdKey(ref message, keys);
        }

        public void ClickCard(ImageCardControl card, Keys modifiers = Keys.None)
        {
            Call("SelectCard", card, modifiers);
        }

        public ImageCardControl[] AddCards(int count, int failedIndex = -1)
        {
            Panel.SuspendLayout();
            try
            {
                for (var index = 0; index < count; index++)
                    Call("AddResultCard", new ResizeResult {
                        InputPath = "photo-" + index + ".png",
                        OutputPath = index == failedIndex ? null : "unused",
                        ErrorMessage = index == failedIndex ? "broken image" : null,
                        Thumbnail = index == failedIndex ? null : new Bitmap(8, 8)
                    });
            }
            finally { Panel.ResumeLayout(true); }
            Application.DoEvents();
            return Panel.Controls.OfType<ImageCardControl>().ToArray();
        }
    }

    private static void Assert(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }

    private static void AssertSelection(TestWindow window, ImageCardControl[] cards, params int[] indexes)
    {
        var expected = new HashSet<ImageCardControl>(indexes.Select(index => cards[index]));
        Assert(window.Field<HashSet<ImageCardControl>>("selectedCards").SetEquals(expected),
            "Selection differs from the expected range: " + string.Join(",", indexes));
        foreach (var card in cards.Where(card => !card.IsDisposed))
            Assert(card.IsSelected == expected.Contains(card), "The card highlight disagrees with selection.");
    }

    public static void TestShiftClick()
    {
        using (var window = new TestWindow())
        {
            var cards = window.AddCards(10, 5);
            window.ClickCard(cards[1], Keys.Shift);
            AssertSelection(window, cards, 1);
            window.ClickCard(cards[2]);
            window.ClickCard(cards[7], Keys.Shift);
            AssertSelection(window, cards, 2, 3, 4, 6, 7);
            window.ClickCard(cards[4], Keys.Shift);
            AssertSelection(window, cards, 2, 3, 4);
            window.ClickCard(cards[0], Keys.Shift);
            AssertSelection(window, cards, 0, 1, 2);
            Assert(window.Field<ImageCardControl>("selectionAnchorCard") == cards[2], "Shift moved the anchor.");
            Assert(window.Field<ImageCardControl>("focusedCard") == cards[0], "Focus did not follow Shift-click.");

            window.ClickCard(cards[9], Keys.Control);
            window.ClickCard(cards[7], Keys.Control | Keys.Shift);
            AssertSelection(window, cards, 0, 1, 2, 7, 8, 9);
            window.ClickCard(cards[8], Keys.Shift);
            AssertSelection(window, cards, 8, 9);
            window.ClickCard(cards[8], Keys.Control);
            AssertSelection(window, cards, 9);
            window.ClickCard(cards[5], Keys.Shift);
            AssertSelection(window, cards, 6, 7, 8);
            window.ClickCard(cards[5]);
            AssertSelection(window, cards);
        }
    }

    public static void TestShiftArrows()
    {
        using (var window = new TestWindow())
        {
            var cards = window.AddCards(10);
            window.ClickCard(cards[3]);
            Assert(window.PressKey(Keys.Shift | Keys.Right), "Shift+Right was not handled.");
            window.PressKey(Keys.Shift | Keys.Right);
            AssertSelection(window, cards, 3, 4, 5);
            window.PressKey(Keys.Shift | Keys.Left);
            AssertSelection(window, cards, 3, 4);
            window.PressKey(Keys.Shift | Keys.Left);
            window.PressKey(Keys.Shift | Keys.Left);
            AssertSelection(window, cards, 2, 3);
            window.PressKey(Keys.Right);
            AssertSelection(window, cards, 3);
            window.PressKey(Keys.Shift | Keys.Left);
            AssertSelection(window, cards, 2, 3);

            window.ClickCard(cards[0], Keys.Control);
            window.PressKey(Keys.Control | Keys.Shift | Keys.Right);
            AssertSelection(window, cards, 0, 1, 2, 3);
            window.PressKey(Keys.Shift | Keys.Left);
            window.PressKey(Keys.Shift | Keys.Left);
            AssertSelection(window, cards, 0);
            window.ClickCard(cards[9]);
            window.PressKey(Keys.Shift | Keys.Right);
            AssertSelection(window, cards, 9);
            window.Call("ClearCurrentBatch");
            cards = window.AddCards(3);
            window.Call("ApplyMarqueeSelection", cards[0].Bounds, false);
            window.Call("ApplyMarqueeSelection", cards[1].Bounds, true);
            window.PressKey(Keys.Shift | Keys.Right);
            AssertSelection(window, cards, 0, 1);
            window.Call("ClearFocusedCard");
            window.PressKey(Keys.Control | Keys.A);
            window.PressKey(Keys.Shift | Keys.Right);
            AssertSelection(window, cards, 0, 1);
        }
    }

    public static void TestShiftVerticalArrows()
    {
        using (var window = new TestWindow())
        {
            var cards = window.AddCards(23);
            var firstRow = cards.TakeWhile(card => card.Top == cards[0].Top).Count();
            Assert(firstRow >= 2 && firstRow < cards.Length, "The fixture did not wrap cards into rows.");
            window.ClickCard(cards[1]);
            window.PressKey(Keys.Shift | Keys.Down);
            AssertSelection(window, cards, Enumerable.Range(1, firstRow + 1).ToArray());
            window.PressKey(Keys.Shift | Keys.Up);
            AssertSelection(window, cards, 1);
            for (var row = 0; row < 30; row++) window.PressKey(Keys.Shift | Keys.Down);
            var lastFocused = window.Field<ImageCardControl>("focusedCard");
            var lastIndex = Array.IndexOf(cards, lastFocused);
            Assert(lastFocused.Top == cards.Last().Top, "Down did not reach the last, incomplete row.");
            AssertSelection(window, cards, Enumerable.Range(1, lastIndex).ToArray());
            Assert(window.Panel.AutoScrollPosition.Y < 0, "Keyboard navigation did not scroll to the selected card.");
            Assert(window.Panel.ClientRectangle.IntersectsWith(lastFocused.Bounds), "Focused card is off screen.");
        }
    }

    public static void TestPartialRemoval()
    {
        using (var window = new TestWindow())
        {
            var cards = window.AddCards(20);
            window.ClickCard(cards[3]);
            window.ClickCard(cards[11], Keys.Shift);
            Assert(window.PressKey(Keys.Delete), "Delete was not handled.");
            var expected = cards.Take(3).Concat(cards.Skip(12)).ToArray();
            Assert(window.Panel.Controls.OfType<ImageCardControl>().SequenceEqual(expected), "Deletion reordered survivors.");
            Assert(cards.Skip(3).Take(9).All(card => card.IsDisposed && card.Result.Thumbnail == null),
                "Deleted cards retained controls or previews.");
            Assert(expected.All(card => !card.IsDisposed && card.Result.Thumbnail != null), "A surviving preview was disposed.");
            AssertSelection(window, cards, 12);
            window.PressKey(Keys.Shift | Keys.Right);
            AssertSelection(window, cards, 12, 13);

            window.ClickCard(cards[0]);
            window.ClickCard(cards[18], Keys.Control);
            window.PressKey(Keys.Delete);
            AssertSelection(window, cards, 19);
            window.Call("ClearCurrentBatch");
            cards = window.AddCards(3, 1);
            window.ClickCard(cards[1]);
            window.PressKey(Keys.Delete);
            Assert(cards[1].IsDisposed, "The focused error card could not be removed.");
            AssertSelection(window, cards, 2);
        }
    }

    public static void TestLargeBatchClear()
    {
        using (var window = new TestWindow())
        {
            var cards = window.AddCards(1000);
            window.ClickCard(cards[500]);
            window.ClickCard(cards[999], Keys.Shift);
            var pending = new ResizeResult { Thumbnail = new Bitmap(8, 8) };
            window.Field<ConcurrentQueue<ResizeResult>>("pendingResults").Enqueue(pending);
            var layouts = 0;
            window.Panel.Layout += (sender, e) => layouts++;
            var timer = Stopwatch.StartNew();
            window.Call("ClearCurrentBatch");
            timer.Stop();
            Console.WriteLine("  Clear 1000 cards: {0:F1} ms; layouts: {1}.", timer.Elapsed.TotalMilliseconds, layouts);
            Assert(layouts <= 5, "Clearing a batch repeatedly recalculates the layout.");
            Assert(cards.All(card => card.IsDisposed && card.Result.Thumbnail == null), "Cleared cards retained resources.");
            Assert(pending.Thumbnail == null && window.Field<ConcurrentQueue<ResizeResult>>("pendingResults").IsEmpty,
                "Queued results were not released.");
            AssertSelection(window, cards);
            Assert(window.Field<ImageCardControl>("focusedCard") == null &&
                window.Field<ImageCardControl>("selectionAnchorCard") == null, "Cleared cards retained focus or anchor.");
            Assert(window.Panel.Controls.Count == 1 && window.Panel.Controls[0] is Label,
                "The empty-list instructions were not restored.");
            Assert(window.Panel.Visible && window.Panel.AutoScrollPosition == Point.Empty,
                "The list stayed hidden or retained its old scroll offset.");
            window.Call("ClearCurrentBatch");
            cards = window.AddCards(3);
            window.ClickCard(cards[1], Keys.Shift);
            window.PressKey(Keys.Shift | Keys.Right);
            AssertSelection(window, cards, 1, 2);
        }
    }

    public static void TestLargeSelectionDelete()
    {
        using (var window = new TestWindow())
        {
            var cards = window.AddCards(1000);
            window.PressKey(Keys.Control | Keys.A);
            var layouts = 0;
            window.Panel.Layout += (sender, e) => layouts++;
            var timer = Stopwatch.StartNew();
            window.PressKey(Keys.Delete);
            timer.Stop();
            Console.WriteLine("  Delete 1000 cards: {0:F1} ms; layouts: {1}.", timer.Elapsed.TotalMilliseconds, layouts);
            Assert(layouts <= 5, "Bulk deletion repeatedly recalculates the layout.");
            Assert(cards.All(card => card.IsDisposed && card.Result.Thumbnail == null), "Deleted cards retained resources.");
            Assert(window.Panel.Controls.Count == 1 && window.Panel.Visible, "Bulk deletion did not restore the empty list.");
            AssertSelection(window, cards);
            cards = window.AddCards(2);
            window.ClickCard(cards[0]);
            window.PressKey(Keys.Shift | Keys.Right);
            AssertSelection(window, cards, 0, 1);
        }
    }

    public static void TestBatchFolderCleanup()
    {
        using (var window = new TestWindow())
        {
            var folder = TempBatchManager.Create();
            try
            {
                for (var index = 0; index < 100; index++)
                    File.WriteAllText(Path.Combine(folder.BatchFolder, index + ".txt"), "temporary output");
                typeof(Form1).GetField("currentBatchFolder", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(window, folder);
                var cards = window.AddCards(3);
                var cleanup = (Task)window.Call("CleanupCurrentBatchFolderAsync");
                var deadline = Stopwatch.StartNew();
                while (!cleanup.IsCompleted && deadline.Elapsed < TimeSpan.FromSeconds(20))
                {
                    Application.DoEvents();
                    Thread.Sleep(1);
                }
                Assert(cleanup.IsCompleted, "Batch cleanup did not complete.");
                cleanup.GetAwaiter().GetResult();
                Assert(!Directory.Exists(folder.BatchFolder), "The old output folder remains.");
                Assert(window.Field<TempBatchManager>("currentBatchFolder") == null, "The old folder is still active.");
                Assert(cards.All(card => card.IsDisposed), "Batch cleanup retained cards.");

                // Cancel a replacement batch while its old files are being cleaned up.
                folder = TempBatchManager.Create();
                for (var index = 0; index < 100; index++)
                    File.WriteAllText(Path.Combine(folder.BatchFolder, index + ".txt"), "temporary output");
                typeof(Form1).GetField("currentBatchFolder", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(window, folder);
                window.AddCards(3);
                var processing = (Task)window.Call("StartProcessingAsync", new List<string> {
                    Path.Combine(folder.BatchFolder, "missing-input.png") });
                window.Call("CancelCurrentBatch");
                deadline.Restart();
                while (!processing.IsCompleted && deadline.Elapsed < TimeSpan.FromSeconds(20))
                {
                    Application.DoEvents();
                    Thread.Sleep(1);
                }
                Assert(processing.IsCompleted, "Cancel during batch cleanup did not complete.");
                processing.GetAwaiter().GetResult();
                Assert(!Directory.Exists(folder.BatchFolder), "Canceled replacement retained the previous folder.");
                Assert(!window.Field<bool>("isProcessing") &&
                    window.Field<CancellationTokenSource>("currentCancellation") == null,
                    "Canceled preparation left processing active.");
                Assert(!window.Field<Control>("processingOverlay").Visible &&
                    !window.Field<System.Windows.Forms.Timer>("resultFlushTimer").Enabled,
                    "Canceled preparation left progress updates active.");
                window.Call("DeleteCurrentBatchFolder");
            }
            finally { folder.Delete(); }
        }
    }
}
