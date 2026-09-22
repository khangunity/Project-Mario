using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;

class Program
{
    static TcpListener server;

    static void Main()
    {
        int port = 5000;

        server = new TcpListener(IPAddress.Any, port);
        server.Start();

        Console.WriteLine("================================");
        Console.WriteLine("       GAME SERVER STARTED");
        Console.WriteLine("================================");
        Console.WriteLine("Port: " + port);
        Console.WriteLine("Waiting for clients...");

        int playerId = 1;

        while (playerId <= 2)
        {
            TcpClient client = server.AcceptTcpClient();

            int id = playerId;

            Console.WriteLine("Player " + id + " connected!");

            Thread clientThread = new Thread(() =>
            {
                HandleClient(client, id);
            });

            clientThread.Start();

            playerId++;
        }

        Console.WriteLine("2 players connected!");
        Console.WriteLine("Game can start.");
    }

    static void HandleClient(TcpClient client, int playerId)
    {
        try
        {
            NetworkStream stream = client.GetStream();

            string message = "PLAYER_ID|" + playerId + "\n";

            byte[] data = System.Text.Encoding.UTF8.GetBytes(message);

            stream.Write(data, 0, data.Length);

            Console.WriteLine("Sent ID to Player " + playerId);

            while (true)
            {
                Thread.Sleep(100);
            }
        }
        catch
        {
            Console.WriteLine("Player " + playerId + " disconnected.");
        }
    }
}