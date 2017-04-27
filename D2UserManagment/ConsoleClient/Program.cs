using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using D2Helpers;
using TextFileLogger;

namespace ConsoleClient
{
    class Program
    {
        static List<Task> _subscriberCreationTasks = new List<Task>();
        static ConcurrentBag<D2UserPasswordSubscriber> _passwordSubscribers;       
        
        private static readonly object _consoleLock = new object();

        static void ShowLgnFile(string ip, byte[] password)
        {
            lock (_consoleLock)
            {
                Console.WriteLine(DateTime.Now);
                Console.WriteLine($"Pasword for {ip}:");
                if (password != null)
                    Console.WriteLine(Encoding.Default.GetString(password));
                Console.WriteLine();
            }            
        }

        static void ReconnectFaultedSubscriberChannel(D2UserPasswordSubscriber subscriber)
        {
            _subscriberCreationTasks.Add(Task.Factory.StartNew(() =>
            {                
                subscriber.Initialize();                
            }, subscriber.Config.TaskCancellationToken.Token));
        }


        static void Main(string[] args)
        {
            /*var xmlDoc = new XmlDocument();
            try
            {
                xmlDoc.Load(AppDomain.CurrentDomain.SetupInformation.ConfigurationFile);
                var selectSingleNode = xmlDoc.SelectSingleNode("//system.diagnostics/sources/source/listeners/add[@name='messages']");
                if (selectSingleNode?.Attributes != null)
                    selectSingleNode.Attributes["initializeData"].Value = (new D2PathHelper()).GetLogsPath() + "D2UserHostMessages.svclog";
                xmlDoc.Save(AppDomain.CurrentDomain.SetupInformation.ConfigurationFile);
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.Message);
                Console.ReadLine();
            }*/
            

            TextLogger _logger;            
            List<SubscriberConfiguration> _configurations = new List<SubscriberConfiguration>();

            _logger = new TextLogger { LogFilePath = new D2PathHelper().GetAssemblyFolderPath() + "D2UserClientService.txt" };
            _passwordSubscribers = new ConcurrentBag<D2UserPasswordSubscriber>();

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

            //Creating clients
            _logger.LogTextMessage("Starting client creation threads...");            
            foreach (var config in _configurations)
            {
                var subscriber = new D2UserPasswordSubscriber(config, _logger);
                _subscriberCreationTasks.Add(Task.Factory.StartNew(() =>
                {
                    subscriber.AddPasswordProcessor(ShowLgnFile);
                    subscriber.AddReconnectionProcessor(ReconnectFaultedSubscriberChannel);
                    subscriber.Initialize();
                    if (subscriber.IsAlive)
                    {                     
                        _passwordSubscribers.Add(subscriber);
                    }                    
                }, config.TaskCancellationToken.Token));                
            }
            _logger.LogTextMessage("Starting client creation threads - OK");

            
            Console.ReadLine();


            Console.WriteLine("Cancelling tasks...");
            foreach (var subConfig in _configurations)
            {
                subConfig.TaskCancellationToken.Cancel();
            }
            Console.WriteLine("Cancelling tasks  - ok");

            Console.WriteLine("Task.WaitAll...");
            try
            {
                Task.WaitAll(_subscriberCreationTasks.ToArray());
            }
            catch (Exception)
            {
            }
            Console.WriteLine("Task.WaitAll - ok");

            Console.WriteLine("Closing subscribers...");
            foreach (var subscriber in _passwordSubscribers)
            {
                subscriber.Close();
            }
            Console.WriteLine("Closing subscribers - ok");
            Console.ReadLine();
        }
    }
}
