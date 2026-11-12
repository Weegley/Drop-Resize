using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
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
    private static string artifactsRoot;

    public static void Initialize(string testRoot) { artifactsRoot = testRoot; }

    private sealed class TestWindow : Form1
    {
        public readonly List<ProcessStartInfo> OpenedFiles = new List<ProcessStartInfo>();
        public Exception OpenFailure;

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

        protected override void OpenFileInDefaultApplication(ProcessStartInfo startInfo)
        {
            if (OpenFailure != null) throw OpenFailure;
            OpenedFiles.Add(startInfo);
        }

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

        public void MouseEvent(Control control, string method, MouseButtons button, int clicks)
        {
            typeof(Control).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(control, new object[] { new MouseEventArgs(button, clicks, 8, 8, 0) });
        }

        public void DoubleClickCard(Control control, MouseButtons button = MouseButtons.Left)
        {
            MouseEvent(control, "OnMouseDown", button, 1);
            MouseEvent(control, "OnMouseUp", button, 1);
            MouseEvent(control, "OnMouseDown", button, 2);
            MouseEvent(control, "OnMouseDoubleClick", button, 2);
            MouseEvent(control, "OnMouseUp", button, 2);
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

    private static string MakeOpenable(ImageCardControl card)
    {
        var directory = Path.Combine(artifactsRoot, "open-" + Guid.NewGuid().ToString("N").Substring(0, 8));
        Directory.CreateDirectory(directory);
        var outputPath = Path.Combine(directory, "resized & \u0444\u043e\u0442\u043e.png");
        using (var image = new Bitmap(8, 8)) image.Save(outputPath, System.Drawing.Imaging.ImageFormat.Png);
        card.Result.OutputPath = outputPath;
        return outputPath;
    }

    public static void TestOpenByDoubleClick()
    {
        using (var window = new TestWindow())
        {
            var cards = window.AddCards(2);
            var outputPath = MakeOpenable(cards[0]);
            window.MouseEvent(cards[0], "OnMouseDown", MouseButtons.Left, 1);
            window.MouseEvent(cards[0], "OnMouseUp", MouseButtons.Left, 1);
            Assert(window.OpenedFiles.Count == 0, "A single click opened a viewer.");

            var clickTargets = new Control[] { cards[0] }.Concat(cards[0].Controls.Cast<Control>()).ToArray();
            foreach (var target in clickTargets)
            {
                window.ClickCard(cards[1]);
                var previousCount = window.OpenedFiles.Count;
                window.DoubleClickCard(target);
                Assert(window.OpenedFiles.Count == previousCount + 1, "A card or child double-click did not open exactly once.");
                var startInfo = window.OpenedFiles.Last();
                Assert(startInfo.FileName == outputPath && startInfo.FileName != cards[0].Result.InputPath,
                    "Double-click opened the source image instead of the resized output.");
                Assert(startInfo.UseShellExecute && startInfo.Verb == "open" &&
                    startInfo.WorkingDirectory == Path.GetDirectoryName(outputPath),
                    "The output was not opened through its default Windows association.");
                Assert(window.Field<ImageCardControl>("dragStartCard") == null, "Double-click retained a pending drag.");
            }
            var opened = window.OpenedFiles.Count;
            window.DoubleClickCard(cards[0], MouseButtons.Right);
            window.DoubleClickCard(cards[0], MouseButtons.Middle);
            Assert(window.OpenedFiles.Count == opened, "A non-left double-click opened a viewer.");
        }
    }

    public static void TestOpenByEnter()
    {
        using (var window = new TestWindow())
        {
            Assert(!window.PressKey(Keys.Enter) && window.OpenedFiles.Count == 0, "Enter in an empty window opened a viewer.");
            var cards = window.AddCards(3);
            var paths = cards.Select(MakeOpenable).ToArray();
            window.ClickCard(cards[0]);
            Assert(window.PressKey(Keys.Enter), "Enter on a focused card was not handled.");
            Assert(window.OpenedFiles.Count == 1 && window.OpenedFiles.Last().FileName == paths[0], "Enter opened the wrong card.");

            window.ClickCard(cards[2], Keys.Shift);
            window.PressKey(Keys.Enter);
            Assert(window.OpenedFiles.Count == 2 && window.OpenedFiles.Last().FileName == paths[2],
                "Enter opened multiple selected cards or ignored the current card.");
            AssertSelection(window, cards, 0, 1, 2);
            window.PressKey(Keys.Control | Keys.Enter);
            Assert(window.OpenedFiles.Count == 2, "Modified Enter unexpectedly opened a viewer.");

            window.PressKey(Keys.Left);
            window.PressKey(Keys.Enter);
            Assert(window.OpenedFiles.Last().FileName == paths[1], "Enter ignored keyboard navigation.");
            window.Field<ComboBox>("workerComboBox").Select();
            var previousCount = window.OpenedFiles.Count;
            Assert(!window.PressKey(Keys.Enter) && window.OpenedFiles.Count == previousCount,
                "Enter on a sidebar control opened an image.");

            window.ClickCard(cards[1]);
            window.PressKey(Keys.Delete);
            window.PressKey(Keys.Enter);
            Assert(window.OpenedFiles.Last().FileName == paths[2], "Enter did not follow focus after deletion.");
            // Tabbing into a card also opens that card, rather than the previous mouse/arrow target.
            cards[0].Select();
            window.PressKey(Keys.Enter);
            Assert(window.OpenedFiles.Last().FileName == paths[0], "Enter ignored the card's actual input focus.");
            previousCount = window.OpenedFiles.Count;
            window.Call("ClearCurrentBatch");
            window.PressKey(Keys.Enter);
            Assert(window.OpenedFiles.Count == previousCount, "Enter reopened a cleared card.");
        }
    }

    public static void TestOpenFailures()
    {
        using (var window = new TestWindow())
        {
            var cards = window.AddCards(2, 1);
            var outputPath = MakeOpenable(cards[0]);
            File.Delete(outputPath);
            window.DoubleClickCard(cards[0]);
            window.PressKey(Keys.Enter);
            Assert(window.OpenedFiles.Count == 0 && window.Field<Label>("statusLabel").Text.Contains("no longer available"),
                "A missing output did not report a recoverable error.");
            window.DoubleClickCard(cards[1]);
            window.PressKey(Keys.Enter);
            Assert(window.OpenedFiles.Count == 0, "An error card opened a viewer.");

            MakeOpenable(cards[0]);
            foreach (var failure in new Exception[] { new Win32Exception(1155, "No associated application"),
                new InvalidOperationException("Cannot launch viewer"), new IOException("File became unavailable"),
                new UnauthorizedAccessException("Access denied") })
            {
                window.OpenFailure = failure;
                window.DoubleClickCard(cards[0]);
                Assert(window.Field<Label>("statusLabel").Text.Contains(failure.Message),
                    "A viewer launch failure was not reported in the status line.");
            }
            Assert(window.OpenedFiles.Count == 0, "A failed launch was treated as successful.");
            window.OpenFailure = null;
            window.PressKey(Keys.Enter);
            Assert(window.OpenedFiles.Count == 1, "A viewer error left card opening unusable.");
            var preview = cards[0].Result.Thumbnail;
            Assert(preview != null && preview.Width == 8, "Opening or a launch failure released the card's preview.");
            window.Call("ClearCurrentBatch");
            window.Call("OpenResultFile", cards[0]);
            Assert(window.OpenedFiles.Count == 1, "A disposed card opened a viewer.");
        }
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
