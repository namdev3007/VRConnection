using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using System.Collections;
using System;

public class ConnectUIScript : MonoBehaviour
{
    [SerializeField]
    private Button hostButton;

    [SerializeField]
    private Button clientButton;

    private UdpClient udpClient;
    private const int broadcastPort = 47777;
    private bool isListening = true;

    // Queue để xử lý trên main thread
    private readonly Queue<string> serverIpQueue = new Queue<string>();

    private void Start()
    {
        hostButton.onClick.AddListener(HostButtonOnClick);
        clientButton.onClick.AddListener(ClientButtonOnClick);
    }

    private void HostButtonOnClick()
    {
        NetworkManager.Singleton.StartHost();
        StartCoroutine(BroadcastServerIP());
    }

    private void ClientButtonOnClick()
    {
        StartListeningForServer();
    }

    private IEnumerator BroadcastServerIP()
    {
        using (UdpClient udpServer = new UdpClient())
        {
            udpServer.EnableBroadcast = true;

            while (true)
            {
                try
                {
                    string localIP = GetLocalIPAddress();
                    string broadcastMessage = "ServerHere:" + localIP;
                    byte[] data = Encoding.UTF8.GetBytes(broadcastMessage);
                    udpServer.Send(data, data.Length, new IPEndPoint(IPAddress.Broadcast, broadcastPort));
                    Debug.Log("Broadcasting server IP: " + localIP);
                }
                catch (SocketException ex)
                {
                    Debug.LogError("Broadcast error: " + ex.Message);
                }

                yield return new WaitForSeconds(1f); // Broadcast mỗi 1 giây
            }
        }
    }

    private void StartListeningForServer()
    {
        udpClient = new UdpClient(broadcastPort);
        udpClient.EnableBroadcast = true;

        Thread listenerThread = new Thread(ListenForServerBroadcast);
        listenerThread.Start();
    }

    private void ListenForServerBroadcast()
    {
        try
        {
            while (isListening)
            {
                try
                {
                    IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, broadcastPort);
                    byte[] data = udpClient.Receive(ref remoteEP); // Chờ gói tin UDP

                    string message = Encoding.UTF8.GetString(data);
                    if (message.StartsWith("ServerHere"))
                    {
                        string serverIP = message.Split(':')[1];
                        Debug.Log("Discovered server at: " + serverIP);

                        // Queue IP của server cho main thread
                        lock (serverIpQueue)
                        {
                            serverIpQueue.Enqueue(serverIP);
                        }
                    }
                }
                catch (SocketException ex)
                {
                    if (isListening) // Log lỗi nếu đang lắng nghe
                    {
                        Debug.LogError("Error while listening for server: " + ex.Message);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError("Fatal error in server broadcast listener: " + ex.Message);
        }
        finally
        {
            udpClient?.Close();
        }
    }

    private void ConnectToServer(string serverIP)
    {
        Debug.Log("Connecting to server: " + serverIP);

        // Gán địa chỉ IP server vào UnityTransport
        NetworkManager.Singleton.GetComponent<Unity.Netcode.Transports.UTP.UnityTransport>().ConnectionData.Address = serverIP;

        // Bắt đầu Client
        NetworkManager.Singleton.StartClient();
    }

    private void Update()
    {
        // Xử lý IP server từ thread khác
        lock (serverIpQueue)
        {
            while (serverIpQueue.Count > 0)
            {
                string serverIP = serverIpQueue.Dequeue();
                ConnectToServer(serverIP);
            }
        }
    }

    private string GetLocalIPAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                return ip.ToString();
            }
        }
        throw new System.Exception("No network adapters with an IPv4 address in the system!");
    }

    private void OnDestroy()
    {
        isListening = false;
        udpClient?.Close();
    }
}
