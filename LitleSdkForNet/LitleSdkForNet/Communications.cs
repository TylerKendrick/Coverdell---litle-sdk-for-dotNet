﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text.RegularExpressions;
using System.Threading;
using Tamir.SharpSsh.jsch;

namespace Litle.Sdk
{
    public class Communications
    {
        private static readonly object SyncLock = new object();

        public static bool ValidateServerCertificate(
            object sender,
            X509Certificate certificate,
            X509Chain chain,
            SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == SslPolicyErrors.None)
                return true;

            Console.WriteLine("Certificate error: {0}", sslPolicyErrors);

            // Do not allow this client to communicate with unauthenticated servers. 
            return false;
        }

        public void NeuterXml(ref string inputXml)
        {
            const string pattern1 = "(?i)<number>.*?</number>";
            const string pattern2 = "(?i)<accNum>.*?</accNum>";

            var rgx1 = new Regex(pattern1);
            var rgx2 = new Regex(pattern2);
            inputXml = rgx1.Replace(inputXml, "<number>xxxxxxxxxxxxxxxx</number>");
            inputXml = rgx2.Replace(inputXml, "<accNum>xxxxxxxxxx</accNum>");
        }

        public void Log(String logMessage, String logFile, bool neuter)
        {
            lock (SyncLock)
            {
                if (neuter)
                {
                    NeuterXml(ref logMessage);
                }
                var logWriter = new StreamWriter(logFile, true);
                var time = DateTime.Now;
                logWriter.WriteLine(time.ToString(CultureInfo.InvariantCulture));
                logWriter.WriteLine(logMessage + "\r\n");
                logWriter.Close();
            }
        }

        public virtual string HttpPost(string xmlRequest, Dictionary<String, String> config)
        {
            string logFile = null;
            if (config.ContainsKey("logFile"))
            {
                logFile = config["logFile"];
            }

            var uri = config["url"];
            var req = (HttpWebRequest) WebRequest.Create(uri);

            var neuter = false;
            if (config.ContainsKey("neuterAccountNums"))
            {
                neuter = ("true".Equals(config["neuterAccountNums"]));
            }

            var printxml = false;
            if (config.ContainsKey("printxml"))
            {
                if ("true".Equals(config["printxml"]))
                {
                    printxml = true;
                }
            }
            if (printxml)
            {
                Console.WriteLine(xmlRequest);
                Console.WriteLine(logFile);
            }

            //log request
            if (logFile != null)
            {
                Log(xmlRequest, logFile, neuter);
            }

            req.ContentType = "text/xml";
            req.Method = "POST";
            req.ServicePoint.MaxIdleTime = 10000;
            req.ServicePoint.Expect100Continue = false;
            if (IsProxyOn(config))
            {
                var myproxy = new WebProxy(config["proxyHost"], int.Parse(config["proxyPort"]))
                {
                    BypassProxyOnLocal = true
                };
                req.Proxy = myproxy;
            }

            // submit http request
            using (var writer = new StreamWriter(req.GetRequestStream()))
            {
                writer.Write(xmlRequest);
            }

            // read response
            var resp = req.GetResponse();
            string xmlResponse;
            using (var stream = resp.GetResponseStream())
            {
                if (stream == null) return null;
                using (var reader = new StreamReader(stream))
                {
                    xmlResponse = reader.ReadToEnd().Trim();
                }
            }
            if (printxml)
            {
                Console.WriteLine(xmlResponse);
            }

            //log response
            if (logFile != null)
            {
                Log(xmlResponse, logFile, neuter);
            }

            return xmlResponse;
        }

        public bool IsProxyOn(Dictionary<String, String> config)
        {
            return config.ContainsKey("proxyHost") && config["proxyHost"] != null && config["proxyHost"].Length > 0 &&
                   config.ContainsKey("proxyPort") && config["proxyPort"] != null && config["proxyPort"].Length > 0;
        }

