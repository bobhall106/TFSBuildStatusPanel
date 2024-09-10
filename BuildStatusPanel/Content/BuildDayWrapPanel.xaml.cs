using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using BuildStatusPanel.Model;

namespace BuildStatusPanel.Content
{
    /// <summary>
    /// Interaction logic for BuildDayWrapPanel.xaml
    /// </summary>
    public partial class BuildDayWrapPanel : Border
    {

        double _successRate = 100;

        public DateTime Date { get; set; }
        public double SuccessRate
        {
            get
            {
                return _successRate;
            }
            set
            {
                _successRate = value;
                RedGradientStop.Offset = 1.0 - _successRate-0.05;
                GreenGradientStop.Offset = 1.0 - _successRate+0.05;
                ToolTip = string.Format("{0}% successful for {1}", (int)(_successRate * 100), DayLabel.DayText);
            }
        }
        public BuildDayWrapPanel()
        {
            InitializeComponent();
        }
        public BuildDayWrapPanel(DateTime currentDay, double panelSize, bool isHorizontal=true)
        {
            string text = currentDay.ToString("dddd dd/MM");
            Date = currentDay.Date;
            InitializeComponent();
            this.DayLabel.Content = text;
            this.DayLabel.DayText = text;
            if (isHorizontal)
            {
                Height = panelSize + 10;
                Panel.Height = panelSize + 5;
                DayLabel.Width = panelSize;
            }
            else
            {
                Width = panelSize + 10;
                Panel.Width = panelSize + 5;
                Panel.VerticalAlignment = VerticalAlignment.Top;
                DayLabel.Width = panelSize;
            }
        }

        /// <summary>
        /// Update an existing Build with the current definition
        /// </summary>
        /// <param name="upd"></param>
        internal void UpdateExistingBuild(BuildDetails upd)
        {
            foreach (object child in Panel.Children)
            {
                if (child is BuildDetailsButton)
                {
                    BuildDetailsButton button = child as BuildDetailsButton;
                    if ((button != null) && (button.BuildDetails!=null ) && (button.BuildDetails.BuildNumber== upd.BuildNumber))
                    {
                        button.UpdateBuildDetails(upd);
                    }
                }
            }
        }

        /// <summary>
        /// Add a new button to the panel
        /// </summary>
        /// <param name="newBuild"></param>
        /// <param name="maxBuildTime"></param>
        /// <param name="maxButtonSize"></param>
        /// <param name="insertAtStart"></param>
        internal void AddNewBuild(BuildDetails newBuild, double maxBuildTime, double maxButtonSize, bool insertAtStart=false)
        {
            foreach (object child in Panel.Children)
            {
                if (child is BuildDetailsButton)
                {
                    BuildDetailsButton button = child as BuildDetailsButton;
                    if ((button != null) && (button.BuildDetails != null) && (button.BuildDetails.BuildNumber == newBuild.BuildNumber))
                    {
                        UpdateExistingBuild(newBuild);
                        return;
                    }
                }
            }
            BuildDetailsButton bdButton = new BuildDetailsButton(newBuild, maxBuildTime, maxButtonSize, false);
            if(insertAtStart)
                Panel.Children.Insert(1, bdButton);//insert after the label at the top
            else
                Panel.Children.Add(bdButton);
            Panel.UpdateLayout();
        }
    }
}
