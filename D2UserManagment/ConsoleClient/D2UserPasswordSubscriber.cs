﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.ServiceModel;
using System.ServiceModel.Security;
using System.Text;
using System.Threading;
using ConsoleClient.D2UserServiceReference;
using TextFileLogger;

namespace ConsoleClient
{
    public struct SubscriberConfiguration
    {
        public string Ip;
        public uint Port;
        public CancellationTokenSource TaskCancellationToken;
    }
    public delegate void PasswordProcessor(string ip, byte[] password);
    public delegate void ReconnectionProcessor(D2UserPasswordSubscriber subscriber);
    public class D2UserPasswordSubscriber : ID2UserCallback
    {
        private D2UserClient _client;
        private PasswordProcessor _passwordProcessor;
        private ReconnectionProcessor _reconnectionProcessor;
        private InstanceContext _callbackInstanceContext;
        private bool _needToAddFaultHandlers = true;

        public SubscriberConfiguration Config { get; set; }        
        public TextLogger Logger { get; set; }
        public bool IsAlive => !Config.TaskCancellationToken.IsCancellationRequested;

        public D2UserPasswordSubscriber(SubscriberConfiguration config, TextLogger logger = null)
        {
            Logger = logger;            
            Config = config;            
        }
        ~D2UserPasswordSubscriber()
        {            
            if (_client == null)
                return;            
            Close();
        }
        
