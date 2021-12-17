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

namespace openvr_tracker_role_gui
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private OpenVrManager openVrManager = new OpenVrManager();

        public MainWindow()
        {
            InitializeComponent();

            this.TrackerRoleList.ItemsSource = openVrManager.TrackerRoles;
            this.ConnectedDeviceList.ItemsSource = openVrManager.ConnectedDevices;
        }

        private void TrackerRoleChanged(object sender, SelectionChangedEventArgs e)
        {
            openVrManager.WriteSteamVrTrackerBindings();
        }
    }
}
