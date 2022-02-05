
using System.IO;
using System.Windows;
namespace LabRemote
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
    
       private void onStartup(object sender, StartupEventArgs e)
        {
            // Check that all required files are in the current directory.
            if (!File.Exists("liblsl32.dll") || !File.Exists("lsl.dll") ||  !File.Exists("NatNetLib.dll") || !File.Exists("NatNetML.dll"))
            {
                MessageBoxResult response = MessageBox.Show("Necessary external libraries are missing, check all dll files are in Lab Remote's root folder and try again.", "Missing Library Files", MessageBoxButton.OK,MessageBoxImage.Error);
                System.Environment.Exit(1);
            }
        }
    }
}
