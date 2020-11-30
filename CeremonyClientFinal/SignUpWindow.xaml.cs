using Ceremony;
using Ceremony.IO;
using Ceremony.IO.Json;
using Ceremony.Wallets;
using CeremonyClientFinal.Network;
using CountryData.Standard;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
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
using System.Windows.Shapes;

namespace CeremonyClientFinal
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class SignUpWindow : Window
    {
        private SignInWindow signInWindow;
        private Wallet newWallet;
        public SignUpWindow()
        {
            InitializeComponent();
            InitInterface();
        }
        private void InitInterface()
        {
            var helper = new CeremonyCountryHelper();
            JArray countries = (JArray)helper.GetCountries();

            foreach (var country in countries)
            {
                cbCountry.Items.Add(country["name"].AsString());
            }

        }
        private void BtnSignUp_Click(object sender, RoutedEventArgs e)
        {
            if (txbUserName.Text == "" || txbPassword.Password == "" || txbEmailAddr.Text == "" ||
                cbCountry.Text == "")
                return;
            if (txbPassword.Password != txbConfirmPassword.Password)
                return;

            CreateNewWallet(txbUserName.Text, txbPassword.Password);

            SendSignUpRequest();

        }

        private void CreateNewWallet(string walletName, string walletPassword)
        {
            KeyPair walletKeyPair = CreateKey();
            newWallet = new Wallet();

            newWallet.SetKeyPair(walletKeyPair);
            newWallet.SaveWallet(walletName, walletPassword);
        }
        public KeyPair CreateKey(byte[] privateKey, KeyType nVersion = KeyType.Transparent)
        {
            KeyPair key = new KeyPair(privateKey, nVersion);
            return key;
        }

        private KeyPair CreateKey()
        {
            byte[] privateKey = new byte[32];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(privateKey);
            }
            KeyPair key = CreateKey(privateKey);
            Array.Clear(privateKey, 0, privateKey.Length);
            return key;
        }
        private void SendSignUpRequest()
        {
            RpcClient rpcClient = new RpcClient();
            string queryString = "";

            rpcClient.SetServerInfo(CeremonyClientFinal.Core.Settings.Default.server_url);

            JObject requestParam = new JObject();
            requestParam["username"] = txbUserName.Text;
            requestParam["password"] = GetMD5(txbPassword.Password);
            requestParam["email"] = txbEmailAddr.Text;
            requestParam["country"] = cbCountry.Text;
            requestParam["pubkey"] = newWallet.GetPublicKey().ToString();
            requestParam["address"] = newWallet.GetAddress();

            JObject requestBody = new JObject();

            requestBody["method"] = "SignUp";
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
                    signInWindow = new SignInWindow();
                    signInWindow.txbEmailAddr.Text = txbEmailAddr.Text;
                    signInWindow.txbPassword.Password = txbPassword.Password;
                    signInWindow.Show();
                    this.Close();
                }
            }
            catch(Exception ex)
            {
                lbNotice.Content = "SignUp Error! Cannot connect to the server.";
                lbNotice.Visibility = Visibility.Visible;
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

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            signInWindow = new SignInWindow();
            signInWindow.Show();
            this.Close();
        }
    }
}
