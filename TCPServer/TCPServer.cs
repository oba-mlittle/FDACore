﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using Common;
using System.Threading;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.Security.Authentication;

namespace FDA
{
    public class TCPServer : IDisposable
    {
        private static X509Certificate2 _serverCertificate;
        private static string _serverCertificatePath = "FDAApp_TempKey.pfx";
        private static string _certificatePass = "!7\u001a\u0013_\u0017\u0005\a\u001fWz\u007f\u0004{";

        private readonly int _listeningPort;

        private readonly int _tickRate = 250; //ms
        TcpListener _server;
        Dictionary<Guid, Client> _clients;
        System.Threading.Timer _timer;
        public static Exception LastError = null;

        public int Port { get { return _listeningPort; } }

        public delegate void TCPCommandHandler(object sender, TCPCommandEventArgs e);
        public event TCPCommandHandler DataAvailable;

        public delegate void DisconnectedHandler(object sender, ClientEventArgs e);
        public event DisconnectedHandler ClientDisconnected;

        public delegate void ConnectedHandler(object sender, ClientEventArgs e);
        public event ConnectedHandler ClientConnected;


        private TCPServer(int listeningPort)
        {
            _listeningPort = listeningPort;
            _server = new TcpListener(IPAddress.Any, _listeningPort);
            _clients = new Dictionary<Guid, Client>();
        }

        public static TCPServer NewTCPServer(int listeningPort)
        {
            TCPServer newServer;
            try
            {
                newServer = new TCPServer(listeningPort);
                //FileStream fs = new FileStream(_serverCertificatePath, FileMode.Open);
                //byte[] certificateData = new byte[fs.Length];
                //fs.Read(certificateData, 0, certificateData.Length);
                //string certificatepass = Helpers.xorString("hYna6tdszeMF4Z577fqL", _certificatePass);
                //_serverCertificate = new X509Certificate2(certificateData, certificatepass);
            }
            catch (Exception ex)
            {
                LastError = ex;
                return null;
            }

            return newServer;
        }

        public void Start()
        {
            _server.Start();

            Globals.SystemManager.LogApplicationEvent(this, "", "Listening for TCP connections on port " + _listeningPort);
            _timer = new System.Threading.Timer(CheckRecieved);
            _timer.Change(_tickRate, Timeout.Infinite);
        }

        public bool Send(Guid clientID, string message)
        {
            if (!_clients.ContainsKey(clientID))
                return false; // client doesn't exist, can't send anything to it

            /*
            byte dataType;
            byte[] dataBlock = new byte[0];

            Type objectType = message.GetType();

            if (objectType == typeof(string))
            {
                dataBlock = Encoding.ASCII.GetBytes((string)message);
                dataType = 0; // ASCII = 0;
            }
            else
            {
                if (objectType == typeof(long))
                {
                    dataBlock = BitConverter.GetBytes((long)message);
                    dataType = 1;  // Long = 1
                }
                else
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    using (var ms = new MemoryStream())
                    {
                        bf.Serialize(ms, message);
                        dataBlock = ms.ToArray();
                    }
                    dataType = 2; // Binary = 2
                }
            }

            byte[] header = new byte[3];
            Array.Copy(BitConverter.GetBytes((ushort)dataBlock.Length), 0, header, 0, 2);
            header[2] = dataType;

            byte[] toSend = new byte[header.Length + dataBlock.Length];

            Array.Copy(header, toSend, header.Length);
            Array.Copy(dataBlock, 0, toSend, header.Length, dataBlock.Length);
            */


            if (_clients.ContainsKey(clientID))
            {
                Client client = _clients[clientID];
                lock (client.SendQueue)
                {
                    client.SendQueue.Enqueue(Encoding.UTF8.GetBytes(message));
                }
            }
            else
                return false;

            return true;
        }

        protected virtual void CheckRecieved(Object o)
        {
            _timer.Change(Timeout.Infinite, Timeout.Infinite);

            if (Globals.FDAStatus != Globals.AppState.Normal)
            {
                goto ResetTimer;
            }
            // check for pending connections
            if (_server.Pending())
            {
                Client client = new Client(_server.AcceptTcpClient()/*, _serverCertificate*/);
                if (client.Connected)
                {

                    client.Disconnected += ClientDisconnectedHandler;

                    _clients.Add(client.ID, client);

                    //Globals.SystemManager.LogApplicationEvent(this, "", "Accepted TCP connection from " + client.Address);
                    ClientConnected?.Invoke(this, new ClientEventArgs(client.ID, client.Address));
                }
            }

            // check for received messages
            foreach (Client client in _clients.Values)
            {
                byte[] received;
                while (client.ReceivedQueue.Count > 0)
                {
                    lock (client.ReceivedQueue)
                    {
                        received = client.ReceivedQueue.Dequeue();
                    }

                    DataAvailable?.Invoke(this, new TCPCommandEventArgs(client.ID, client.Address, received));
                }
            }

            ResetTimer:
            _timer.Change(_tickRate, Timeout.Infinite);
        }

        private void ClientDisconnectedHandler(object sender, EventArgs e)
        {
            Client dcClient = (Client)sender;
            _clients?.Remove(dcClient.ID);
            //Globals.SystemManager.LogApplicationEvent(this, "", "TCP client " + dcClient.Address + " disconnected");
            ClientDisconnected?.Invoke(dcClient, new ClientEventArgs(dcClient.ID, dcClient.Address));
        }

        private class Client : IDisposable
        {
            private TcpClient _connection;
            private string _address;
            private BackgroundWorker _workerThread;
            public Queue<byte[]> ReceivedQueue;
            public Queue<byte[]> SendQueue;
            private Guid _clientID;
            private X509Certificate2 _serverCert;

