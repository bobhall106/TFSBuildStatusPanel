using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BuildStatusPanel.Model;

namespace BuildStatusPanel.Content
{
    /// <summary>
    /// Interaction logic for BuildDetailsButton.xaml
    /// </summary>
    public partial class BuildDetailsButton : Button
    {
        private readonly bool _isVertical;
        private readonly double _MaxBuildTime;
        private readonly double _MaxButtonSize;
        private static BuildDetailsButton _previousSelectedButton=null;
        #region Properties

        /// <summary>
        /// The build details from tfs
        /// </summary>
        public BuildDetails BuildDetails { get; set; }

        #endregion Properties

        #region Constructors

        /// <summary>
        /// default ctor
        /// </summary>
        public BuildDetailsButton()
        {
            InitializeComponent();
        }

        /// <summary>
        /// ctor with details
        /// </summary>
        /// <param name="details"></param>
        /// <param name="buttonSize"></param>
        /// <param name="isVertical"></param>
        public BuildDetailsButton(BuildDetails details, double maxBuildTime , double maxButtonSize, bool isVertical=true)
        {
            _MaxBuildTime = maxBuildTime;
            _MaxButtonSize = maxButtonSize;
            _isVertical = isVertical;
            InitializeComponent();
            Init(details);
            Click += BuildDetailsButton_Activated;
            GotFocus += BuildDetailsButton_Activated;
        }

        #endregion Constructors

        #region Methods

        double ButtonSize()
        {
            double buttonSize = BuildDetails.BuildTimeMins * _MaxButtonSize / _MaxBuildTime > _MaxButtonSize ? _MaxButtonSize : BuildDetails.BuildTimeMins * _MaxButtonSize / _MaxBuildTime;//max height
            buttonSize = buttonSize < 8 ? 8 : buttonSize;//min size
            return buttonSize;
        }
        /// <summary>
        /// Init the control values
        /// </summary>
        /// <param name="details"></param>
        /// <param name="buttonSize"></param>
        public void Init(BuildDetails details)
        {
            BuildDetails = details;
            double buttonSize= ButtonSize();
            Width = _isVertical? 15: (buttonSize<30 ? 30 : buttonSize);//have at least 30 width for horz
            Height = _isVertical? buttonSize : 20;
            DateTime finished = DateTime.MinValue;
            if (DateTime.TryParse(details.FinishTimeString, out finished))
            {
                if (finished > DateTime.MinValue)
                    Content = finished.ToString("HH:mm");
                else
                    Content = "►";//crude play button for now...
            }
            FontSize = 10.0;
            Background = new SolidColorBrush(details.ColorValue);
            Foreground = new SolidColorBrush(Colors.White);
            Margin = new Thickness(1);
            ToolTip = details.Information;
            StopBuildMenu.Visibility = details.IsRunning ? Visibility.Visible : Visibility.Collapsed;
            UpdateLayout();
        }

        /// <summary>
        /// Update the button with the new details
        /// </summary>
        /// <param name="upd"></param>
        internal void UpdateBuildDetails(BuildDetails upd)
        {
            if ((BuildDetails != null) && (BuildDetails.BuildNumber == upd.BuildNumber))
            {
                Init(upd);
            }
        }

        #endregion Methods

        #region Overrides


        #endregion Overrides

        /// <summary>
        /// User wants to stop the build
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StopBuild_Click(object sender, RoutedEventArgs e)
        {
            if ((BuildDetails!=null) && (BuildDetails.IsRunning))
            {
                MessageBoxResult res = MessageBox.Show(string.Format("Are you sure you want to stop build '{0}{1}'?", BuildDetails.BuildName, BuildDetails.BuildNumber), "Queue new build", MessageBoxButton.YesNo);
                if (res == MessageBoxResult.Yes)
                    BuildDetails.StopBuild();
            }

        }

        /// <summary>
        /// User wants to copy info to clipboard
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CopyDetailsMenu_Click(object sender, RoutedEventArgs e)
        {
            if (BuildDetails != null)
            {
                System.Windows.Clipboard.SetText(BuildDetails.Information);
            }
        }

        /// <summary>
        /// User wants to copy drop folder
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CopyDropFolderMenu_Click(object sender, RoutedEventArgs e)
        {
            if ((BuildDetails != null) &&(BuildDetails.DropLocation!=null))
            {
                System.Windows.Clipboard.SetText(BuildDetails.DropLocation);
            }
        }

        /// <summary>
        /// user wants to copy installer location
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CopyInstallFolderMenu_Click(object sender, RoutedEventArgs e)
        {
            if ((BuildDetails != null)&&(BuildDetails.InstallerLocation!=null))
            {
                System.Windows.Clipboard.SetText(BuildDetails.InstallerLocation);
            }
        }

        private void ContextMenu_Drop(object sender, DragEventArgs e)
        {

        }

        /// <summary>
        /// Build details button has been activated
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BuildDetailsButton_Activated(object sender, RoutedEventArgs e)
        {
            if (sender is BuildDetailsButton)
            {
                BuildDetailsButton bdButton = sender as BuildDetailsButton;
                if (bdButton != null)
                {
                    HightlightButton(bdButton);
                    BuildStatusPanel.MainWindow main = Application.Current.MainWindow as BuildStatusPanel.MainWindow;
                    if (main != null)
                    {
                        main.UpdateBuildDetailsPanel(bdButton.BuildDetails);
                        main.UpdateBuildResults(bdButton.BuildDetails);
                    }
                }
            }
        }

        /// <summary>
        /// Highlight the button style
        /// </summary>
        /// <param name="bdButton"></param>
        private static void HightlightButton(BuildDetailsButton bdButton)
        {
            if (_previousSelectedButton != null)
            {
                _previousSelectedButton.BorderBrush = new SolidColorBrush(Colors.LightGray);
                _previousSelectedButton.Opacity = 0.75;
                _previousSelectedButton.FontStyle = FontStyles.Normal;

            }
            _previousSelectedButton = bdButton;
            _previousSelectedButton.BorderBrush = new SolidColorBrush(Colors.Black);
            _previousSelectedButton.Opacity = 1;
            _previousSelectedButton.FontStyle = FontStyles.Italic;
        }
    }
}