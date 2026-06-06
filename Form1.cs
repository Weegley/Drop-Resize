using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace DropResize
{
    public partial class Form1 : Form
    {
        private readonly ProfileStore profileStore = new ProfileStore();
        private readonly UpdateCheckService updateCheckService = new UpdateCheckService();
        private readonly List<ImageCardControl> selectedCards = new List<ImageCardControl>();
        private readonly ConcurrentQueue<ResizeResult> pendingResults = new ConcurrentQueue<ResizeResult>();

        private List<ResizeProfile> profiles;
        private CancellationTokenSource currentCancellation;
        private CancellationTokenSource updateCheckCancellation;
        private Process currentWorkerProcess;
        private bool isProcessing;
        private bool updateCheckInProgress;
        private bool suppressUpdateCheckSettingSave;
        private TempBatchManager currentBatchFolder;
        private int currentBatchId;
        private ImageCardControl dragStartCard;
        private Point dragStartPoint;
        private bool marqueeSelecting;
        private bool marqueeAdditive;
        private Point marqueeStartContentPoint;
        private Point lastMarqueeMousePoint;
        private int pendingProgressDone;
        private int pendingProgressTotal;
        private int pendingProgressBatchId;
        private int progressUpdatePosted;

        public Form1()
        {
            InitializeComponent();
            profiles = profileStore.Load();
            ConfigureRuntimeUi();
            RestoreWindowSettings();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            if (checkUpdatesCheckBox.Checked)
            {
                BeginUpdateCheck();
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            SaveWindowSettings();

            if (isProcessing && currentCancellation != null)
            {
                currentCancellation.Cancel();
                KillCurrentWorker();
            }

            if (updateCheckCancellation != null)
            {
                updateCheckCancellation.Cancel();
            }

            DeleteCurrentBatchFolder();
            TempBatchManager.DeleteRoot();

            base.OnFormClosing(e);
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == (Keys.Control | Keys.A))
            {
                SelectAllResultCards();
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void RestoreWindowSettings()
        {
            var settings = Properties.Settings.Default;

            if (settings.WindowSize.Width >= MinimumSize.Width && settings.WindowSize.Height >= MinimumSize.Height)
            {
                StartPosition = FormStartPosition.Manual;
                Size = settings.WindowSize;

                var restoredBounds = new Rectangle(settings.WindowLocation, settings.WindowSize);
                if (Screen.AllScreens.Any(screen => screen.WorkingArea.IntersectsWith(restoredBounds)))
                {
                    Location = settings.WindowLocation;
                }
            }

            if (settings.WindowState == FormWindowState.Maximized)
            {
                WindowState = FormWindowState.Maximized;
            }
        }

        private void SaveWindowSettings()
        {
            var settings = Properties.Settings.Default;
            var bounds = WindowState == FormWindowState.Normal ? Bounds : RestoreBounds;

            if (bounds.Width >= MinimumSize.Width && bounds.Height >= MinimumSize.Height)
            {
                settings.WindowLocation = bounds.Location;
                settings.WindowSize = bounds.Size;
            }

            settings.WindowState = WindowState == FormWindowState.Minimized
                ? FormWindowState.Normal
                : WindowState;
            settings.Save();
        }

        private void ConfigureRuntimeUi()
        {
            BindProfiles(profileStore.LoadLastProfileId());
            profileComboBox.SelectedIndexChanged += ProfileComboBox_SelectedIndexChanged;

            workerComboBox.Items.Clear();
            workerComboBox.Items.AddRange(new object[] { "Auto", "1", "2", "4", "8", "Custom" });
            workerComboBox.SelectedIndex = 0;
            customWorkerCount.Maximum = Math.Max(1, Environment.ProcessorCount * 2);
            customWorkerCount.Value = Math.Max(1, Environment.ProcessorCount - 1);
            customWorkerCount.Enabled = false;

            suppressUpdateCheckSettingSave = true;
            checkUpdatesCheckBox.Checked = Properties.Settings.Default.CheckForUpdates;
            suppressUpdateCheckSettingSave = false;

            processingOverlay.BackColor = Color.FromArgb(235, 255, 255, 255);
            processingOverlay.BringToFront();
            CenterProcessingBox();
            UpdateInstructionLayout();
        }

        private void BindProfiles(string selectedId)
        {
            var id = selectedId;
            if (id == null && profileComboBox != null && profileComboBox.SelectedItem is ResizeProfile)
            {
                id = ((ResizeProfile)profileComboBox.SelectedItem).Id;
            }

            profileComboBox.DataSource = null;
            profileComboBox.DataSource = profiles;

            if (!string.IsNullOrEmpty(id))
            {
                var selected = profiles.FirstOrDefault(p => p.Id == id);
                if (selected != null)
                {
                    profileComboBox.SelectedItem = selected;
                }
            }
        }

        private void AddProfile_Click(object sender, EventArgs e)
        {
            var baseProfile = ((ResizeProfile)profileComboBox.SelectedItem).Clone();
            baseProfile.Id = Guid.NewGuid().ToString("N");
            baseProfile.Name = "Custom " + baseProfile.MaxWidth;
            baseProfile.IsBuiltIn = false;

            using (var dialog = new ProfileEditorDialog(baseProfile))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                profiles.Add(dialog.Profile);
                profileStore.Save(profiles);
                BindProfiles(dialog.Profile.Id);
            }
        }

        private void EditProfile_Click(object sender, EventArgs e)
        {
            var selected = profileComboBox.SelectedItem as ResizeProfile;
            if (selected == null)
            {
                return;
            }

            using (var dialog = new ProfileEditorDialog(selected))
            {
                if (dialog.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                var index = profiles.FindIndex(p => p.Id == selected.Id);
                profiles[index] = dialog.Profile;
                profileStore.Save(profiles);
                BindProfiles(dialog.Profile.Id);
            }
        }

        private void DeleteProfile_Click(object sender, EventArgs e)
        {
            var selected = profileComboBox.SelectedItem as ResizeProfile;
            if (selected == null)
            {
                return;
            }

            if (selected.IsBuiltIn)
            {
                MessageBox.Show(
                    "Built-in profiles can be reset, but not deleted.",
                    "Drop&Resize",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            var answer = MessageBox.Show(
                "Delete this custom profile?",
                "Drop&Resize",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (answer != DialogResult.Yes)
            {
                return;
            }

            profiles.Remove(selected);
            profileStore.Save(profiles);
            BindProfiles(profiles.FirstOrDefault() == null ? null : profiles.First().Id);
        }

        private void ResetProfile_Click(object sender, EventArgs e)
        {
            var selected = profileComboBox.SelectedItem as ResizeProfile;
            if (selected == null)
            {
                return;
            }

            if (!selected.IsBuiltIn)
            {
                MessageBox.Show(
                    "Only built-in profiles can be reset.",
                    "Drop&Resize",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Information);
                return;
            }

            var defaultProfile = profileStore.GetDefaultBuiltIn(selected.Id);
            if (defaultProfile == null)
            {
                return;
            }

            var index = profiles.FindIndex(p => p.Id == selected.Id);
            profiles[index] = defaultProfile;
            profileStore.Save(profiles);
            BindProfiles(defaultProfile.Id);
        }

        private void WorkerComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            customWorkerCount.Enabled = workerComboBox.SelectedItem.ToString() == "Custom";
        }

        private void CheckUpdatesCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (suppressUpdateCheckSettingSave)
            {
                return;
            }

            Properties.Settings.Default.CheckForUpdates = checkUpdatesCheckBox.Checked;
            Properties.Settings.Default.Save();

            if (checkUpdatesCheckBox.Checked)
            {
                BeginUpdateCheck();
            }
            else if (updateCheckCancellation != null)
            {
                updateCheckCancellation.Cancel();
            }
        }

        private void ProfileComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            var selected = profileComboBox.SelectedItem as ResizeProfile;
            if (selected != null)
            {
                profileStore.SaveLastProfileId(selected.Id);
            }
        }

        private void CardsPanel_Resize(object sender, EventArgs e)
        {
            UpdateInstructionLayout();
        }

        private void ProcessingOverlay_Resize(object sender, EventArgs e)
        {
            CenterProcessingBox();
        }

        private void CenterProcessingBox()
        {
            if (processingOverlay == null || processingBox == null)
            {
                return;
            }

            processingBox.Location = new Point(
                Math.Max(0, (processingOverlay.ClientSize.Width - processingBox.Width) / 2),
                Math.Max(0, (processingOverlay.ClientSize.Height - processingBox.Height) / 2));
        }

        private async void ResizeAgain_Click(object sender, EventArgs e)
        {
            var sourceCards = selectedCards.Count > 0
                ? selectedCards
                : cardsPanel.Controls.OfType<ImageCardControl>().ToList();

            var inputFiles = sourceCards
                .Select(c => c.Result.InputPath)
                .Where(ResizeJob.IsSupportedInputFile)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (inputFiles.Count == 0)
            {
                SetStatus("No existing source images are available for resize again.");
                return;
            }

            await StartProcessingAsync(inputFiles);
        }

        private void BeginUpdateCheck()
        {
            if (updateCheckInProgress)
            {
                return;
            }

            if (!checkUpdatesCheckBox.Checked)
            {
                return;
            }

            if (updateCheckCancellation != null)
            {
                updateCheckCancellation.Cancel();
            }

            updateCheckCancellation = new CancellationTokenSource();
            var ignored = CheckForUpdatesAsync(updateCheckCancellation.Token);
        }

        private async Task CheckForUpdatesAsync(CancellationToken cancellationToken)
        {
            updateCheckInProgress = true;

            try
            {
                var update = await updateCheckService.CheckForUpdateAsync(cancellationToken);
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                if (update == null)
                {
                    return;
                }

                SetStatus("Update available: Drop&Resize " + update.VersionText + ".");
                using (var dialog = new UpdateAvailableDialog(update))
                {
                    dialog.ShowDialog(this);
                }
            }
            catch (OperationCanceledException)
            {
            }
            catch (Exception ex)
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    SetStatus("Update check failed: " + ex.Message);
                }
            }
            finally
            {
                if (updateCheckCancellation != null && updateCheckCancellation.Token == cancellationToken)
                {
                    updateCheckCancellation.Dispose();
                    updateCheckCancellation = null;
                }

                updateCheckInProgress = false;
            }
        }

        private void Form1_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        }

        private async void Form1_DragDrop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                return;
            }

            var droppedFiles = ((string[])e.Data.GetData(DataFormats.FileDrop)).ToList();
            var files = droppedFiles
                .Where(ResizeJob.IsSupportedInputFile)
                .Where(path => !IsInCurrentBatchFolder(path))
                .ToList();

            if (files.Count == 0)
            {
                SetStatus(droppedFiles.Any(IsInCurrentBatchFolder)
                    ? "Dropped files from the current output batch were ignored."
                    : "No supported image files were dropped.");
                return;
            }

            await StartProcessingAsync(files);
        }

        private async Task StartProcessingAsync(List<string> files)
        {
            if (isProcessing)
            {
                var answer = MessageBox.Show(
                    "Cancel current processing and start a new batch?",
                    "Drop&Resize",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question);

                if (answer != DialogResult.Yes)
                {
                    return;
                }

                currentCancellation.Cancel();
                KillCurrentWorker();
            }

            var profile = (ResizeProfile)profileComboBox.SelectedItem;
            var workerCount = GetWorkerCount();
            SetStatus("Processing " + files.Count + " image(s) with " + workerCount + " worker(s)...");
            ShowProcessingOverlay(files.Count, workerCount);
            await Task.Delay(75);

            var batchId = ++currentBatchId;
            CleanupCurrentBatchFolder();

            currentBatchFolder = TempBatchManager.Create();
            currentCancellation = new CancellationTokenSource();
            var batchCancellation = currentCancellation;
            isProcessing = true;
            var jobPath = Path.Combine(currentBatchFolder.BatchFolder, "job.xml");
            var resultPath = Path.Combine(currentBatchFolder.BatchFolder, "results.xml");
            SaveWorkerJob(new WorkerJob
            {
                InputFiles = files,
                Profile = profile,
                OutputFolder = currentBatchFolder.BatchFolder,
                WorkerCount = workerCount,
                ResultPath = resultPath
            }, jobPath);

            try
            {
                resultFlushTimer.Start();
                await RunWorkerProcessAsync(jobPath, batchId, batchCancellation.Token);

                foreach (var result in LoadWorkerResults(resultPath))
                {
                    QueueResult(result.ToResizeResult(), batchId);
                }
            }
            catch (OperationCanceledException)
            {
                SetStatusSafe("Processing canceled.", batchId);
            }
            catch (Exception ex)
            {
                SetStatusSafe("Processing failed: " + ex.Message, batchId);
            }
            finally
            {
                if (currentCancellation == batchCancellation)
                {
                    FlushPendingResults(int.MaxValue);
                    resultFlushTimer.Stop();
                    HideProcessingOverlay();
                    isProcessing = false;
                    if (!batchCancellation.IsCancellationRequested)
                    {
                        SetStatusSafe(
                            "Batch complete. Drag selected completed cards out to copy resized files.",
                            batchId);
                    }
                }
            }
        }

        private async Task RunWorkerProcessAsync(string jobPath, int batchId, CancellationToken cancellationToken)
        {
            var workerPath = ExtractEmbeddedWorkerExecutable();

            var errors = new StringBuilder();
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = workerPath,
                    Arguments = "\"" + jobPath + "\"",
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                },
                EnableRaisingEvents = true
            };

            var completed = new TaskCompletionSource<int>();

            process.OutputDataReceived += (sender, args) =>
            {
                if (!string.IsNullOrWhiteSpace(args.Data))
                {
                    HandleWorkerOutput(args.Data, batchId);
                }
            };
            process.ErrorDataReceived += (sender, args) =>
            {
                if (!string.IsNullOrWhiteSpace(args.Data))
                {
                    errors.AppendLine(args.Data);
                }
            };
            process.Exited += (sender, args) => completed.TrySetResult(process.ExitCode);

            currentWorkerProcess = process;
            process.Start();
            try
            {
                process.PriorityClass = ProcessPriorityClass.BelowNormal;
            }
            catch
            {
                // Priority changes can fail under restricted process permissions.
            }

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            using (cancellationToken.Register(() =>
            {
                try
                {
                    if (!process.HasExited)
                    {
                        process.Kill();
                    }
                }
                catch
                {
                    // The process may exit between HasExited and Kill.
                }
            }))
            {
                var exitCode = await completed.Task;
                if (cancellationToken.IsCancellationRequested)
                {
                    throw new OperationCanceledException(cancellationToken);
                }

                if (exitCode != 0)
                {
                    throw new InvalidOperationException(errors.Length == 0
                        ? "Worker exited with code " + exitCode + "."
                        : errors.ToString().Trim());
                }
            }

            process.Dispose();
            if (currentWorkerProcess == process)
            {
                currentWorkerProcess = null;
            }
        }

        private static string ExtractEmbeddedWorkerExecutable()
        {
            const string resourceName = "Drop&Resize.Worker.exe";
            var targetFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "Drop&Resize",
                "Worker");
            Directory.CreateDirectory(targetFolder);

            var targetPath = Path.Combine(targetFolder, resourceName);
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
            {
                if (stream != null)
                {
                    using (var output = File.Create(targetPath))
                    {
                        stream.CopyTo(output);
                    }

                    return targetPath;
                }
            }

            var fallbackPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, resourceName);
            if (File.Exists(fallbackPath))
            {
                return fallbackPath;
            }

            throw new FileNotFoundException("Worker executable was not found.", fallbackPath);
        }

        private void HandleWorkerOutput(string line, int batchId)
        {
            var parts = line.Split(' ');
            if (parts.Length == 3 && parts[0] == "PROGRESS")
            {
                int done;
                int total;
                if (int.TryParse(parts[1], out done) && int.TryParse(parts[2], out total))
                {
                    QueueProgress(done, total, batchId);
                }
            }
        }

        private static void SaveWorkerJob(WorkerJob job, string path)
        {
            using (var stream = File.Create(path))
            {
                var serializer = new XmlSerializer(typeof(WorkerJob));
                serializer.Serialize(stream, job);
            }
        }

        private static List<WorkerResult> LoadWorkerResults(string path)
        {
            using (var stream = File.OpenRead(path))
            {
                var serializer = new XmlSerializer(typeof(List<WorkerResult>));
                return (List<WorkerResult>)serializer.Deserialize(stream);
            }
        }

        private void KillCurrentWorker()
        {
            try
            {
                if (currentWorkerProcess != null && !currentWorkerProcess.HasExited)
                {
                    currentWorkerProcess.Kill();
                }
            }
            catch
            {
                // Cancellation and shutdown should continue even if the worker already exited.
            }
        }

        private bool IsInCurrentBatchFolder(string path)
        {
            if (currentBatchFolder == null || string.IsNullOrEmpty(currentBatchFolder.BatchFolder))
            {
                return false;
            }

            var folder =
                Path.GetFullPath(currentBatchFolder.BatchFolder)
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) +
                Path.DirectorySeparatorChar;

            var fullPath = Path.GetFullPath(path);
            return fullPath.StartsWith(folder, StringComparison.OrdinalIgnoreCase);
        }

        private int GetWorkerCount()
        {
            var selected = workerComboBox.SelectedItem.ToString();
            if (selected == "Auto")
            {
                return Math.Max(1, Environment.ProcessorCount - 1);
            }

            if (selected == "Custom")
            {
                return (int)customWorkerCount.Value;
            }

            return int.Parse(selected);
        }

        private void ClearCurrentBatch()
        {
            ResizeResult ignored;
            while (pendingResults.TryDequeue(out ignored))
            {
                DisposeResultThumbnail(ignored);
            }

            selectedCards.Clear();
            foreach (Control control in cardsPanel.Controls.Cast<Control>().Where(c => c != instructionLabel).ToList())
            {
                cardsPanel.Controls.Remove(control);
                control.Dispose();
            }

            if (instructionLabel.Parent != cardsPanel)
            {
                cardsPanel.Controls.Add(instructionLabel);
            }

            UpdateInstructionLayout();
        }

        private void CleanupCurrentBatchFolder()
        {
            ClearCurrentBatch();
            DeleteCurrentBatchFolder();
        }

        private void DeleteCurrentBatchFolder()
        {
            if (currentBatchFolder != null)
            {
                currentBatchFolder.Delete();
                currentBatchFolder = null;
            }
        }

        private void QueueResult(ResizeResult result, int batchId)
        {
            if (batchId != currentBatchId)
            {
                DisposeResultThumbnail(result);
                return;
            }

            pendingResults.Enqueue(result);
        }

        private void QueueProgress(int done, int total, int batchId)
        {
            pendingProgressDone = done;
            pendingProgressTotal = total;
            pendingProgressBatchId = batchId;

            if (Interlocked.Exchange(ref progressUpdatePosted, 1) == 0)
            {
                BeginInvoke(new Action(ApplyPendingProgress));
            }
        }

        private void ResultFlushTimer_Tick(object sender, EventArgs e)
        {
            ApplyPendingProgress();
        }

        private void ApplyPendingProgress()
        {
            Interlocked.Exchange(ref progressUpdatePosted, 0);

            if (pendingProgressBatchId == currentBatchId && pendingProgressTotal > 0)
            {
                SetStatus("Processed " + pendingProgressDone + " of " + pendingProgressTotal + " image(s).");
                UpdateProcessingOverlay(pendingProgressDone, pendingProgressTotal);
            }
        }

        private void FlushPendingResults(int maxCount)
        {
            var added = 0;
            ResizeResult result;
            cardsPanel.SuspendLayout();
            while (added < maxCount && pendingResults.TryDequeue(out result))
            {
                AddResultCard(result);
                added++;
            }
            cardsPanel.ResumeLayout();
        }

        private void AddResultCard(ResizeResult result)
        {
            if (instructionLabel.Parent == cardsPanel)
            {
                cardsPanel.Controls.Remove(instructionLabel);
            }

            var card = new ImageCardControl(result);
            card.CardMouseDown += ImageCard_MouseDown;
            card.CardMouseMove += ImageCard_MouseMove;
            card.CardMouseUp += ImageCard_MouseUp;
            card.SetDropHandlers(Form1_DragEnter, Form1_DragDrop);
            cardsPanel.Controls.Add(card);
        }

        private static void DisposeResultThumbnail(ResizeResult result)
        {
            if (result != null && result.Thumbnail != null)
            {
                result.Thumbnail.Dispose();
                result.Thumbnail = null;
            }
        }

        private void CardsPanel_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left || instructionLabel.Parent == cardsPanel)
            {
                return;
            }

            marqueeSelecting = true;
            marqueeAdditive = (ModifierKeys & Keys.Control) == Keys.Control;
            marqueeStartContentPoint = ToContentPoint(e.Location);
            lastMarqueeMousePoint = e.Location;
            cardsPanel.SelectionRectangle = Rectangle.Empty;
            cardsPanel.Capture = true;
            marqueeScrollTimer.Start();

            if (!marqueeAdditive)
            {
                ClearSelection();
            }
        }

        private void CardsPanel_MouseMove(object sender, MouseEventArgs e)
        {
            if (!marqueeSelecting)
            {
                return;
            }

            lastMarqueeMousePoint = e.Location;
            UpdateMarqueeSelection(e.Location);
        }

        private void CardsPanel_MouseUp(object sender, MouseEventArgs e)
        {
            if (!marqueeSelecting)
            {
                return;
            }

            marqueeSelecting = false;
            cardsPanel.Capture = false;
            marqueeScrollTimer.Stop();
            cardsPanel.SelectionRectangle = Rectangle.Empty;
        }

        private void MarqueeScrollTimer_Tick(object sender, EventArgs e)
        {
            if (!marqueeSelecting)
            {
                marqueeScrollTimer.Stop();
                return;
            }

            var scrolled = ScrollForMarqueePoint(lastMarqueeMousePoint);
            if (scrolled)
            {
                UpdateMarqueeSelection(lastMarqueeMousePoint);
            }
        }

        private bool ScrollForMarqueePoint(Point point)
        {
            const int edge = 28;
            const int step = 24;

            var currentY = -cardsPanel.AutoScrollPosition.Y;
            var nextY = currentY;

            if (point.Y < 0)
            {
                nextY = Math.Max(0, currentY - step);
            }
            else if (point.Y > cardsPanel.ClientSize.Height)
            {
                nextY = Math.Min(cardsPanel.VerticalScroll.Maximum, currentY + step);
            }
            else if (point.Y < edge)
            {
                nextY = Math.Max(0, currentY - step);
            }
            else if (point.Y > cardsPanel.ClientSize.Height - edge)
            {
                nextY = Math.Min(cardsPanel.VerticalScroll.Maximum, currentY + step);
            }

            if (nextY == currentY)
            {
                return false;
            }

            cardsPanel.AutoScrollPosition = new Point(-cardsPanel.AutoScrollPosition.X, nextY);
            return true;
        }

        private void UpdateMarqueeSelection(Point mousePoint)
        {
            var currentContentPoint = ToContentPoint(mousePoint);
            var contentRectangle = MakeRectangle(marqueeStartContentPoint, currentContentPoint);
            var clientRectangle = ContentToClientRectangle(contentRectangle);
            cardsPanel.SelectionRectangle = clientRectangle;
            ApplyMarqueeSelection(clientRectangle, marqueeAdditive);
        }

        private void ImageCard_MouseDown(object sender, MouseEventArgs e)
        {
            var card = (ImageCardControl)sender;
            if (!card.Result.Success || e.Button != MouseButtons.Left)
            {
                return;
            }

            dragStartCard = card;
            dragStartPoint = e.Location;

            var ctrl = (ModifierKeys & Keys.Control) == Keys.Control;
            if (ctrl)
            {
                ToggleCardSelection(card);
                return;
            }

            if (!selectedCards.Contains(card))
            {
                SelectOnly(card);
            }
        }

        private void ImageCard_MouseMove(object sender, MouseEventArgs e)
        {
            if (dragStartCard == null || e.Button != MouseButtons.Left)
            {
                return;
            }

            if (Math.Abs(e.X - dragStartPoint.X) < SystemInformation.DragSize.Width / 2 &&
                Math.Abs(e.Y - dragStartPoint.Y) < SystemInformation.DragSize.Height / 2)
            {
                return;
            }

            var dragCard = dragStartCard;
            dragStartCard = null;

            if (!selectedCards.Contains(dragCard))
            {
                SelectOnly(dragCard);
            }

            DragSelectedCards();
        }

        private void ImageCard_MouseUp(object sender, MouseEventArgs e)
        {
            dragStartCard = null;
        }

        private void SelectOnly(ImageCardControl card)
        {
            ClearSelection();
            selectedCards.Add(card);
            card.IsSelected = true;
        }

        private void ClearSelection()
        {
            foreach (var selected in selectedCards.ToList())
            {
                selected.IsSelected = false;
            }

            selectedCards.Clear();
        }

        private void SelectAllResultCards()
        {
            ClearSelection();

            foreach (var card in cardsPanel.Controls.OfType<ImageCardControl>().Where(card => card.Result.Success))
            {
                selectedCards.Add(card);
                card.IsSelected = true;
            }
        }

        private void ToggleCardSelection(ImageCardControl card)
        {
            if (selectedCards.Contains(card))
            {
                selectedCards.Remove(card);
                card.IsSelected = false;
                return;
            }

            selectedCards.Add(card);
            card.IsSelected = true;
        }

        private void DragSelectedCards()
        {
            var paths = selectedCards
                .Where(c => c.Result.Success && !string.IsNullOrEmpty(c.Result.OutputPath))
                .Select(c => c.Result.OutputPath)
                .ToArray();

            if (paths.Length == 0)
            {
                return;
            }

            var data = new DataObject();
            data.SetData(DataFormats.FileDrop, paths);
            DoDragDrop(data, DragDropEffects.Copy);
        }

        private void ApplyMarqueeSelection(Rectangle rectangle, bool additive)
        {
            var cards = cardsPanel.Controls.OfType<ImageCardControl>()
                .Where(card => card.Result.Success)
                .ToList();

            if (!additive)
            {
                foreach (var card in cards)
                {
                    var selected = rectangle.IntersectsWith(card.Bounds);
                    card.IsSelected = selected;

                    if (selected && !selectedCards.Contains(card))
                    {
                        selectedCards.Add(card);
                    }
                    else if (!selected)
                    {
                        selectedCards.Remove(card);
                    }
                }

                return;
            }

            foreach (var card in cards.Where(card => rectangle.IntersectsWith(card.Bounds)))
            {
                if (!selectedCards.Contains(card))
                {
                    selectedCards.Add(card);
                    card.IsSelected = true;
                }
            }
        }

        private static Rectangle MakeRectangle(Point start, Point end)
        {
            return new Rectangle(
                Math.Min(start.X, end.X),
                Math.Min(start.Y, end.Y),
                Math.Abs(start.X - end.X),
                Math.Abs(start.Y - end.Y));
        }

        private Point ToContentPoint(Point clientPoint)
        {
            return new Point(
                clientPoint.X - cardsPanel.AutoScrollPosition.X,
                clientPoint.Y - cardsPanel.AutoScrollPosition.Y);
        }

        private Rectangle ContentToClientRectangle(Rectangle contentRectangle)
        {
            return new Rectangle(
                contentRectangle.X + cardsPanel.AutoScrollPosition.X,
                contentRectangle.Y + cardsPanel.AutoScrollPosition.Y,
                contentRectangle.Width,
                contentRectangle.Height);
        }

        private void UpdateInstructionLayout()
        {
            if (instructionLabel == null || cardsPanel == null || instructionLabel.Parent != cardsPanel)
            {
                return;
            }

            instructionLabel.Width = Math.Max(260, cardsPanel.ClientSize.Width - 24);
            instructionLabel.Height = Math.Max(160, cardsPanel.ClientSize.Height - 28);
        }

        private void SetStatusSafe(string text, int batchId)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string, int>(SetStatusSafe), text, batchId);
                return;
            }

            if (batchId != currentBatchId)
            {
                return;
            }

            SetStatus(text);
        }

        private void SetStatus(string text)
        {
            statusLabel.Text = text;
        }

        private void ShowProcessingOverlay(int total, int workerCount)
        {
            processingProgressBar.Maximum = Math.Max(1, total);
            processingProgressBar.Value = 0;
            processingLabel.Text = "Processing 0 of " + total + " image(s) with " + workerCount + " worker(s)...";
            processingOverlay.Visible = true;
            processingOverlay.BringToFront();
            processingOverlay.Refresh();
        }

        private void UpdateProcessingOverlay(int done, int total)
        {
            if (!processingOverlay.Visible)
            {
                return;
            }

            processingProgressBar.Maximum = Math.Max(1, total);
            processingProgressBar.Value = Math.Max(0, Math.Min(processingProgressBar.Maximum, done));
            processingLabel.Text = "Processing " + done + " of " + total + " image(s)...";
        }

        private void HideProcessingOverlay()
        {
            processingOverlay.Visible = false;
        }
    }
}
