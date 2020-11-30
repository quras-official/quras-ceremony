using Ceremony;
using Ceremony.Core;
using Ceremony.Cryptography.ECC;
using Ceremony.IO.Json;
using Ceremony.Wallets;
using CeremonyClient.Utils;
using CeremonyClientFinal.Network;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Resources;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace CeremonyClientFinal
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public UserInfo MyInfo;
        public int nCurrentState = 0; // 0 -> Waiting, 1 -> Started, 2 -> Finished
        public SeedInfoTransaction SeedInfoFromServer;
        public int nLastSegmentNumber = 0;
        public int nMyRange = 0;
        private KeyPair SeedKey;

        private int nGenerated = 0;
        private int nPercentage = 0;
        private long nFileSize = 0;

        private bool is_TryAgain = false;

        public MainWindow()
        {
            InitializeComponent();

            System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
            dispatcherTimer.Tick += dispatcherTimer_Tick;
            dispatcherTimer.Interval = new TimeSpan(0, 1, 0);
            dispatcherTimer.Start();

            System.Windows.Threading.DispatcherTimer dispatcher1Timer = new System.Windows.Threading.DispatcherTimer();
            dispatcher1Timer.Tick += dispatcherAlert_Tick;
            dispatcher1Timer.Interval = new TimeSpan(0, 0, 0, 0, 400);
            dispatcher1Timer.Start();
        }

        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            GetServerStatus();
        }

        private void dispatcherAlert_Tick(object sender, EventArgs e)
        {
            if (is_TryAgain == true)
            {
                btnUpload.Content = "Retry Upload";
                btnUpload.IsEnabled = true;
            }

            if (((string)btnUpload.Content).Contains("Uploading") && nGenerated >= 1 && nGenerated < 2)
            {
                btnUpload.Content = "Uploading " + nPercentage + "%";
            }

            if (nGenerated >= 2)
            {
                SendCompleteCeremony();
                GetServerStatus();
                nGenerated = -1;
            }
        }

        private string GetMD5(string source)
        {
            using (var md5 = MD5.Create())
            {
                byte[] hashResult = md5.ComputeHash(Encoding.ASCII.GetBytes(source));
                return BitConverter.ToString(hashResult).Replace("-", "");
            }
        }

        public SeedInfoTransaction GetSeedKey()
        {
            RpcClient rpcClient = new RpcClient();
            string queryString = "";

            rpcClient.SetServerInfo(CeremonyClientFinal.Core.Settings.Default.server_url);

            JObject requestParam = new JObject();
            requestParam["pubkey"] = MyInfo.MyWallet.GetPublicKey().ToString();

            JObject requestBody = new JObject();

            requestBody["method"] = "GetSeedKey";
            requestBody["params"] = requestParam;

            queryString = requestBody.ToString();

            try
            {
                string response = rpcClient.SendRequest(queryString);
                JObject responseBody = JObject.Parse(response);
                JObject result = responseBody["result"];

                if (result["result"].AsString() == "true")
                {
                    SeedInfoTransaction tx = new SeedInfoTransaction();
                    tx.FromJson(result["data"]);
                    return tx;
                }

                return null;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public void SendCompleteCeremony()
        {
            RpcClient rpcClient = new RpcClient();
            string queryString = "";

            rpcClient.SetServerInfo(CeremonyClientFinal.Core.Settings.Default.server_url);

            JObject requestParam = new JObject();
            requestParam["pubkey"] = MyInfo.MyWallet.GetPublicKey().ToString();
            requestParam["SegmentNumber"] = nLastSegmentNumber + nMyRange;

            JObject requestBody = new JObject();

            requestBody["method"] = "CompleteCeremony";
            requestBody["params"] = requestParam;

            queryString = requestBody.ToString();

            try
            {
                string response = rpcClient.SendRequest(queryString);
                JObject responseBody = JObject.Parse(response);
                JObject result = responseBody["result"];

                if (result["result"].AsString() == "true")
                {
                    tbLogMessage.Text = "Send status to the server successfully";
                    tbLogMessage.Foreground = Brushes.Green;
                }
                else
                {
                    tbLogMessage.Text = result["msg"].AsString();
                    tbLogMessage.Foreground = Brushes.DarkRed;
                    if (result["msg"].AsString().Contains("VERIFY=false"))
                    {
                        File.Delete("crypto//constraint.key");
                        btnUpload.Content = "Generate";

                        if (!txbHexSeed.Text.Equals(""))
                        {
                            btnUpload.IsEnabled = true;
                        }

                        tbLogMessage.Text = "Verifying Key failed. Regenerate the key.";
                        tbLogMessage.Foreground = Brushes.DarkRed;
                    }
                }
            }
            catch(Exception ex)
            {
                tbLogMessage.Text = "Sending status exception occured";
                tbLogMessage.Foreground = Brushes.DarkRed;
            }
        }

        public SeedInfoTransaction UploadSeedKey(SeedInfoTransaction transInfo)
        {
            RpcClient rpcClient = new RpcClient();
            string queryString = "";

            rpcClient.SetServerInfo(CeremonyClientFinal.Core.Settings.Default.server_url);

            JObject requestParam = new JObject();
            requestParam["pubkey"] = MyInfo.MyWallet.GetPublicKey().ToString();
            requestParam["transaction"] = transInfo.ToJson();

            JObject requestBody = new JObject();

            requestBody["method"] = "UploadSeedKey";
            requestBody["params"] = requestParam;

            queryString = requestBody.ToString();

            try
            {
                string response = rpcClient.SendRequest(queryString);
                JObject responseBody = JObject.Parse(response);
                JObject result = responseBody["result"];

                if (result["result"].AsString() == "true")
                {
                    GetServerStatus();
                }
                else
                {
                    // --------------------------> add alert here.
                }

                return null;
            }
            catch (Exception ex)
            {
                return null;
            }
        }

        public void GetServerStatus()
        {
            RpcClient rpcClient = new RpcClient();
            string queryString = "";

            rpcClient.SetServerInfo(CeremonyClientFinal.Core.Settings.Default.server_url);

            JObject requestParam = new JObject();
            requestParam["pubkey"] = MyInfo.MyWallet.GetPublicKey().ToString();

            JObject requestBody = new JObject();
                
            requestBody["method"] = "GetInfo";
            requestBody["params"] = requestParam;

            queryString = requestBody.ToString();

            try
            {
                string response = rpcClient.SendRequest(queryString);
                JObject responseBody = JObject.Parse(response);
                JObject result = responseBody["result"];

                if (result["result"].AsString() == "true")
                {
                    JObject updateData = result["data"];
                    MyInfo.TotalParticipant = int.Parse(updateData["total_participants"].AsString());
                    MyInfo.SelectedIndex = int.Parse(updateData["selected_participants"].AsString());
                    MyInfo.MyStatus = int.Parse(updateData["my_status"].AsString());
                    nLastSegmentNumber = int.Parse(updateData["segment_number"].AsString());
                    nMyRange = (CeremonyClientFinal.Core.Settings.Default.nTotalSegments - nLastSegmentNumber) / (MyInfo.TotalParticipant - MyInfo.MyOrder + 1);

                    switch (updateData["ceremony_status"].AsString())
                    {
                        case "CEREMONY_STATUS_WAITING":
                            nCurrentState = 0;
                            break;
                        case "CEREMONY_STATUS_STARTED":
                            nCurrentState = 1;
                            break;
                        case "CEREMONY_STATUS_COMPLETED":
                            nCurrentState = 2;
                            break;
                        default:
                            break;
                    }

                    if (MyInfo.SelectedIndex == MyInfo.MyOrder && nCurrentState == 1)
                    {
                        SeedInfoFromServer = GetSeedKey();
                        txbSeedText.IsEnabled = true;
                        txbSeedText.Tag = "Write your seed text here...";
                        btnReset.IsEnabled = true;
                        btnGenerate.IsEnabled = true;
                        
                    }

                    UpdateUserInterface();
                }
            }
            catch(Exception ex)
            {

            }

        }
        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        public void UpdateUserInterface()
        {
            lbName.Content = MyInfo.UserName;
            lbCountry.Content = MyInfo.Country;
            lbAddress.Content = MyInfo.MyWallet.GetAddress();
            lbCurrentTurn.Content = MyInfo.SelectedIndex.ToString() + "/" + MyInfo.TotalParticipant.ToString();

            if (nCurrentState == 0)
                lbCurrentState.Content = "Waiting";
            else if (nCurrentState == 1)
                lbCurrentState.Content = "Started";
            else if (nCurrentState == 2)
                lbCurrentState.Content = "Finished";

            if (MyInfo.MyStatus == 2)
            {
                grdState.Visibility = Visibility.Visible;
                btnUpload.Content = "Uploaded";
            }

            if (MyInfo.MyStatus != 1)
            {
                btnGenerate.IsEnabled = false;
                btnReset.IsEnabled = false;
                btnUpload.IsEnabled = false;

                txbSeedText.IsEnabled = false;
            }
        }

        private void BtnGenerate_Click(object sender, RoutedEventArgs e)
        {
            using (var md5 = MD5.Create())
            {
                string hexString = md5.ComputeHash(Encoding.ASCII.GetBytes(txbSeedText.Text)).ToHexString();
                byte[] hashResult = new byte [32];
                for (int i = 0; i < 32; i ++)
                {
                    hashResult[i] = (byte)hexString[i];
                }
                SeedKey = new KeyPair(hashResult);

                txbHexSeed.Text = SeedKey.PublicKey.ToString();

                btnUpload.IsEnabled = true;
                /**/
            }
        }

        private void BtnReset_Click(object sender, RoutedEventArgs e)
        {
            txbHexSeed.Text = "";
            txbSeedText.Text = "";
        }

        private void BtnUpload_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (File.Exists("crypto//constraint.key"))
                    nGenerated = 1;
                else
                    nGenerated = 0;

                if (nGenerated == 0)
                {
                    /*ECPoint orgSeed = SeedInfoFromServer.GetXORSeed(MyInfo.MyWallet.GetPrivateKey(), SeedInfoFromServer.SenderPubKey);
                    ECPoint mySeed = SeedKey.PublicKey;*/

                    ECPoint XORSeed = SeedKey.PublicKey;// GenerateXORSeed(orgSeed, mySeed);

                    ExtractResource();

                    ExecuteGenerate(XORSeed);
                }

                btnUpload.Content = "Uploading";
                btnUpload.IsEnabled = false;
                is_TryAgain = false;

                nGenerated = 1;

                Thread upload = new Thread(() => UploadFile("crypto//constraint.key", 0));
                upload.Start();
            }
            catch (Exception ex)
            {
                tbLogMessage.Text = "Unknown Exception Occured";
                tbLogMessage.Foreground = Brushes.DarkRed;
            }
        }

        private bool ExecuteGenerate(ECPoint SeedValue)
        {
            btnUpload.Content = "Generating";
            btnUpload.IsEnabled = false;
            ProcessStartInfo startInfo = new ProcessStartInfo();
            startInfo.CreateNoWindow = false;
            startInfo.UseShellExecute = false;
            startInfo.FileName = "output//snarks.exe";
            startInfo.WindowStyle = ProcessWindowStyle.Hidden;
            startInfo.Arguments = SeedValue.ToString() + " " + nLastSegmentNumber.ToString() + " " + nMyRange ;

            try
            {
                // Start the process with the info we specified.
                // Call WaitForExit and then the using statement will close.
                using (Process exeProcess = Process.Start(startInfo))
                {
                    exeProcess.WaitForExit();
                    if (File.Exists("crypto//constraint.key"))
                    {
                        tbLogMessage.Text = "Key Generated Successfully.";
                        tbLogMessage.Foreground = Brushes.Green;
                        nGenerated = 1;
                    }
                }
                return true;
            }
            catch
            {
                // Log error.
                return false;
            }
        }

        private void ExtractResource()
        {
            ResourceManager rm = new ResourceManager("CeremonyClientFinal.Ceremony", Assembly.GetExecutingAssembly());
            Directory.CreateDirectory("output");
            File.WriteAllBytes("output//snarks.exe",(byte[])rm.GetObject("Quras_snarks"));
            File.WriteAllBytes("output//libcryptoMD.dll", (byte[])rm.GetObject("libcryptoMD"));
            File.WriteAllBytes("output//libsodium.dll", (byte[])rm.GetObject("libsodium"));
            Directory.CreateDirectory("crypto");
        }

        private ECPoint GenerateXORSeed(ECPoint OrgSeed, ECPoint MySeed)
        {
            if (OrgSeed.ToString().Equals("00"))
                return MySeed;
            byte[] orgSeedBytes = OrgSeed.ToString().HexToBytes();
            byte[] mySeedBytes = MySeed.ToString().HexToBytes();

            byte[] xorResult = new byte[Math.Max(orgSeedBytes.Length, mySeedBytes.Length)];

            xorResult[0] = 03;

            for (int i = 1; i < Math.Min(orgSeedBytes.Length, mySeedBytes.Length); i ++)
            {
                xorResult[i] = (byte)(orgSeedBytes[i] ^ mySeedBytes[i]);
            }

            return ECPoint.Parse(xorResult.ToHexString(), ECCurve.Secp256r1);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            StaticUtils.NotifyMessageMgr = new CeremonyClient.Dialogs.NotifyMessage.NotifyMessageManager
               (
                   this.Left,
                   this.Top,
                   Width,
                   Height,
                   400,
                   30
               );
            StaticUtils.NotifyMessageMgr.Start();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (StaticUtils.NotifyMessageMgr != null)
            {
                StaticUtils.NotifyMessageMgr.Stop();
            }
        }

        private void Window_LocationChanged(object sender, EventArgs e)
        {
            if (StaticUtils.NotifyMessageMgr != null)
            {
                StaticUtils.NotifyMessageMgr.ResetNotifyMessageLocations(this.Left, this.Top, this.ActualWidth, this.ActualHeight, 400, 30);
            }
        }

        private void UploadFile(string path, int nType = 0)
        {
            FileStream fs = File.OpenRead(path);

            byte[] buf = new byte[1024 * 1024];
            int len;

            nFileSize = new System.IO.FileInfo(path).Length;
            long nCurrentUploaded = 0;
            nPercentage = 0;

            while (MyInfo.MyOrder == MyInfo.SelectedIndex && (len = fs.Read(buf, 0, buf.Length)) > 0)
            {
                int nRetryCount = 3;

                while (nRetryCount > 0)
                {
                    RpcClient rpcClient = new RpcClient();
                    string queryString = "";

                    rpcClient.SetServerInfo(CeremonyClientFinal.Core.Settings.Default.server_url);

                    JObject requestParam = new JObject();

                    requestParam["UserId"] = MyInfo.MyOrder;
                    requestParam["FileName"] = "constraint.key";
                    requestParam["FileData"] = buf.ToHexString().Substring(0, len * 2);
                    requestParam["FileInit"] = (nCurrentUploaded == 0 ? 1 : 0);

                    JObject requestBody = new JObject();

                    requestBody["method"] = "SendKey";
                    requestBody["params"] = requestParam;

                    queryString = requestBody.ToString();

                    try
                    {
                        string response = rpcClient.SendRequest(queryString);
                        JObject responseBody = JObject.Parse(response);
                        JObject result = responseBody["result"];

                        if (result.AsString() == "true")
                        {
                            break;
                        }
                    }
                    catch (Exception ex)
                    {
                        nRetryCount--;
                    }
                }

                if (nRetryCount == 0)
                {
                    is_TryAgain = true;
                    fs.Close();
                    return;
                }

                nCurrentUploaded += len;
                nPercentage = Math.Min(100,(int)(100 * nCurrentUploaded / nFileSize));
            }

            nGenerated ++;
            fs.Close();
        }
    }
}