        public void Initialize()
        {
            Logger?.LogTextMessage("Initialize()...");
            _needToAddFaultHandlers = true;
            bool needSaveToLog = true;
            while (!Config.TaskCancellationToken.Token.IsCancellationRequested)
            {
                if (_client == null)
                {                    
                    try
                    {
                        _callbackInstanceContext = new InstanceContext(this);
                        var clientTcpBinding = new NetTcpBinding();
                        clientTcpBinding.Security.Mode = SecurityMode.TransportWithMessageCredential;
                        clientTcpBinding.Security.Message.ClientCredentialType = MessageCredentialType.UserName;

                        clientTcpBinding.OpenTimeout = TimeSpan.FromSeconds(10);
                        clientTcpBinding.ReceiveTimeout = TimeSpan.FromSeconds(10);
                        clientTcpBinding.ReliableSession.Enabled = true;
                        clientTcpBinding.ReliableSession.InactivityTimeout = TimeSpan.FromSeconds(10);
                        
                        var clientEndpointAddress = new EndpointAddress(new Uri("net.tcp://" + Config.Ip + ":" + Config.Port + "/D2SecService"),
                            new DnsEndpointIdentity("OICD2 Server"));
                        
                        _client = new D2UserClient(_callbackInstanceContext, clientTcpBinding, clientEndpointAddress);
                        
                        if (_client.ClientCredentials != null)
                        {
                            _client.ClientCredentials.ServiceCertificate.Authentication.CertificateValidationMode = X509CertificateValidationMode.None;
                            _client.ClientCredentials.UserName.UserName = "D2_User";
                            _client.ClientCredentials.UserName.Password = new string("D2_User".ToCharArray().Reverse().ToArray());
                        }
                    }
                    catch (Exception exception)
                    {
                        if (needSaveToLog)
                        {
                            Logger?.LogTextMessage($"CreateClient() error: {exception.Message}\nStack:\n{exception.StackTrace}");
                            needSaveToLog = false;
                        }
                        Thread.Sleep(100);
                        continue;
                    }                    
                }
                try
                {                   
                    //var sw = Stopwatch.StartNew();                    
                    _client.SubscribeOnPasswordChange();
                    //sw.Stop();
                    //Debug.Print(sw.Elapsed.ToString());
                    foreach (var chan in _callbackInstanceContext.OutgoingChannels)
                    {
                        chan.Faulted += OnChannelFaulted;
                        //chan.Closed += OnChannelFaulted;
                    }                    
                }
                catch (Exception exception)
                {
                    if (_client.State == CommunicationState.Faulted)
                    {
                        _client.Abort();
                        if (needSaveToLog)
                        {
                            Logger?.LogTextMessage($"SubscribeOnPasswordChange() error: {exception.Message}");
                            needSaveToLog = false;
                        }
                        _client = null;                        
                        Thread.Sleep(100);       
                        continue;                     
                    }
                }
                Logger?.LogTextMessage("Initialize() - OK");                
                break;
            }
            ProcessCancellationPending();            
        }
        public void Close()
        {
            if(_client == null)
                return;            
            try
            {
                /*if (_client.ClientCredentials != null)
                {
                    _client.ClientCredentials.UserName.UserName = "D2_User";
                    _client.ClientCredentials.UserName.Password = "D2_User".Reverse().ToString();
                }*/
                _client.UnsubscribeFromPasswordChange();
            }
            catch (Exception exception)
            {
                _client?.Abort();
                _client = null;
                Logger?.LogTextMessage($"UnsubscribeFromPasswordChange() - error: {exception.Message}");
                return;
            }
            try
            {                
                _client?.Close();
            }
            catch (Exception)
            {
            }            
            _client = null;
        }
        public void AddPasswordProcessor(PasswordProcessor processor)
        {
            if (_passwordProcessor == null)
                _passwordProcessor = processor;
            else
            {
                if(_passwordProcessor.GetInvocationList().Contains(processor))
                    _passwordProcessor += processor;
            }
        
        }
        public void RemovePasswordProcessor(PasswordProcessor processor)
        {
            if (_passwordProcessor != null)
            {
                if (_passwordProcessor.GetInvocationList().Contains(processor))
                    _passwordProcessor -= processor;
            }
               
        }
        public void AddReconnectionProcessor(ReconnectionProcessor processor)
        {
            if (_reconnectionProcessor == null)
                _reconnectionProcessor = processor;
            else
            {
                if (_reconnectionProcessor.GetInvocationList().Contains(processor))
                    _reconnectionProcessor += processor;
            }
                
        }
        public void RemoveReconnectionProcessor(ReconnectionProcessor processor)
        {
            if (_reconnectionProcessor != null)
            {
                if (_reconnectionProcessor.GetInvocationList().Contains(processor))
                    _reconnectionProcessor -= processor;
            }                
        }        
        public void NotifyPasswordChange(byte[] newPassword)
        {
            if (_needToAddFaultHandlers)
            {
                _needToAddFaultHandlers = false;
                foreach (var chan in _callbackInstanceContext.IncomingChannels)
                {
                    chan.Faulted += OnChannelFaulted;
                    //chan.Closed += OnChannelFaulted;
                }                
            }            
            try
            {
                _passwordProcessor?.Invoke(Config.Ip, newPassword);
            }
            catch (Exception exception)
            {
                Logger?.LogTextMessage($"_processor() - invocation error: {exception.Message}");
            }
        }
        
        void OnChannelFaulted(object sender, EventArgs e)
        {
            if (_client == null)
                return;
            if (_client.State != CommunicationState.Faulted)
                return;

            Logger?.LogTextMessage($"OnChannelFaulted() called on channel: {((IContextChannel)sender).RemoteAddress} State: {_client.State}");

            //TEST            
            //Console.WriteLine($"OnChannelFaulted called.Channel {((IContextChannel)sender).RemoteAddress}state is:" + _client.State);
            //Console.WriteLine();

            try
            {
                /*if (_client.ClientCredentials != null)
                {
                    _client.ClientCredentials.UserName.UserName = "D2_User";
                    _client.ClientCredentials.UserName.Password = "D2_User".Reverse().ToString();
                }*/
                _client.Abort();
            }
            catch (Exception)
            {
            }
            _client = null;            
            if(!Config.TaskCancellationToken.Token.IsCancellationRequested)
                _reconnectionProcessor?.Invoke(this);                        
        }

        void ProcessCancellationPending()
        {
            if (_client == null)
                return;
            if (Config.TaskCancellationToken.Token.IsCancellationRequested)
            {                
                if (_client.State == CommunicationState.Faulted)
                {
                    _client.Abort();
                }
                else
                {
                    _client.Close();
                }
                _client = null;
            }
        }
    }
}
