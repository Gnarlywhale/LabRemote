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
using Microsoft.WindowsAPICodePack.Dialogs;
using LSL;
using System.Media;
using System.Windows.Threading;
using System.Diagnostics;
using System.Threading;


class LSLStream
{
    public string Name { get; set; }
    public Boolean Controllable { get; set; }
    
    public liblsl.StreamInfo InfoHandle { get; set; }
    public LSLStream(string name, Boolean controllable , liblsl.StreamInfo infohandle)
    {
        this.Name = name;
        this.Controllable = controllable;
        this.InfoHandle = infohandle;

    }
}
namespace LabRemote
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private static List<LSLStream> LSLstreams;
        private static Process recorderProcess;
        private static SoundPlayer player;
        private static DispatcherTimer trialTimer;
        private static Boolean isRunning;
        private static DateTime trialStart;

        public MainWindow()
        {

            InitializeComponent();
            loadStreams(this, null);
            isRunning = false;
            player = new SoundPlayer();
            player.Stream = Properties.Resources.coin;
            trialTimer = new DispatcherTimer();
            trialTimer.Interval = TimeSpan.FromMilliseconds(1);
            trialTimer.Tick += new EventHandler(trialTimerTick);
        }

        private void trialTimerTick(object sender, EventArgs e)
        {
            TimeSpan elapsed = DateTime.Now.Subtract(trialStart);
            trialTime.Content = elapsed.ToString(@"mm\:ss\:ff");
            CommandManager.InvalidateRequerySuggested();
        }
        private void loadStreams(object sender, RoutedEventArgs e)
        {
            liblsl.StreamInfo[] streams = liblsl.resolve_streams();
            LSLstreams = new List<LSLStream>();
            for (int i = 0; i < streams.Length; i++)
            {
                String name = streams[i].name();
                LSLstreams.Add(new LSLStream(name, false, streams[i]));
            }
        }
        private void refreshStreams(object sender, RoutedEventArgs e)
        {
            loadStreams(null, null);
            populateStreams(null, null);
        }
        public void populateStreams(object sender, RoutedEventArgs e)
        {
            streamGrid.ItemsSource = LSLstreams;
        }

        private void SelectProjectPath(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog dialog = new CommonOpenFileDialog();
            dialog.IsFolderPicker = true;
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                ProjectPath.Text = dialog.FileName;
            }
        }

        private void incrementTrial(object sender, RoutedEventArgs e)
        {
            int i = 0;
            if (!Int32.TryParse(trialNum.Text, out i))
            {
                i = 0;
            }
            i += 1;
            trialNum.Text = i.ToString();
        }
        private void decrementTrial(object sender, RoutedEventArgs e)
        {
            int i = 0;
            if (!Int32.TryParse(trialNum.Text, out i))
            {
                i = 0;
            }
            i -= 1;
            trialNum.Text = i.ToString();
        }
        private void playBeep(object sender, RoutedEventArgs e)
        {

            player.Stream.Position = 0;
            player.Play();
        }

        private void RecordBtn_Click(object sender, RoutedEventArgs e)
        {
            if (!isRunning)
            {
                trialTimer.Start();
                trialStart = DateTime.Now;
                RecordBtn.Content = "Stop Trial";
                isRunning = true;

                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.CreateNoWindow = true;
                startInfo.UseShellExecute = false;
                startInfo.RedirectStandardInput = true;
                startInfo.FileName = "LabRecorderCLI.exe";
                startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                startInfo.Arguments = " testsituation.xdf \"name='Keyboard'\"";
                recorderProcess = Process.Start(startInfo);
               

            } else
            {
                recorderProcess.StandardInput.Write("\n");
                recorderProcess.StandardInput.Flush();
                RecordBtn.Content = "Start Trial";
                trialTimer.Stop();
                isRunning = false;

            }
        }

        
    }
}
