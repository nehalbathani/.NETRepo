using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using EncryptionUtility.Utility;


namespace EncryptionUtility
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
       
    public partial class MainWindow : Window
    {
        private static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        string cleartext_Flag = string.Empty;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void btnEncrypt_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if(cleartext_Flag == "N")
                {
                    logger.Info("Starting Encryption");
                    //Add Vector length check
                    if (txtEncVec.Text.Length != 24)
                    {
                        Console.WriteLine("Vector should be equal to 24 characters");
                        txtEncVec.Clear();
                        txtEncVec.Focus();
                    } //
                    else
                    {
                        txtDecStr.Text = AESProvider.EncryptString(txtEncStr.Text, txtEncKey.Text, txtEncVec.Text);

                        //Add a method to write to file
                        FileCopy fCopy = new FileCopy();
                        fCopy.SetConfigToFile(txtDecStr.Text, txtEncKey.Text, txtEncVec.Text,cleartext_Flag);
                        
                    }
                }
                else if (cleartext_Flag == "Y")
                {
                    FileCopy fCopy = new FileCopy();
                    fCopy.SetConfigToFile(txtEncStr.Text, "", "",cleartext_Flag);
                }
            }//try
            catch (Exception exp)
            {
                logger.Error(exp.Message);
                logger.Error(exp.StackTrace);
                logger.Error(exp.InnerException);
                throw (exp);
            }
        }

        private void btnDecrypt_Click(object sender, RoutedEventArgs e)
        {
            txtEncStr.Text = AESProvider.DecryptString(txtDecStr.Text, txtEncKey.Text, txtEncVec.Text);
        }

        private void btnGenKey_Click(object sender, RoutedEventArgs e)
        {
            string key = string.Empty;
            string vector = string.Empty;
            AESProvider.GenerateKeyAndVector(out key, out vector);
            txtEncKey.Text = key;
            txtEncVec.Text = vector;
        }

        private void radiocleartxt_Checked(object sender, RoutedEventArgs e)
        {
            cleartext_Flag = "Y";
        }

        private void radioEncrypt_Checked(object sender, RoutedEventArgs e)
        {
            cleartext_Flag = "N";
        }

        
       }
}