        public virtual string SocketStream(string xmlRequestFilePath, string xmlResponseDestinationDirectory,
            Dictionary<String, String> config)
        {
            string url = config["onlineBatchUrl"];
            int port = Int32.Parse(config["onlineBatchPort"]);
            TcpClient tcpClient;
            SslStream sslStream;

            try
            {
                tcpClient = new TcpClient(url, port);
                sslStream = new SslStream(tcpClient.GetStream(), false, ValidateServerCertificate, null);
            }
            catch (SocketException e)
            {
                throw new LitleOnlineException("Error establishing a network connection", e);
            }

            try
            {
                sslStream.AuthenticateAsClient(url);
            }
            catch (AuthenticationException e)
            {
                tcpClient.Close();
                throw new LitleOnlineException("Error establishing a network connection - SSL Authencation failed", e);
            }

            if ("true".Equals(config["printxml"]))
            {
                Console.WriteLine("Using XML File: " + xmlRequestFilePath);
            }

            using (var readFileStream = new FileStream(xmlRequestFilePath, FileMode.Open))
            {
                int bytesRead;

                do
                {
                    var byteBuffer = new byte[1024*sizeof (char)];
                    bytesRead = readFileStream.Read(byteBuffer, 0, byteBuffer.Length);

                    sslStream.Write(byteBuffer, 0, bytesRead);
                    sslStream.Flush();
                } while (bytesRead != 0);
            }

            string batchName = Path.GetFileName(xmlRequestFilePath);
            string destinationDirectory = Path.GetDirectoryName(xmlResponseDestinationDirectory);

            Debug.Assert(destinationDirectory != null, "destinationDirectory != null");
            if (!Directory.Exists(destinationDirectory))
            {
                Directory.CreateDirectory(destinationDirectory);
            }

            if ("true".Equals(config["printxml"]))
            {
                Console.WriteLine("Writing to XML File: " + xmlResponseDestinationDirectory + batchName);
            }

            using (var writeFileStream = new FileStream(xmlResponseDestinationDirectory + batchName, FileMode.Create))
            {
                int bytesRead;

                do
                {
                    var byteBuffer = new byte[1024*sizeof (char)];
                    bytesRead = sslStream.Read(byteBuffer, 0, byteBuffer.Length);
                    writeFileStream.Write(byteBuffer, 0, bytesRead);
                } while (bytesRead > 0);
            }

            tcpClient.Close();
            sslStream.Close();

            return xmlResponseDestinationDirectory + batchName;
        }

        public virtual void FtpDropOff(string fileDirectory, string fileName, Dictionary<String, String> config)
        {
            ChannelSftp channelSftp;

            string url = config["sftpUrl"];
            string username = config["sftpUsername"];
            string password = config["sftpPassword"];
            string knownHostsFile = config["knownHostsFile"];
            string filePath = fileDirectory + fileName;

            bool printxml = config["printxml"] == "true";
            if (printxml)
            {
                Console.WriteLine("Sftp Url: " + url);
                Console.WriteLine("Username: " + username);
                Console.WriteLine("Known hosts file path: " + knownHostsFile);
            }

            var jsch = new JSch();
            jsch.setKnownHosts(knownHostsFile);

            Session session = jsch.getSession(username, url);
            session.setPassword(password);

            try
            {
                session.connect();

                Channel channel = session.openChannel("sftp");
                channel.connect();
                channelSftp = (ChannelSftp) channel;
            }
            catch (SftpException e)
            {
                throw new LitleOnlineException("Error occured while attempting to establish an SFTP connection", e);
            }
            catch (JSchException e)
            {
                throw new LitleOnlineException("Error occured while attempting to establish an SFTP connection", e);
            }

            try
            {
                if (printxml)
                {
                    Console.WriteLine("Dropping off local file " + filePath + " to inbound/" + fileName + ".prg");
                }
                channelSftp.put(filePath, "inbound/" + fileName + ".prg", ChannelSftp.OVERWRITE);
                if (printxml)
                {
                    Console.WriteLine("File copied - renaming from inbound/" + fileName + ".prg to inbound/" + fileName +
                                      ".asc");
                }
                channelSftp.rename("inbound/" + fileName + ".prg", "inbound/" + fileName + ".asc");
            }
            catch (SftpException e)
            {
                throw new LitleOnlineException("Error occured while attempting to upload and save the file to SFTP", e);
            }

            channelSftp.quit();
            session.disconnect();
        }

