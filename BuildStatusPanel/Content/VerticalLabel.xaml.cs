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

namespace BuildStatusPanel.Content
{
    /// <summary>
    /// Interaction logic for VerticalLabel.xaml
    /// </summary>
    public partial class VerticalLabel : Label
    {
        double _successRate = 100;
        public string DayText { get; set; }
        public double SuccessRate
        {
            get
            {
                return _successRate;
            }
            set
            {
                _successRate = value;
                ToolTip = string.Format("{0}% successful for {1}", (int)(_successRate*100), DayText);
            }
        }
        public VerticalLabel()
        {
            InitializeComponent();
        }
        public VerticalLabel(string text)
        {
            InitializeComponent();
            Content = text;
            DayText = text;
            Foreground = new SolidColorBrush(Colors.DarkGray);

        }
    }
}
