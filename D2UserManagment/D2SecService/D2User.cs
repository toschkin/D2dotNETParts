using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mime;
using System.Reflection;
using System.Security.Cryptography;
using System.ServiceModel;
using System.Text;
using System.Threading;
using D2Helpers;
using Microsoft.Win32;

namespace D2SecService
{
    [ServiceBehavior(InstanceContextMode = InstanceContextMode.Single)]
    public class D2User : ID2User 
    {
        private volatile List<IPasswordChangeEvent> _clientList = new List<IPasswordChangeEvent>();
        public byte[] GetD2UserPassword()
        {
            int currentRetry = 5;
            do
            {
                try
                {
                    var pathHelper = new D2PathHelper();                    
                    return File.ReadAllBytes(pathHelper.GetD2Path() + "Lgn" 
                        + new D2IniFileHelper(pathHelper.GetD2IniPath()).GetD2IniKeyValue("Servers", "ServerIP", Environment.MachineName) 
                        + ".dat");                    
                }
                catch (Exception)
                {
                    currentRetry--;
                    Thread.Sleep(20);
                }
            } while (currentRetry > 0);
            return null;
        }
      
        public void SubscribeOnPasswordChange()
        {
            IPasswordChangeEvent callback = OperationContext.Current.GetCallbackChannel<IPasswordChangeEvent>();
            if (!_clientList.Contains(callback))
                _clientList.Add(callback);
            NotifySubscriber(callback);
        }

        public void UnsubscribeFromPasswordChange()
        {
            IPasswordChangeEvent callback = OperationContext.Current.GetCallbackChannel<IPasswordChangeEvent>();
            if(_clientList.Contains(callback))
                _clientList.Remove(callback);
        }

        public void NotifySubscriber(IPasswordChangeEvent client)
        {                       
            try
            {
                client.NotifyPasswordChange(GetD2UserPassword());
            }
            catch (Exception)
            {//client so rapidly became dead (???) we will remove it from list on next call to NotifyAllSubscribers                
            }           
        }

        public void NotifyAllSubscribers()
        {
            List<IPasswordChangeEvent> deadClients = new List<IPasswordChangeEvent>();
            foreach (var client in _clientList)
            {                                
                try
                {
                    client.NotifyPasswordChange(GetD2UserPassword());
                }
                catch (Exception)
                {                    
                   deadClients.Add(client);                                                                                                   
                }                                                                     
            }
            foreach (var deadClient in deadClients)
                _clientList.Remove(deadClient);
        }
    }
}