        public virtual void FtpPoll(string fileName, int timeout, Dictionary<string, string> config)
        {
            fileName = fileName + ".asc";
            bool printxml = config["printxml"] == "true";
            if (printxml)
            {
                Console.WriteLine("Polling for outbound result file.  Timeout set to " + timeout +
                                  "ms. File to wait for is " + fileName);
            }
            ChannelSftp channelSftp;

            string url = config["sftpUrl"];
            string username = config["sftpUsername"];
            string password = config["sftpPassword"];
            string knownHostsFile = config["knownHostsFile"];
            var jsch = new JSch();
            jsch.setKnownHosts(knownHostsFile);

            Session session = jsch.getSession(username, url);
            session.setPassword(password);

            try
            {
                session.connect();

                Channel channel = session.openChannel("sftp");
                channel.connect();
                channelSftp = (ChannelSftp) channel;
            }
            catch (SftpException e)
            {
                throw new LitleOnlineException("Error occured while attempting to establish an SFTP connection", e);
            }

            //check if file exists
            SftpATTRS sftpATTRS = null;
            var stopWatch = new Stopwatch();
            stopWatch.Start();
            do
            {
                if (printxml)
                {
                    Console.WriteLine("Elapsed time is " + stopWatch.Elapsed.TotalMilliseconds);
                }
                try
                {
                    sftpATTRS = channelSftp.lstat("outbound/" + fileName);
                    if (printxml)
                    {
                        Console.WriteLine("Attrs of file are: " + sftpATTRS);
                    }
                }
                catch (SftpException e)
                {
                    if (printxml)
                    {
                        Console.WriteLine(e.message);
                    }
                    Thread.Sleep(30000);
                }
            } while (sftpATTRS == null && stopWatch.Elapsed.TotalMilliseconds <= timeout);
        }

        public virtual void FtpPickUp(string destinationFilePath, Dictionary<String, String> config, string fileName)
        {
            ChannelSftp channelSftp;

            bool printxml = config["printxml"] == "true";

            string url = config["sftpUrl"];
            string username = config["sftpUsername"];
            string password = config["sftpPassword"];
            string knownHostsFile = config["knownHostsFile"];
            var jsch = new JSch();
            jsch.setKnownHosts(knownHostsFile);

            Session session = jsch.getSession(username, url);
            session.setPassword(password);

            try
            {
                session.connect();

                Channel channel = session.openChannel("sftp");
                channel.connect();
                channelSftp = (ChannelSftp) channel;
            }
            catch (SftpException e)
            {
                throw new LitleOnlineException("Error occured while attempting to establish an SFTP connection", e);
            }

            try
            {
                if (printxml)
                {
                    Console.WriteLine("Picking up remote file outbound/" + fileName + ".asc");
                    Console.WriteLine("Putting it at " + destinationFilePath);
                }
                channelSftp.get("outbound/" + fileName + ".asc", destinationFilePath);
                if (printxml)
                {
                    Console.WriteLine("Removing remote file output/" + fileName + ".asc");
                }
                channelSftp.rm("outbound/" + fileName + ".asc");
            }
            catch (SftpException e)
            {
                throw new LitleOnlineException(
                    "Error occured while attempting to retrieve and save the file from SFTP", e);
            }

            channelSftp.quit();
            session.disconnect();
        }

        public struct SshConnectionInfo
        {
            public string Host;
            public string User;
            public string Pass;
            public string IdentityFile;
        }
    }
}