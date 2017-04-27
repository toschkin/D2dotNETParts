using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.ServiceModel;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using D2Helpers;
using TextFileLogger;

namespace D2Connection
{
    
    public partial class D2UserClientService : ServiceBase
    {
        private List<Task> _subscriberCreationTasks = new List<Task>();
        private ConcurrentBag<D2UserPasswordSubscriber> _passwordSubscribers;
        private TextLogger _logger;
        private List<SubscriberConfiguration> _configurations = new List<SubscriberConfiguration>();

        public D2UserClientService()
        {
            InitializeComponent();
            _passwordSubscribers = new ConcurrentBag<D2UserPasswordSubscriber>();
            _logger = new TextLogger {LogFilePath = new D2PathHelper().GetAssemblyFolderPath() + "D2UserClientService.log" };            
        }

        protected override void OnStart(string[] args)
        {
            _logger.LogTextMessage("Starting service...");

            LoadSettingsFromIniFile();

            CreateSubscribers();

            _logger.LogTextMessage("Starting service - OK");              
        }

        protected override void OnStop()
        {
            _logger.LogTextMessage("Stopping service...");
            
            foreach (var subConfig in _configurations)
            {
                subConfig.TaskCancellationToken.Cancel();
            }

            _logger.LogTextMessage("Task.WaitAll...");
            try
            {
                Task.WaitAll(_subscriberCreationTasks.ToArray());
            }
            catch (Exception)
            {
            }            
            _logger.LogTextMessage("Task.WaitAll - OK");

            _logger.LogTextMessage("Closing subscribers...");
            foreach (var subscriber in _passwordSubscribers)
            {
                subscriber.Close();
            }
            _logger.LogTextMessage("Closing subscribers - OK");

            _logger.LogTextMessage("Stopping service - OK");
        }

        void CreateLgnFile(string ip, byte[] password)
        {            
            string fileName = (new D2PathHelper()).GetAssemblyFolderPath() + @"Lgn" + ip + ".dat";            
            try
            {
                if (password != null)
                    File.WriteAllBytes(fileName, password);
                else
                    File.Create(fileName);
            }
            catch (Exception exception)
            {
                _logger.LogTextMessage($"Create/Write to file ({fileName}) exception:" + exception.Message);                
            }                                                       
        }

        void ReconnectFaultedSubscriberChannel(D2UserPasswordSubscriber subscriber)
        {
            _subscriberCreationTasks.Add(Task.Factory.StartNew(() =>
            {
                subscriber.Initialize();
            }, subscriber.Config.TaskCancellationToken.Token));
        }

        void LoadSettingsFromIniFile()
        {
            //Ini file loading            
            _logger.LogTextMessage("Loading ini...");
            D2IniFileHelper iniFile = new D2IniFileHelper(new D2PathHelper().GetAssemblyFolderPath() + "D2UserClientService.ini");
            for (int i = 0; i < (uint)iniFile.GetD2IniKeyValue("Addresses", "Count", 0); i++)
            {
                SubscriberConfiguration configuration = new SubscriberConfiguration
                {
                    Ip = iniFile.GetD2IniKeyValue("Addresses", $"IP{i + 1}", ""),
                    Port = (uint)iniFile.GetD2IniKeyValue("Addresses", $"Port{i + 1}", 47700),
                    TaskCancellationToken = new CancellationTokenSource()
                };
                if (_configurations.All(o => o.Ip != configuration.Ip))
                {
                    _configurations.Add(configuration);
                }
            }
            _logger.LogTextMessage("Loading ini - OK");
        }

        void CreateSubscribers()
        {
            //Creating clients
            _logger.LogTextMessage("Creating subscribers...");
            foreach (var config in _configurations)
            {
                var subscriber = new D2UserPasswordSubscriber(config, _logger);
                _subscriberCreationTasks.Add(Task.Factory.StartNew(() =>
                {
                    subscriber.AddPasswordProcessor(CreateLgnFile);
                    subscriber.AddReconnectionProcessor(ReconnectFaultedSubscriberChannel);
                    subscriber.Initialize();
                    if (subscriber.IsAlive)
                    {
                        _passwordSubscribers.Add(subscriber);
                    }
                }, config.TaskCancellationToken.Token));
            }
            _logger.LogTextMessage("Creating subscribers - OK");
        }

        static void Main()
        {
            Run(new D2UserClientService());
        }
    }
}
