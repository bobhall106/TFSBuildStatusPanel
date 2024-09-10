using System.Collections.Generic;
using System.Windows.Forms.DataVisualization.Charting;
using System.Windows;
using System;
using BuildStatusPanel.Model;
using System.Linq;
using System.Drawing;
using System.Windows.Threading;
using System.Threading;
using System.IO;
using System.Windows.Media.Imaging;

namespace BuildStatusPanel.Content
{
    /// <summary>
    /// Interaction logic for CheckinsGraphWindow.xaml
    /// </summary>
    public partial class CheckinTimesGraphWindow : Window
    {
        private bool isClosing=false;

        public bool Is3D { get; set; }
        public bool IsCheckins { get; set; }
        public bool IsFiles { get; set; }

        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public CheckinTimesGraphWindow()
        {
            Is3D = true;
            IsFiles = true;
            IsCheckins = true;
            InitializeComponent();
            ToDate = DateTime.Now;
            DateTime prev = ToDate;
            FromDate=prev.AddDays(-14);
            FromDatePicker.DisplayDate = FromDate;
            ToDatePicker.DisplayDate = ToDate;
            FromDatePicker.SelectedDate = FromDate;
            ToDatePicker.SelectedDate = ToDate;
            UpdateLayout();
            this.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() =>
            {
                Refresh();
            }));
        }

        /// <summary>
        /// get teh checngests from tfs and load them into a key/value list
        /// </summary>
        void LoadChangesets()
        {
            this.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() =>
            {
                RefeshButton.IsEnabled = false;
                Chart chart = this.FindName("CheckinsWinformChart") as Chart;
                chart.Titles["title2"].Text = "searching...";
                chart.DataSource = null;
                chart.Series["Number Of Checkins"].Points.Clear();
                chart.Series["Number Of Files"].Points.Clear();
                chart.Invalidate();
                chart.Update();
            }));
            List<string> hours = new List<string>();
            List<int> previousNumCheckins= new List<int>();
            List<int> previousFileCount = new List<int>();
            for (int hour = 0; hour < 24; hour++)
            {
                string hourLabel = string.Format("{0}:00-{1}:00", hour.ToString("D2"), (hour + 1).ToString("D2"));
                hours.Add(hourLabel);
                previousNumCheckins.Add(0);
                previousFileCount.Add(0);
            }
            List<KeyValuePair<string, int>> previousValueList = new List<KeyValuePair<string, int>>();
            TFSBuildStatusHelper buildHelper = new TFSBuildStatusHelper();
            buildHelper.Init(MainWindow.DefaultSettings.TFSServerURIPath, MainWindow.DefaultSettings.DefaultProject);
            string path = string.Format("$/{0}", MainWindow.DefaultSettings.DefaultProject);
            DateTime from = FromDate.Date;
            DateTime end = ToDate.Date.AddDays(1);
            DateTime thisDay = from;
            int totalCheckinCount = 0;
            int totalFileCount = 0;
            while (!isClosing &&(thisDay< end))
            {
                List<int> numCheckins = new List<int>();
                List<int> fileCounts = new List<int>();
                List<string> paths = new List<string>();
                paths.Add(path);
                int addDays = (end.Date - thisDay.Date).Days > 7 ? 7 : (end.Date - thisDay.Date).Days;
                List<ChangeSetItem> changesets = buildHelper.GetChangesets(paths, thisDay, thisDay.AddDays(addDays), 1000);
                totalCheckinCount += changesets.Count;
                int maxfileCount = 10;
                for (int hour = 0; hour < 24; hour++)
                {
                    int count = (from csi in changesets where csi.CreationDateTime.Hour == hour select csi).Count();
                    int existing = previousNumCheckins[hour];
                    numCheckins.Add(existing + count);
                    int fileCount = (from csi in changesets where csi.CreationDateTime.Hour == hour select csi.ChangesCount).Sum();
                    totalFileCount += fileCount;
                    int existingFileCount = previousFileCount[hour];
                    fileCounts.Add(existingFileCount + fileCount);
                    maxfileCount = (existingFileCount + fileCount) > maxfileCount ? existingFileCount + fileCount : maxfileCount;
                }
                previousNumCheckins = numCheckins;
                previousFileCount = fileCounts;
                this.Dispatcher.BeginInvoke(DispatcherPriority.Normal, new Action(() =>
                {
                    Chart chart = this.FindName("CheckinsWinformChart") as Chart;
                    chart.Titles["title2"].Text = string.Format("{3} files in {2} checkins between {0} and {1}",
                    FromDate.ToShortDateString(),
                    thisDay.ToShortDateString(),
                    totalCheckinCount, totalFileCount);
                    if(IsCheckins)
                        chart.Series["Number Of Checkins"].Points.DataBindXY(hours, numCheckins);
                    if (IsFiles)
                    {
                        chart.Series["Number Of Files"].Points.DataBindXY(hours, fileCounts);
                        chart.ChartAreas["chartArea1"].AxisY.Maximum = maxfileCount*2;//try to ensure the files are smaller visually than the checkins behind in 3d mode
                    }
                    chart.Invalidate();
                    chart.Update();
                    UpdateLayout();
                }));
                thisDay=thisDay.AddDays(addDays);
                Thread.Sleep(0);
            };
            this.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() =>
            {
                RefeshButton.IsEnabled = true;
            }));
        }

        /// <summary>
        /// Set the basic binding for the main series
        /// </summary>
        private void SetSeriesBinding()
        {
            Chart chart = this.FindName("CheckinsWinformChart") as Chart;
            chart.Series["Number Of Checkins"].XValueMember = "Key";
            chart.Series["Number Of Checkins"].YValueMembers = "Value";
            chart.Series["Number Of Checkins"].ToolTip = string.Format("#VALY Changesets (#PERCENT) checked in between #AXISLABEL from {0} until {1}",
                FromDate.ToShortDateString(),
                ToDate.ToShortDateString());
            chart.Series["Number Of Checkins"].LegendToolTip = "Number of checkins accross all branches";
            chart.Series["Number Of Checkins"].YAxisType = AxisType.Secondary;

            chart.Series["Number Of Files"].ToolTip = string.Format("#VALY files (#PERCENT) checked in between #AXISLABEL from {0} until {1}",
                FromDate.ToShortDateString(),
                ToDate.ToShortDateString());
            chart.Series["Number Of Files"].LegendToolTip = "Number of files checked in accross all branches";
            
        }

        /// <summary>
        /// set the styleing on the chart
        /// </summary>
        private void SetChartStyle()
        {
            Chart chart = this.FindName("CheckinsWinformChart") as Chart;
            //chart.Series["Number Of Checkins"].CustomProperties = "PieDrawingStyle=Concave";
            chart.BorderSkin = new BorderSkin();
            chart.BorderSkin.SkinStyle = BorderSkinStyle.Emboss;
            chart.BorderlineDashStyle = ChartDashStyle.Solid;
            chart.BackGradientStyle = GradientStyle.TopBottom;
            chart.BackColor = Color.LightBlue;
            chart.BorderlineColor = Color.Gray;
            chart.BorderlineWidth = 2;
            chart.ChartAreas["chartArea1"].Area3DStyle.Enable3D = Is3D;
            chart.Titles["title"].Font = new Font("Arial", 14);
            chart.ChartAreas["chartArea1"].AxisY.Title = "# files";
            chart.ChartAreas["chartArea1"].AxisX.Title = "Time range";
            chart.ChartAreas["chartArea1"].AxisY2.Title = "# checkins";
            chart.ChartAreas["chartArea1"].AxisY2.Enabled = AxisEnabled.True;
            chart.ChartAreas["chartArea1"].AxisY.IsLogarithmic = false;
            chart.ChartAreas["chartArea1"].AxisY2.IsLogarithmic = false;
            chart.ChartAreas["chartArea1"].AxisX.IsLabelAutoFit = true;
            chart.ChartAreas["chartArea1"].AxisX.LabelAutoFitStyle = LabelAutoFitStyles.LabelsAngleStep30;
            chart.ChartAreas["chartArea1"].AxisX.LabelStyle.Enabled = true;
            AddChartContextMenu(chart);
        }

        private void AddChartContextMenu(Chart chart)
        {
            chart.ContextMenu = new System.Windows.Forms.ContextMenu();
            System.Windows.Forms.MenuItem mnuItemNew = new System.Windows.Forms.MenuItem();
            mnuItemNew.Text = "Copy Chart";
            mnuItemNew.Click += MnuItemNew_Click;
            chart.ContextMenu.MenuItems.Add(mnuItemNew);
        }

        private void MnuItemNew_Click(object sender, EventArgs e)
        {
            CopyChart_Click(sender, null);
        }

        /// <summary>
        /// user wants a refresh
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Refesh_Click(object sender, RoutedEventArgs e)
        {
            RefeshButton.IsEnabled = false;
            this.Dispatcher.BeginInvoke(DispatcherPriority.ApplicationIdle, new Action(() =>
            {
                Refresh();
            }));
        }

        //refresh the data
        private void Refresh()
        {
            RefeshButton.IsEnabled = false;
            SetSeriesBinding();
            SetChartStyle();
            Thread nodeThread = new Thread(() => LoadChangesets());
            nodeThread.Start();
        }

        private void checkinTimesGraphsWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            isClosing = true;
        }

        private void CopyChart_Click(object sender, RoutedEventArgs e)
        {
            Chart chart = this.FindName("CheckinsWinformChart") as Chart;
            if (chart != null)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    chart.SaveImage(ms, ChartImageFormat.Bmp);
                    Bitmap bmp = new Bitmap(ms);
                    BitmapSource bm = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                                   bmp.GetHbitmap(),
                                   IntPtr.Zero,
                                   System.Windows.Int32Rect.Empty,
                                   BitmapSizeOptions.FromWidthAndHeight(bmp.Width, bmp.Height));
                    Clipboard.SetImage(bm);
                }
            }
        }
    }
}
