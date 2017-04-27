using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.ServiceModel;
using System.ServiceModel.Description;
using System.ServiceModel.Security;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using D2SecService;
using System.Threading.Tasks;
using System.Xml;
using D2Helpers;
using TextFileLogger;

namespace D2UserHost
{
    public partial class D2UserHostService : ServiceBase
    {        
        private ServiceHost _host;
        private TextLogger _logger;
        private List<Task> _tasks;        
        private D2User _userPasswordServiceInstance;
        private CancellationTokenSource _timerCancellationToken = new CancellationTokenSource();
        private uint _tcpPort;
        private string _serverIp;
        private int _passwordChangeInterval;
        public D2UserHostService()
        {
            InitializeComponent();
            _logger = new TextLogger {LogFilePath = (new D2PathHelper()).GetLogsPath() + "D2UserHost.log"};
            _tasks = new List<Task>();           
        }

        protected override void OnStart(string[] args)
        {
            StartService();
        }

        protected override void OnStop()
        {
            StopService();
        }

        private void StartService()
        {
            ChangeConfigFile();

            LoadIniFile();

            CreateServiceHost();

            if (_passwordChangeInterval > 0)
            {
                StartChangingPassword();
            }
        }

        private void StopService()
        {
            if (_tasks != null)
            {
                _logger.LogTextMessage("Stopping timer thread...");
                _timerCancellationToken.Cancel();
                try
                {
                    Task.WaitAll(_tasks.ToArray());
                }
                catch (Exception)
                {                    
                }                
                _logger.LogTextMessage("Stopping timer thread - OK");
            }            

            _logger.LogTextMessage("Closing host...");
            try
            {
                _host?.Close();
                _host = null;
            }
            catch (Exception exception)
            {
                _logger.LogTextMessage($"Error while closing host:\n{exception.Message}\nStack:\n{exception.StackTrace}");
            }
            _logger.LogTextMessage("Closing host - OK");
        }

        void LoadIniFile()
        {
            _logger.LogTextMessage("Reading D2.ini...");
            var pathHelper = new D2IniFileHelper(new D2PathHelper().GetD2IniPath());
            _tcpPort = (uint)pathHelper.GetD2IniKeyValue("D2UserHost", "TcpPort", 47700);
            _serverIp = pathHelper.GetD2IniKeyValue("Servers", "ServerIP", Environment.MachineName);
            _passwordChangeInterval = pathHelper.GetD2IniKeyValue("D2UserHost", "ChangeInterval", 5);
            _logger.LogTextMessage("Reading D2.ini - OK");
        }

        void CreateServiceHost()
        {
            _logger.LogTextMessage("Creating host...");            
            try
            {
                _host?.Close();

                string addressTcp = "net.tcp://" + _serverIp + ":" + _tcpPort + "/D2SecService";
                Uri[] baseAddresses = { new Uri(addressTcp) };

                _userPasswordServiceInstance = new D2User();
                _host = new ServiceHost(_userPasswordServiceInstance, baseAddresses);

                ServiceMetadataBehavior mexBehavior = new ServiceMetadataBehavior();
                _host.Description.Behaviors.Add(mexBehavior);

                NetTcpBinding tcpBinding = new NetTcpBinding()
                {
                    ReceiveTimeout = TimeSpan.MaxValue
                };
                tcpBinding.ReliableSession.Enabled = true;
                tcpBinding.Security.Mode = SecurityMode.TransportWithMessageCredential;
                tcpBinding.Security.Message.ClientCredentialType = MessageCredentialType.UserName;

                //tcpBinding.ReliableSession.InactivityTimeout = TimeSpan.FromSeconds(10);

                _host.Credentials.UserNameAuthentication.UserNamePasswordValidationMode = UserNamePasswordValidationMode.Custom;                
                string certPath = new D2PathHelper().GetD2Path() + "Server.pfx";
                _logger.LogTextMessage($"Reading certificate: {certPath} ...");
                _host.Credentials.ServiceCertificate.Certificate = new X509Certificate2(certPath);
                _host.Credentials.UserNameAuthentication.CustomUserNamePasswordValidator = new ClientAuthenticator();
                _logger.LogTextMessage($"Reading certificate: {certPath} - OK");
                _host.AddServiceEndpoint(typeof(ID2User), tcpBinding, addressTcp);
                _host.AddServiceEndpoint(typeof(IMetadataExchange), MetadataExchangeBindings.CreateMexTcpBinding(), "mex");                
                _host.Open();
            }
            catch (Exception exception)
            {
                _logger.LogTextMessage($"Error while creating host:\n{exception.Message}\nStack:\n{exception.StackTrace}");
            }
            _logger.LogTextMessage("Creating host - OK");
        }

        void StartChangingPassword()
        {
            _logger.LogTextMessage("Starting timer thread...");

            PasswordChangeWorker worker = new PasswordChangeWorker
            {
                IpAddress = _serverIp,
                Logger = _logger,
                D2UserService = _userPasswordServiceInstance
            };

            
            Task changePasswordNowTask = Task.Factory.StartNew(() => { worker.ChangeD2UserPassword(); });
            _tasks.Add(changePasswordNowTask);

            Task timerTask = PeriodicTaskFactory.Start(() =>
            {
                worker.ChangeD2UserPassword();
            } //,intervalInMilliseconds: 20 * 1000, // fire every 20 sec...
                , intervalInMilliseconds: _passwordChangeInterval*1000, // fire every X seconds...
                synchronous: true,
                cancelToken: _timerCancellationToken.Token);
            _tasks.Add(timerTask);

            Task consoleTask = timerTask.ContinueWith(_ =>
            {
                _logger.LogTextMessage("Stopping timer thread - OK");
            });
            _tasks.Add(consoleTask);

            _logger.LogTextMessage("Starting timer thread - OK");
        }

        void ChangeConfigFile()
        {
            /*_logger.LogTextMessage("ChangeConfigFile...");
            var xmlDoc = new XmlDocument();
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
                _logger.LogTextMessage($"ChangeConfigFile error: {exception.Message}");
            }
            _logger.LogTextMessage("ChangeConfigFile - OK");*/
        }

        static void Main()
        {
            Run(new D2UserHostService());            
        }
    }    
}