            public TcpClient Connection { get => _connection; }
            public String Address { get => _address; }
            public bool Connected { get => IsConnected(_connection.Client); }
            public Guid ID { get => _clientID; }
            public delegate void DisconnectedEventHandler(object sender, EventArgs e);
            public event DisconnectedEventHandler Disconnected;

            public delegate void DataReceivedHandler(object sender, DataReceivedEventArgs e);

            public class DataReceivedEventArgs : EventArgs
            {
                byte[] Data;

                DataReceivedEventArgs(byte[] data)
                {
                    Data = data;
                }
            }

            public Client(TcpClient client/*, X509Certificate2 serverCert*/)
            {
                _connection = client;
                //_serverCert = serverCert;
                _workerThread = new BackgroundWorker();
                _workerThread.DoWork += ClientWorker_DoWork;
                _workerThread.RunWorkerCompleted += ClientWorker_Completed;
                _workerThread.WorkerSupportsCancellation = true;
                ReceivedQueue = new Queue<byte[]>();
                SendQueue = new Queue<byte[]>();
                _workerThread.RunWorkerAsync();
                _clientID = Guid.NewGuid();
                _address = ((IPEndPoint)(_connection.Client.RemoteEndPoint)).Address.ToString();
            }

            private void ClientWorker_DoWork(object sender, DoWorkEventArgs e)
            {
                NetworkStream stream = null;
                try
                {
                    using (stream = _connection.GetStream())
                    {
                        _connection.GetStream().ReadTimeout = 1000;

                        /*
                        if (!stream.IsAuthenticated)
                        {
                            try
                            {
                                stream.AuthenticateAsServer(_serverCert, false, SslProtocols.Tls12, false);
                            }
                            catch (AuthenticationException ex)
                            {
                                Console2.WriteLine("TCPServer Authentication failed: " + ex.Message);
                            }
                        }
                        */

                        byte[] readBuffer = new byte[65535];
                        byte[] header = new byte[3];
                        int readSize;
                        int dataSize;
                        byte dataType;
                        while (Connected && IsConnected(_connection.Client) && !_workerThread.CancellationPending /*&& stream.IsAuthenticated*/)  //while the client is connected at both ends, we look for incoming messages or messages queued for sending
                        {
                            while (_connection.GetStream().DataAvailable)
                            {
                                //readSize = stream.Read(header, 0, 3);
                                //dataSize = BitConverter.ToUInt16(header, 0);
                                //dataType = header[2];

                                // read the data portion of the message
                                //readBuffer = new byte[dataSize];
                                readSize = stream.Read(readBuffer, 0, readBuffer.Length);
                                Array.Resize(ref readBuffer, readSize);

                                lock (ReceivedQueue)
                                {
                                    ReceivedQueue.Enqueue(readBuffer);
                                }
                            }

                            while (SendQueue.Count > 0)
                            {
                                byte[] toSend;
                                lock (SendQueue)
                                {
                                    toSend = SendQueue.Dequeue();
                                }
                                stream.Write(toSend, 0, toSend.Length);
                            }

                            Thread.Sleep(100);
                        }
                        stream.Close();
                        stream.Dispose();
                    }


                    e.Result = Address + " disconnected from FDA TCP server";
                }
                catch (Exception ex)
                {
                    e.Result = Address + " disconnected from FDA TCP server: " + ex.Message;
                }
                finally
                {
                    _connection.Close();
                    _connection.Dispose();
                }
            }

            private void ClientWorker_Completed(object sender, RunWorkerCompletedEventArgs e)
            {
                if (_workerThread != null)
                {
                    _workerThread.DoWork -= ClientWorker_DoWork;
                    _workerThread.RunWorkerCompleted -= ClientWorker_Completed;
                    _workerThread.Dispose();
                }
                Disconnected?.Invoke(this, new EventArgs());
            }

            /*
            bool ClientCertificateValidation(Object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
            {               
                if (sslPolicyErrors == SslPolicyErrors.None) { return true; }
                if (sslPolicyErrors == SslPolicyErrors.RemoteCertificateChainErrors) { return true; } //we don't have a proper certificate tree
                return false;
            }
            */

            private bool IsConnected(Socket socket)
            {
                try
                {
                    return !(socket.Poll(1, SelectMode.SelectRead) && socket.Available == 0);
                }
                catch (SocketException)
                {
                    return false;
                }
            }

            public void Dispose()
            {
                if (_workerThread != null)
                {
                    if (_workerThread.IsBusy)
                        _workerThread.CancelAsync();

                    while (_workerThread.IsBusy)
                        Thread.Sleep(10);
                }
                ReceivedQueue.Clear();
                SendQueue.Clear();
            }
        }


        public class ClientEventArgs : EventArgs
        {
            private readonly Guid _clientID;
            private readonly string _hostname;

            public Guid ClientID { get { return _clientID; } }


            internal ClientEventArgs(Guid clientID, string host)
            {
                _clientID = clientID;
                _hostname = host;
            }
        }


        public class TCPCommandEventArgs : EventArgs
        {
            private readonly byte[] _data;
            private readonly Guid _clientID;
            private readonly string _host;

            internal TCPCommandEventArgs(Guid client, string host, byte[] data)
            {
                _data = data;
                _clientID = client;
                _host = host;

            }

            public byte[] Data { get { return _data; } }

            public Guid ClientID { get { return _clientID; } }
            public string Host { get { return _host; } }

            public string Message
            {
                get { return Message; }
            }

        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    _timer.Change(Timeout.Infinite, Timeout.Infinite);
                    Client[] clients = _clients.Values.ToArray();
                    foreach (Client client in clients)
                        client.Dispose();
                }
                _clients.Clear();
                _clients = null;

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~TCPServer() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion


    }
}

