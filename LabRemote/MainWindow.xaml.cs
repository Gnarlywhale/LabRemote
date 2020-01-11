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
using System.IO;
using NatNetML;

class LSLStream
{
    public string Name { get; set; }
    public Boolean Controllable { get; set; }
    public Boolean Record { get; set; }
    public liblsl.StreamInfo InfoHandle { get; set; }
    public LSLStream(string name, Boolean controllable , liblsl.StreamInfo infohandle)
    {
        this.Name = name;
        this.Controllable = controllable;
        this.InfoHandle = infohandle;
        this.Record = true;

    }
}
namespace LabRemote
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //Nat Net Initialization the address stuff should probably be editable.
        private static NatNetML.NatNetClientML mNatNet;    // The client instance
        private static string mStrLocalIP = "127.0.0.1";   // Local IP address (string)
        private static string mStrServerIP = "127.0.0.1";  // Server IP address (string)
        private static NatNetML.ConnectionType mConnectionType = ConnectionType.Multicast;  // multicast or unicast mode

        private static List<LSLStream> LSLstreams;
        private static Process recorderProcess;
        private static SoundPlayer player;
        private static DispatcherTimer trialTimer;
        private static Boolean isRunning;
        private static DateTime trialStart;
        private static Boolean beeped = false;
        private static liblsl.StreamOutlet labRecorderOutlet;
        private static int[] beepSample = new int[1];
        private static int[] triggerSample = new int[1];
        private static List<string> controlStrings = new List<string>();

        public MainWindow()
        {

            InitializeComponent();
            loadStreams(this, null);
            beepSample[0] = 1;
            triggerSample[0] = 2;
            liblsl.StreamInfo sInfo = new liblsl.StreamInfo("LabRemote", "Markers", 1, 0, liblsl.channel_format_t.cf_int32, "labremote");
            labRecorderOutlet = new liblsl.StreamOutlet(sInfo);
            isRunning = false;
            player = new SoundPlayer();
            player.Stream = Properties.Resources.boop;
            trialTimer = new DispatcherTimer();
            trialTimer.Interval = TimeSpan.FromMilliseconds(1);
            trialTimer.Tick += new EventHandler(trialTimerTick);

            // Setup list of streams that can be controlled, so far just NatNet/Optitrack. Assume that Optitrack is in the name.
            controlStrings.Add("OptiTrackFrameID");
            refreshStreams(null, null);


        }

        private void trialTimerTick(object sender, EventArgs e)
        {
            TimeSpan elapsed = DateTime.Now.Subtract(trialStart);
            trialTime.Content = elapsed.ToString(@"mm\:ss\:ff");
            CommandManager.InvalidateRequerySuggested();
            if ((Boolean)BeepBox.IsChecked && !beeped && elapsed.TotalSeconds >= Double.Parse(BeepDelay.Text))
            {
                playBeep(null, null);

                beeped = true;
            }
        }
        private void loadStreams(object sender, RoutedEventArgs e)
        {
            liblsl.StreamInfo[] streams = liblsl.resolve_streams();
            LSLstreams = new List<LSLStream>();
            LSLstreams.Clear();
            for (int i = 0; i < streams.Length; i++)
            {
                String name = streams[i].name();
                Boolean controllable = false;
                if (controlStrings.Contains(name))
                {
                    controllable = true;
                }
                LSLstreams.Add(new LSLStream(name, controllable, streams[i]));
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
            labRecorderOutlet.push_sample(beepSample);
            player.Play();

        }

        private void RecordBtn_Click(object sender, RoutedEventArgs e)
        {
            int resp = 0;
            if (!isRunning)
            {

                isRunning = true;
                string trialName = TrialName.Text.Replace(".xdf", "") + "_" + trialNum.Text;
                string fullTrial;
                if (ProjectPath.Text.Length > 0)
                {
                    fullTrial = "\"" + System.IO.Path.Combine(ProjectPath.Text, trialName + ".xdf") + "\"";
                }
                else
                {
                    fullTrial = trialName + ".xdf";

                }

                string streamList = " ";

                foreach (LSLStream lStream in streamGrid.Items)
                {
                    if (lStream.Record) streamList += "\"name=\"\"" + lStream.Name + "\"\"\" ";

                    if (lStream.Controllable)
                    {
                        if (lStream.Name.Equals("OptiTrackFrameID")) {
                            // Start "Control" of streams

                            mNatNet.SendMessageAndWait("SetRecordTakeName," + trialName, out resp);

                            //TODO: Break if recording doesn't start
                            mNatNet.SendMessageAndWait("StartRecording", out resp);


                        }
                    }
                }
                ProcessStartInfo startInfo = new ProcessStartInfo();
                startInfo.CreateNoWindow = true;
                startInfo.UseShellExecute = false;
                startInfo.RedirectStandardInput = true;
                startInfo.RedirectStandardOutput = true;
                startInfo.FileName = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "LabRecorderCLI.exe");
                startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                startInfo.Arguments = fullTrial + " " + streamList;

                recorderProcess = Process.Start(startInfo);
                string status = recorderProcess.StandardOutput.ReadLine();
                while (!status.Contains("Enter to quit"))
                {
                    System.Threading.Thread.Sleep(5);
                    status = recorderProcess.StandardOutput.ReadLine();

                }
                incrementTrial(null, null);
                trialTimer.Start();
                trialStart = DateTime.Now;
                RecordBtn.Content = "Stop Trial";
            } else
            {
                recorderProcess.StandardInput.Write("\n");
                recorderProcess.StandardInput.Flush();

                foreach (LSLStream lStream in streamGrid.Items)
                {
                    if (lStream.Controllable)
                    {
                        if (lStream.Name.Equals("OptiTrackFrameID"))
                        {
                            mNatNet.SendMessageAndWait("StopRecording", out resp);
                            //TODO: Display message if recording wasn't able to stop recording
                        }
                    }
                }

                RecordBtn.Content = "Start Trial";
                trialTimer.Stop();
                isRunning = false;
                beeped = false;
            }
        }

        private void TriggerBtn_Click(object sender, RoutedEventArgs e)
        {
            labRecorderOutlet.push_sample(triggerSample);
        }

        private void ControlBox_Checked(object sender, RoutedEventArgs e)
        {
            
            System.Windows.Controls.CheckBox ckbox = sender as System.Windows.Controls.CheckBox;
            LSLStream curStream = ckbox.DataContext as LSLStream;
            if (curStream.Controllable)
            {
                if (curStream.Name.Equals("OptiTrackFrameID"))
                {
                    // Move to method that gets called when the "OptiTrack" control checkbox is checked
                    mNatNet = new NatNetML.NatNetClientML();
                    int[] verNatNet = new int[4];
                    verNatNet = mNatNet.NatNetVersion();
                    Console.WriteLine("NatNet SDK Version: {0}.{1}.{2}.{3}", verNatNet[0], verNatNet[1], verNatNet[2], verNatNet[3]);

                    /*  [NatNet] Connecting to the Server    */
                    Console.WriteLine("\nConnecting...\n\tLocal IP address: {0}\n\tServer IP Address: {1}\n\n", mStrLocalIP, mStrServerIP);

                    NatNetClientML.ConnectParams connectParams = new NatNetClientML.ConnectParams();
                    connectParams.ConnectionType = mConnectionType;
                    connectParams.ServerAddress = mStrServerIP;
                    connectParams.LocalAddress = mStrLocalIP;
                    mNatNet.Connect(connectParams);
                }
            }
        } 
    }
}
