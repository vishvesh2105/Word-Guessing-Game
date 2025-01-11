using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace GameServerService
{
    internal class GameServer
    {
        private TcpListener server;
        private bool isRunning;
        private readonly string gameDataDirectory;
        private readonly object sessionLock = new object();
        private Dictionary<string, GameSession> activeSessions;
        private readonly Logger logger;

        public GameServer(string ipAddress, int port, string gameDataDirectory)
        {
            this.gameDataDirectory = gameDataDirectory;
            IPAddress localAddr = IPAddress.Parse(ipAddress); // Case matches parameter
            server = new TcpListener(localAddr, port); // Matches configuration values passed from GameServerService
            activeSessions = new Dictionary<string, GameSession>();
            logger = new Logger();
        }

        public void Start()
        {
            try
            {
                server.Start();
                isRunning = true;
                logger.Log("Game Server started. Waiting for connections...");

                while (isRunning)
                {
                    TcpClient client = server.AcceptTcpClient();
                    logger.Log("Client connected.");
                    Task.Run(() => HandleClient(client));
                }
            }
            catch (SocketException ex)
            {
                logger.Log($"SocketException: {ex.Message}", System.Diagnostics.EventLogEntryType.Error);
            }
            finally
            {
                Stop();
            }
        }

        private void HandleClient(TcpClient client)
        {
            string clientId = Guid.NewGuid().ToString();
            GameSession session = new GameSession(clientId, gameDataDirectory);

            lock (sessionLock)
            {
                activeSessions[clientId] = session;
            }

            NetworkStream stream = client.GetStream();
            byte[] buffer = new byte[1024];

            try
            {
                string initialData = $"{session.GetGameString()}|{session.GetWordCount()}";
                byte[] initialDataBytes = Encoding.UTF8.GetBytes(initialData);
                stream.Write(initialDataBytes, 0, initialDataBytes.Length);

                while (isRunning)
                {
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break;

                    string receivedData = Encoding.UTF8.GetString(buffer, 0, bytesRead).Trim();
                    logger.Log($"Received from {clientId}: {receivedData}");

                    if (receivedData.Equals("EndGame", StringComparison.OrdinalIgnoreCase))
                    {
                        string confirmMessage = "Are you sure you want to end the session? Reply YES to confirm.";
                        byte[] confirmBytes = Encoding.UTF8.GetBytes(confirmMessage);
                        stream.Write(confirmBytes, 0, confirmBytes.Length);

                        int confirmRead = stream.Read(buffer, 0, buffer.Length);
                        string confirmResponse = Encoding.UTF8.GetString(buffer, 0, confirmRead).Trim();

                        if (confirmResponse.Equals("YES", StringComparison.OrdinalIgnoreCase))
                        {
                            lock (sessionLock)
                            {
                                activeSessions.Remove(clientId);
                            }
                            break;
                        }
                        continue;
                    }

                    string response = session.ProcessRequest(receivedData);
                    byte[] responseBytes = Encoding.UTF8.GetBytes(response);
                    stream.Write(responseBytes, 0, responseBytes.Length);
                    logger.Log($"Sent to {clientId}: {response}");
                }
            }
            catch (Exception ex)
            {
                logger.Log($"Error handling client {clientId}: {ex.Message}", System.Diagnostics.EventLogEntryType.Error);
            }
            finally
            {
                client.Close();
                lock (sessionLock)
                {
                    activeSessions.Remove(clientId);
                }
                logger.Log($"Client {clientId} disconnected.");
            }
        }

        public void Stop()
        {
            logger.Log("Stopping server...");
            isRunning = false;

            lock (sessionLock)
            {
                foreach (var session in activeSessions.Values)
                {
                    session.NotifyShutdown();
                }
                activeSessions.Clear();
            }

            server.Stop();
            logger.Log("Server stopped.");
        }
    }

    internal class GameSession
    {
        private readonly string clientId;
        private readonly string gameDataDirectory;
        private string currentString;
        private int wordsRemaining;
        private List<string> wordList;

        public GameSession(string clientId, string gameDataDirectory)
        {
            this.clientId = clientId;
            this.gameDataDirectory = gameDataDirectory;
            StartNewGame();
        }

        private void StartNewGame()
        {
            string[] gameFiles = Directory.GetFiles(gameDataDirectory, "*.txt");
            if (gameFiles.Length == 0)
                throw new InvalidOperationException("No game files found in directory.");

            string selectedFile = gameFiles[new Random().Next(gameFiles.Length)];
            string[] fileLines = File.ReadAllLines(selectedFile);

            currentString = fileLines[0];
            wordsRemaining = int.Parse(fileLines[1]);
            wordList = new List<string>(fileLines.Skip(2).ToArray());

            Console.WriteLine($"Game started for {clientId}. Words to find: {wordsRemaining}");
        }

        public string ProcessRequest(string request)
        {
            if (wordList.Contains(request))
            {
                wordList.Remove(request);
                wordsRemaining--;

                if (wordsRemaining == 0)
                {
                    StartNewGame();
                    return "Congratulations! You found all words. Starting a new game...";
                }

                return $"Correct! Words remaining: {wordsRemaining}";
            }

            return $"Incorrect! Words remaining: {wordsRemaining}";
        }

        public string GetGameString() => currentString;

        public int GetWordCount() => wordsRemaining;

        public void NotifyShutdown()
        {
            Console.WriteLine($"Notifying client {clientId} of server shutdown.");
        }
    }
}
