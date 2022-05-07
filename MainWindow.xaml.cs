using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
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
//specific namespaces needed for project
using Microsoft.Win32;
using System.Security.Cryptography.X509Certificates;


namespace KeyFile_WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Loaded += PageLoaded;
        }

        private void PageLoaded(object sender, RoutedEventArgs e)
        {
            CurrentDirectory.Text = @"C:\";
            RefreshFileList();
        }
        private void FileButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)//if user chose a file
            {
                //Get basic info of selected file
                FileNameTextBlock.Text = openFileDialog.FileName;
                GetFileAttributes();
            }
        }

        private void AddUserButton_Click(object sender, RoutedEventArgs e)
        {
            //Get cert info with the 'dig' command process
            Process DigProcess = ExecuteProcess("C:\\Program Files\\ISC BIND 9\\bin\\dig.exe", "+tcp +short CERT " + TransformEmailToDNS(UserEmailTextBox.Text));

            DigProcess.WaitForExit();
            String DnsOutput = DigProcess.StandardOutput.ReadToEnd();
            //split string to just get certificate (starting with the 4th item)
            string[] subs = DnsOutput.Split(' ');

            String certStr = "-----BEGIN CERTIFICATE-----\r\n";
            for (int i = 3;i < subs.Length - 1; i++)
            {
                certStr += subs[i] + "\r\n";
            }
            certStr += subs[subs.Length - 1] + "-----END CERTIFICATE-----\r\n";

            //Save output Cert into a temp PEM file
            String tmpFile = CreateTmpFile();
            StreamWriter streamWriter = new StreamWriter(tmpFile) ;
            //StreamWriter streamWriter = new StreamWriter(fName) ;
            streamWriter.Write(certStr);
            streamWriter.Close();


            //Add confirm dialog box to display cert details and confirm that the user wants to add the user's cert to the file?
            X509Certificate cert = new X509Certificate(tmpFile);

            // Get the value.
            string resultsTrue = cert.ToString(true);

            // Display the value to the console.
            if (MessageBox.Show("Certificate Found.\r\n Please confirm you would like to share with this person: " +
                "\r\n\r\n" + resultsTrue, "", (MessageBoxButton)1) == (MessageBoxResult)1)
            {
                //Call Cipher.exe to add the cert to the file (cert in the temp file)
                //call cipher /E filename first since it requires file to be EFS encrypted first before adding another user
                Process CipherProcess = ExecuteProcess("CIPHER", "/E \"" + FileNameTextBlock.Text + "\"");

                CipherProcess = ExecuteProcess("CIPHER", "/ADDUSER /CERTFILE:\"" + tmpFile + "\" \"" + FileNameTextBlock.Text + "\"");
                //This could take some time....or require inserting key device and entering PIN
                CipherProcess.WaitForExit();

                // Refresh the EFS information in the textbox to show the new added cert
                GetFileAttributes();
            }
        }

        private Process ExecuteProcess(string pName, string pOptions)
        {
            //Get cert info with the 'dig' command process
            Process proc = new Process();
            proc.StartInfo.FileName = pName;
            proc.StartInfo.Arguments = pOptions;
            proc.StartInfo.CreateNoWindow = true;
            proc.StartInfo.UseShellExecute = false;
            proc.StartInfo.RedirectStandardOutput = true;
            proc.Start();

            return proc;
        }
        private void RefreshFileList()
        {
            FileList.Items.Clear();
            if (!CurrentDirectory.Text.EndsWith('\\'))
                CurrentDirectory.Text += "\\";

            string[] dirs = Directory.GetDirectories(CurrentDirectory.Text);
            foreach (string dirName in dirs)
                FileList.Items.Add("D " + dirName.Substring(dirName.LastIndexOf('\\') + 1));

            string[] fileEntries = Directory.GetFiles(CurrentDirectory.Text);
            foreach (string fileName in fileEntries)
            {
                if (fileName.Length > 0)
                    FileList.Items.Add("  " + fileName.Substring(fileName.LastIndexOf('\\') + 1));
            }
        }
        private void GetFileAttributes()
        {

            FileInfo fileInfo = new FileInfo(FileNameTextBlock.Text);
            FileSizeTextBlock.Text = fileInfo.Length.ToString() + " bytes";
            FileModifiedDateTextBlock.Text = fileInfo.LastWriteTime.ToString();
            FileEncryptionTextBlock.Text = "None";

            //Get EFS info with the 'CIPHER' command process
            Process CipherProcess = ExecuteProcess("CIPHER", "/C \"" + FileNameTextBlock.Text + "\"");
            //This could take some time....or require inserting key device and entering PIN
            //First, read in and discard first 4 lines of output (not needed)
            for (int i = 0; i < 4; i++)
            {
                CipherProcess.StandardOutput.ReadLine();
            }

            //read each line of cipher's output and process it accordingly
            String output = "";
            String prev = "";
            UserList.Items.Clear(); 
            while (true)
            {
                String line = CipherProcess.StandardOutput.ReadLine();
                if (line != null)
                {
                    output += line;
                    if (line.StartsWith("    Certificate thumbprint:"))
                    {
                        UserList.Items.Add("Name: " + prev.Substring(4) + "\r\nThumbprint: " + line.Substring(28));
                    }
                    else if (line.StartsWith("  Key Information:"))
                    {
                        FileEncryptionTextBlock.Text = "ENCRYPTED: " + CipherProcess.StandardOutput.ReadLine().Trim() + " " + CipherProcess.StandardOutput.ReadLine().Trim() ;
                    }
                    prev = line;
                }
                else
                    break;
            }
            //for debug
            //FileEFSProperties.Text = output;
            CipherProcess.WaitForExit();
        }
        private static string CreateTmpFile()
        {
            string fileName = string.Empty;

            try
            {
                fileName = System.IO.Path.GetTempFileName();
                FileInfo fileInfo = new FileInfo(fileName);
                fileInfo.Attributes = FileAttributes.Temporary;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unable to create TEMP file or set its attributes: " + ex.Message);
            }

            return fileName;
        }
        private String TransformEmailToDNS(string email) {
            String dnsEmail;
            //split string to just get certificate (starting with the 4th item)
            string[] subs = email.Split('@');
            if (subs.Length > 1)
            {
                //e.g. 'John.Doe@domain.com' is now 'john\\.doe.domain.com'
                dnsEmail = subs[0].Replace(".","\\.").ToLower() + "." + subs[1];
            }
            else { dnsEmail = subs[0]; }

            return dnsEmail; 
        }

        private void RemoveUserButton_Click(object sender, RoutedEventArgs e)
        {
            //iterate through each selected item in UserList
            System.Collections.IList selectedItems = UserList.SelectedItems;

            if (UserList.SelectedIndex != -1)
            {
                for (int i = selectedItems.Count - 1; i >= 0; i--)
                {
                    // get the selected user's fingerprint first
                    String item = (String)selectedItems[i];
                    string[] subs = item.Split(':');
                    String fprint = subs[subs.Length - 1];

                    // call cipher to remove access for that cert fingerprint
                    Process CipherProcess = ExecuteProcess("CIPHER", "/REMOVEUSER /CERTHASH:\"" + fprint + "\" \"" + FileNameTextBlock.Text + "\"");
                    CipherProcess.WaitForExit();

                    //remove item from list onlt if cipher command successful
                    if (CipherProcess.ExitCode == 0)
                    {
                        UserList.Items.Remove(selectedItems[i]);
                    }

                }
            }
        }

        private void FileList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            String selection = (String)(FileList.SelectedItem);
            if (selection != null)
            {
                // if directory selected, go into it!
                if (selection.StartsWith("D "))
                {
                    CurrentDirectory.Text += selection.Substring(2);
                    RefreshFileList();

                }
                //if file is selected, add to right side and refresh file info.
                else if (selection.StartsWith("  "))
                {
                    FileNameTextBlock.Text = CurrentDirectory.Text + selection.Substring(2);
                    GetFileAttributes();
                }

            }
        }

        private void Up_Button_Click(object sender, RoutedEventArgs e)
        {
            String dir = CurrentDirectory.Text;

            // TODO Check for root directory, changing drives, etc....

            // Remove trailing \ symbol first
            if (dir.EndsWith('\\'))
                dir = dir.Substring(0, dir.Length - 1);
           
            // Remove last subdirectory 
            CurrentDirectory.Text = dir.Substring(0, dir.LastIndexOf('\\'));
            RefreshFileList();
        }
    }
}
