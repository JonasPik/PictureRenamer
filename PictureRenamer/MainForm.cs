using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace PictureRenamer
{
    public partial class MainForm : Form
    {
        public string FolderPath;

        /// <summary>
        /// Constructor
        /// </summary>
        public MainForm()
        {
            InitializeComponent();

            StartButton.Enabled = false;

            CancelBtn.Enabled = false;
        }

        /// <summary>
        /// Start Buttton Click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StartButton_Click(object sender, EventArgs e)
        {
            if (!myWorker.IsBusy)
            {
                StartButton.Enabled = false;

                ChooseFolderButton.Enabled = false;

                ResultBox.Text = string.Empty;

                CancelBtn.Enabled = true;

                myWorker.RunWorkerAsync(FolderPath);
            }
        }

        /// <summary>
        /// Choose Folder Button Click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ChooseFolderButton_Click(object sender, EventArgs e)
        {
            var dialog = new FolderBrowserDialog
            {
                SelectedPath = !string.IsNullOrEmpty(FolderPath) ? FolderPath : @"C:\"
            };

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                FolderPathLabel.Text = dialog.SelectedPath;

                FolderPath = dialog.SelectedPath;

                StartButton.Enabled = true;

            }
        }

        /// <summary>
        /// Cancel Button Click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CancelBtn_Click(object sender, EventArgs e)
        {
            myWorker.CancelAsync();
        }

        /// <summary>
        /// Get Date Taken property from image
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private string GetDateTakenFromImage(string path)
        {
            try
            {
                var myImage = Image.FromFile(path);

                var propItem = myImage.GetPropertyItem(36867);

                var datetime = new Regex(":").Replace(Encoding.UTF8.GetString(propItem.Value), "-", 2).Replace(':', '_');

                myImage.Dispose();

                return datetime.Substring(0, datetime.Length - 1);
            }
            catch (Exception ex)
            {
                return string.Empty;
            }
        }

        /// <summary>
        /// Do Work
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void myWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            var sendingWorker = (BackgroundWorker)sender;

            var files = new DirectoryInfo(e.Argument.ToString()).GetFiles();

            ProcessList(files, sendingWorker, e);

            files = null;

            e.Result = "Complete";
        }

        /// <summary>
        /// Worker Work Completed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void myWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (!e.Cancelled && e.Error == null)
            {
                StatusLabel.Text = "Done";

                ResultBox.Text += e.Result.ToString();
            }
            else if (e.Cancelled)
            {
                StatusLabel.Text = "Cancelled";
            }
            else
            {
                StatusLabel.Text = "An error has occurred";
            }

            // Re-enable the start button
            StartButton.Enabled = true;

            CancelBtn.Enabled = false;

            ChooseFolderButton.Enabled = true;
        }

        /// <summary>
        /// Worker Progress Changed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected void myWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            //Show the progress to the user based on the input we got from the background worker
            StatusLabel.Text = $"Photo Processed: {e.ProgressPercentage}...";

            ResultBox.Text = (e.UserState.ToString());
        }

        /// <summary>
        /// Proccess file
        /// </summary>
        /// <param name="file"></param>
        /// <param name="sb"></param>
        public void ProcessFile(FileInfo file, StringBuilder sb)
        {
            var newName = GetDateTakenFromImage(file.FullName);

            var ext = "." + file.Name.Split('.')[1];

            // If "Date Taken" property is not found, then don't rename
            if (!string.IsNullOrEmpty(newName))
            {
                var destination = file.DirectoryName + "\\" + newName;

                while (File.Exists(destination + ext))
                {
                    destination += "_0";
                }

                File.Move(file.FullName, destination + ext);
            }
            else
            {
                sb.AppendLine($"{file.Name} Was NOT renamed.");
            }

            sb.AppendLine($"{file.Name} was processed.");
        }

        /// <summary>
        /// Process file list
        /// </summary>
        /// <param name="files"></param>
        /// <param name="worker"></param>
        /// <param name="e"></param>
        public void ProcessList(FileInfo[] files, BackgroundWorker worker, DoWorkEventArgs e)
        {
            var count = 0;

            var sb = new StringBuilder();

            foreach (var file in files)
            {
                if (!worker.CancellationPending)
                {
                    if (file.Name.EndsWith(".jpg", StringComparison.InvariantCultureIgnoreCase)
                        || file.Name.EndsWith(".jpeg", StringComparison.InvariantCultureIgnoreCase)
                        || file.Name.EndsWith(".png", StringComparison.InvariantCultureIgnoreCase))
                    {
                        ProcessFile(file, sb);

                        count++;

                        //Report our progress to the main thread
                        worker.ReportProgress(count, sb);
                    }

                }
                else
                {
                    //If a cancellation request is pending, assign this flag a value of true
                    e.Cancel = true; 
                    break;
                }

            }
        }

    }
}
