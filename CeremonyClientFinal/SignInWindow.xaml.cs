using Ceremony.IO.Json;
using Ceremony.Wallets;
using Ceremony.Core;
using CeremonyClientFinal.Network;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
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

namespace CeremonyClientFinal
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class SignInWindow : Window
    {
        private SignUpWindow signUpWindow;
        private MainWindow mainWindow;
        public SignInWindow()
        {
            InitializeComponent();
        }

        private void BtnSignUp_Click(object sender, RoutedEventArgs e)
        {
            signUpWindow = new SignUpWindow();
            signUpWindow.Show();
            this.Close();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        private string GetMD5(string source)
        {
            using (var md5 = MD5.Create())
            {
                byte[] hashResult = md5.ComputeHash(Encoding.ASCII.GetBytes(source));
                return BitConverter.ToString(hashResult).Replace("-", "");
            }
        }

        private Wallet OpenWallet(string userName)
        {
            try
            {
                Wallet myWallet = new Wallet();
                myWallet.ReadWallet(userName, txbPassword.Password);
                if (myWallet.GetAddress() != "")
                    return myWallet;
                return null;
            }
            catch(Exception ex)
            {
                return null;
            }
        }

        private void BtnSignIn_Click(object sender, RoutedEventArgs e)
        {
            //
            RpcClient rpcClient = new RpcClient();
            string queryString = "";

            rpcClient.SetServerInfo(CeremonyClientFinal.Core.Settings.Default.server_url);

            JObject requestParam = new JObject();
            requestParam["email"] = txbEmailAddr.Text;
            requestParam["password"] = GetMD5(txbPassword.Password);

            JObject requestBody = new JObject();

            requestBody["method"] = "SignIn";
            requestBody["params"] = requestParam;

            queryString = requestBody.ToString();

            try
            {
                string response = rpcClient.SendRequest(queryString);
                JObject responseBody = JObject.Parse(response);
                JObject result = responseBody["result"];

                if (result["result"].AsString() != "true")
                {
                    lbNotice.Content = result["msg"].AsString();
                    lbNotice.Visibility = Visibility.Visible;
                }
                else
                {
                    JObject data = result["data"];
                    UserInfo userInfo = new UserInfo();

                    userInfo.UserName = data["name"].AsString();
                    userInfo.EmailAddr = txbEmailAddr.Text;
                    userInfo.Country = data["country"].AsString();
                    userInfo.TotalParticipant = int.Parse(data["total"].AsString());
                    userInfo.MyOrder = int.Parse(data["order"].AsString());
                    userInfo.SelectedIndex = int.Parse(data["selected_index"].AsString());
                    userInfo.MyWallet = OpenWallet(userInfo.UserName);

                    if (userInfo.MyWallet == null)
                    {
                        lbNotice.Content = "Sign in failed! Check your wallet file.";
                        lbNotice.Visibility = Visibility.Visible;
                    }
                    else
                    {
                        mainWindow = new MainWindow();
                        mainWindow.MyInfo = userInfo;
                        mainWindow.GetServerStatus();
                        mainWindow.Show();
                        this.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                lbNotice.Content = "Sign in failed! Cannot connect to the server.";
                lbNotice.Visibility = Visibility.Visible;
            }
        }
    }

    public class PasswordBoxMonitor : DependencyObject
    {
        public static bool GetIsMonitoring(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsMonitoringProperty);
        }

        public static void SetIsMonitoring(DependencyObject obj, bool value)
        {
            obj.SetValue(IsMonitoringProperty, value);
        }

        public static readonly DependencyProperty IsMonitoringProperty =
            DependencyProperty.RegisterAttached("IsMonitoring", typeof(bool), typeof(PasswordBoxMonitor), new UIPropertyMetadata(false, OnIsMonitoringChanged));



        public static int GetPasswordLength(DependencyObject obj)
        {
            return (int)obj.GetValue(PasswordLengthProperty);
        }

        public static void SetPasswordLength(DependencyObject obj, int value)
        {
            obj.SetValue(PasswordLengthProperty, value);
        }

        public static readonly DependencyProperty PasswordLengthProperty =
            DependencyProperty.RegisterAttached("PasswordLength", typeof(int), typeof(PasswordBoxMonitor), new UIPropertyMetadata(0));

        private static void OnIsMonitoringChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var pb = d as PasswordBox;
            if (pb == null)
            {
                return;
            }
            if ((bool)e.NewValue)
            {
                pb.PasswordChanged += PasswordChanged;
            }
            else
            {
                pb.PasswordChanged -= PasswordChanged;
            }
        }

        static void PasswordChanged(object sender, RoutedEventArgs e)
        {
            var pb = sender as PasswordBox;
            if (pb == null)
            {
                return;
            }
            SetPasswordLength(pb, pb.Password.Length);
        }
    }
}
