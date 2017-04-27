using System;
using System.Collections.Generic;
using System.DirectoryServices;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using D2Helpers;
using D2SecService;
using TextFileLogger;

namespace D2UserHost
{
    
    public class PasswordChangeWorker
    {
        [DllImport("SetupCryptHelper.dll", CharSet = CharSet.Unicode)]
        private static extern int CreateLgnFile(string fullPath, string password);
        
        public string IpAddress { get; set; }
        public TextLogger Logger { get; set; }
        public D2User D2UserService { get; set; }

        public void ChangeD2UserPassword()
        {
            //Generating new password
            string newPassword = Guid.NewGuid().ToString();
            newPassword = newPassword.Replace("-", "");
            Random random = new Random(DateTime.Now.Millisecond);
            newPassword = newPassword.Remove(random.Next(8, 31));

            //Creating temporary Lgn
            string fileName = (new D2PathHelper()).GetD2Path() + @"_Lgn" + IpAddress + ".dat";
            try
            {
                int retCode = CreateLgnFile(fileName, newPassword);
                if (retCode != 0)
                {
                    Logger?.LogTextMessage("CreateLgnFile returns " + retCode);
                }
            }
            catch (Exception exception)
            {                
                Logger?.LogTextMessage("Exception while calling CreateLgnFile: "+ exception.Message);
                return;
            }
            
            //Change password for D2_User in Windows
            try
            {
                DirectoryEntry localDirectory = new DirectoryEntry("WinNT://" + Environment.MachineName);
                DirectoryEntries users = localDirectory.Children;
                DirectoryEntry user = users.Find("D2_User");
                user.Invoke("SetPassword", newPassword);                    
            }
            catch (Exception exception)
            {
                Logger?.LogTextMessage("Exception while invoking SetPassword: " 
                    + exception.Message + "\nStack:" + exception.StackTrace);
                return;
            }

            //Renaming file if password changed successfully 
            string newFileName = (new D2PathHelper()).GetD2Path() + @"Lgn" + IpAddress + ".dat";
            try
            {
                if (File.Exists(newFileName))
                {
                    File.Delete(newFileName);
                }
                File.Move(fileName, newFileName);
            }
            catch (Exception exception)
            {
                Logger?.LogTextMessage("Exception while renaming Lgn-file: " + exception.Message);
                return;
            }
            //Notifying clients
            D2UserService.NotifyAllSubscribers();          
        }        
    }
}
