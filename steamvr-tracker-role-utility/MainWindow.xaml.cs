using System;
using System.Collections.Generic;
using System.Diagnostics;
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

namespace steamvr_tracker_role_utility
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private OpenVrManager openVrManager;
        public MainWindow()
        {
            InitializeComponent();
            
            Trace.Listeners.Add(new TextBoxTraceListener(LogTextBox));

            openVrManager = new OpenVrManager();
            this.TrackerRoleList.ItemsSource = openVrManager.TrackerRoles;
            this.ConnectedDeviceList.ItemsSource = openVrManager.ConnectedDevices;
        }

        private void TrackerRoleChanged(object sender, SelectionChangedEventArgs e)
        {
            openVrManager.WriteSteamVrTrackerBindings();
        }
    }
}
