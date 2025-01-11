using System;
using System.ServiceProcess;
using System.Configuration;
using System.IO;
using System.Threading.Tasks;

namespace GameServerService
{
    public partial class GameServerService : ServiceBase
    {
        private GameServer server;
        private readonly Logger logger;

        public GameServerService()
        {
            InitializeComponent();
            logger = new Logger(); // Initialize logger
        }

        protected override void OnStart(string[] args)
        {
            string gameDataDirectory = ConfigurationManager.AppSettings["gameDataDirectory"];
            string ipAddress = ConfigurationManager.AppSettings["ipAddress"];
            int port = int.Parse(ConfigurationManager.AppSettings["port"]);

            logger.Log("Service is starting.");

            try
            {
                // Run the server initialization in a separate task
                Task.Run(() =>
                {
                    try
                    {
                        server = new GameServer(ipAddress, port, gameDataDirectory);
                        server.Start();
                        logger.Log("Game Server started successfully.");
                    }
                    catch (Exception ex)
                    {
                        logger.Log($"Error in GameServer.Start: {ex.Message}", System.Diagnostics.EventLogEntryType.Error);
                        Stop(); // Stop the service if the server fails to start
                    }
                });
            }
            catch (Exception ex)
            {
                logger.Log($"Error initializing service: {ex.Message}", System.Diagnostics.EventLogEntryType.Error);
                Stop(); // Stop the service if initialization fails
            }
        }

        protected override void OnStop()
        {
            logger.Log("Service is stopping.");
            if (server != null)
            {
                server.Stop();
                logger.Log("Game Server stopped successfully.");
            }
        }
    }
}
