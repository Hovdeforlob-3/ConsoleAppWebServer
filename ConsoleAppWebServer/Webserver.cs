using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
namespace ConsoleAppWebServer
{
    class Webserver
    {
        #region Variabler / dictionary
        public bool running = false;

        private int timeout = 8;
        private Encoding charEncoder = Encoding.UTF8;
        private Socket serverSocket;
        private string folderPath;

        private Dictionary<string, string> extensions = new Dictionary<string, string>()
        {
            { "htm", "text/html" },
            { "html", "text/html" },
            { "xml", "text/xml" },
            { "txt", "text/plain" },
            { "css", "text/css" },
            { "png", "image/png" },
            { "gif", "image/gif" },
            { "jpg", "image/jpg" },
            { "jpeg", "image/jpeg" },
            { "zip", "application/zip"}
        };
        #endregion

        /// <summary>
        /// denne metode starter en webserver som føste laver en connction og der efter laver en thread til de forskille responser 
        /// </summary>
        /// <param name="ipAddress"></param>
        /// <param name="port"></param>
        /// <param name="maxNoumberOfConfig"></param>
        /// <param name="folderpath"></param>
        /// <returns></returns>
        public bool start(IPAddress ipAddress, int port, int maxNoumberOfConfig, string folderpath)
        {
            if (running) return false;

            try
            {
                serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                serverSocket.Bind(new IPEndPoint(ipAddress, port));
                serverSocket.Listen(maxNoumberOfConfig);
                serverSocket.ReceiveTimeout = timeout;
                serverSocket.SendTimeout = timeout;
                running = true;
                this.folderPath = folderpath;
            }
            catch { return false; }


            Thread requestListenerT = new Thread(() =>
            {
                while (running)
                {
                    Socket clientSocket;
                    try
                    {
                        clientSocket = serverSocket.Accept();

                        Thread requestHandler = new Thread(() =>
                        {
                            clientSocket.ReceiveTimeout = timeout;
                            clientSocket.SendTimeout = timeout;
                            try { handleTheRequest(clientSocket); }
                            catch
                            {
                                try { clientSocket.Close(); } catch { }
                            }
                        });
                        requestHandler.Start();
                    }
                    catch { }
                }
            });
            requestListenerT.Start();

            return true;
        }

        /// <summary>
        /// denne metode tjekker om webserver køre og hvis den gør lukker den for serversocketen 
        /// </summary>
        public void stop()
        {
            if (running)
            {
                running = false;
                try { serverSocket.Close(); }
                catch { }
                serverSocket = null;
            }
        }

        private void handleTheRequest(Socket clientSocket)
        {
            byte[] buffer = new byte[10240];
            int receivedBCount = clientSocket.Receive(buffer);
            string strReceived = charEncoder.GetString(buffer, 0, receivedBCount);

            string httpMethod = strReceived.Substring(0, strReceived.IndexOf(" "));

            int start = strReceived.IndexOf(httpMethod) + httpMethod.Length + 1;
            int length = strReceived.LastIndexOf("HTTP") - start - 1;
            string requestedUrl = strReceived.Substring(start, length);

            string requestedFile;
            if (httpMethod.Equals("GET") || httpMethod.Equals("POST"))
                requestedFile = requestedUrl.Split('?')[0];
            else
            {
                notImplemented(clientSocket);
                return;
            }

            requestedFile = requestedFile.Replace("/", @"\").Replace("\\..", "");
            start = requestedFile.LastIndexOf('.') + 1;
            if (start > 0)
            {
                length = requestedFile.Length - start;
                string extension = requestedFile.Substring(start, length);
                if (extensions.ContainsKey(extension))
                    if (File.Exists(folderPath + requestedFile))

                        sendOkResponse(clientSocket,
                          File.ReadAllBytes(folderPath + requestedFile), extensions[extension]);
                    else
                        notFound(clientSocket);
            }
            else
            {
                if (requestedFile.Substring(length - 1, 1) != @"\")
                    requestedFile += @"\";
                if (File.Exists(folderPath + requestedFile + "index.htm"))
                    sendOkResponse(clientSocket,
                      File.ReadAllBytes(folderPath + requestedFile + "\\index.htm"), "text/html");
                else if (File.Exists(folderPath + requestedFile + "index.html"))
                    sendOkResponse(clientSocket,
                      File.ReadAllBytes(folderPath + requestedFile + "\\index.html"), "text/html");
                else
                    notFound(clientSocket);
            }
        }

        #region response metoder (som sender en response til en web browseren)
        private void notImplemented(Socket clientSocket)
        {
            sendResponse(clientSocket, " <html><head><meta" +
                " http - equiv =\"Content-Type\" content=\"text/html; " +
                "charset = utf - 8\"> </ head >< body >< h2 > Atasoy Simple Web" +
                "Server </ h2 >< div > 501 - Method Not " +
                "Implemented </ div ></ body ></ html > ",
                "501 Not Implemented", "text/html");
        }

        private void notFound(Socket clientSocket)
        {
            sendResponse(clientSocket, "<html><head><meta " +
                "http - equiv =\"Content-Type\" content=\"text/html;" +
                "charset = utf - 8\"></head><body><h2>Atasoy Simple Web " +
                "Server </ h2 >< div > 404 - Not " +
                "Found </ div ></ body ></ html > ",
                "404 Not Found", "text/html");
        }

        private void sendOkResponse(Socket clientSocket, byte[] bContent, string contentType)
        {
            sendResponse(clientSocket, bContent, "200 OK", contentType);
        }

        private void sendResponse(Socket clientSocket, string strContent, string responseCode, string contentType)
        {
            byte[] bContent = charEncoder.GetBytes(strContent);
            sendResponse(clientSocket, bContent, responseCode, contentType);
        }

        private void sendResponse(Socket clientSocket, byte[] bContent, string responseCode, string contentType)
        {
            try
            {
                byte[] bHeader = charEncoder.GetBytes(
                                    "HTTP/1.1 " + responseCode + "\r\n"
                                  + "Server: Atasoy Simple Web Server\r\n"
                                  + "Content-Length: " + bContent.Length.ToString() + "\r\n"
                                  + "Connection: close\r\n"
                                  + "Content-Type: " + contentType + "\r\n\r\n");
                clientSocket.Send(bHeader);
                clientSocket.Send(bContent);
                clientSocket.Close();
            }
            catch { }
        }
        #endregion
    }
}
