using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.IO;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
class Program
{
    static TcpListener server;

    // Lưu các client đang kết nối
    static Dictionary<int, TcpClient> clients = new Dictionary<int, TcpClient>();

    // Khóa để tránh nhiều Thread cùng sửa dữ liệu
    static readonly object lockObject = new object();

    static void Main()
    {
        int port = 5000;

        server = new TcpListener(IPAddress.Any, port);
        server.Start();

        Console.WriteLine("================================");
        Console.WriteLine("       GAME SERVER STARTED");
        Console.WriteLine("================================");
        Console.WriteLine("Port: " + port);
        Console.WriteLine("Waiting for players...");

        while (true)
        {
            TcpClient client = server.AcceptTcpClient();

            int playerId = GetFreePlayerId();

            // Nếu đã đủ 2 người
            if (playerId == -1)
            {
                Console.WriteLine("Server full! Rejecting client.");
                client.Close();
                continue;
            }

            lock (lockObject)
            {
                clients[playerId] = client;
            }

            Console.WriteLine(
                "Player " + playerId + " connected!"
            );

            Thread clientThread = new Thread(() =>
            {
                HandleClient(client, playerId);
            });

            clientThread.Start();
        }
    }

    // Tìm Player ID còn trống
    static int GetFreePlayerId()
    {
        lock (lockObject)
        {
            for (int i = 1; i <= 2; i++)
            {
                if (!clients.ContainsKey(i))
                {
                    return i;
                }
            }
        }

        return -1;
    }

    // Xử lý một Player
    static void HandleClient(TcpClient client, int playerId)
    {
        try
        {
            NetworkStream stream = client.GetStream();

            // Gửi ID cho Unity
            SendMessage(
                client,
                "PLAYER_ID|" + playerId + "\n"
            );

            Console.WriteLine(
                "Sent ID to Player " + playerId
            );

            // Đọc dữ liệu từ Unity
            StreamReader reader = new StreamReader(
                stream,
                Encoding.UTF8
            );

            while (true)
            {
                string message = reader.ReadLine();

                // ReadLine trả về null khi client ngắt kết nối
                if (message == null)
                {
                    break;
                }

                if (message.StartsWith("MOVE|"))
                {
                    HandleMove(message);
                }
            }
        }
        catch (Exception)
        {
            // Player bị ngắt kết nối
        }

        // Player thoát
        DisconnectPlayer(playerId);

        client.Close();
    }

    // Xử lý MOVE
    static void HandleMove(string message)
    {
        try
        {
            string[] parts = message.Split('|');

            if (parts.Length < 5)
                return;

            int playerId = int.Parse(parts[1]);

            float x = float.Parse(
                parts[2],
                CultureInfo.InvariantCulture
            );

            float y = float.Parse(
                parts[3],
                CultureInfo.InvariantCulture
            );

            float z = float.Parse(
                parts[4],
                CultureInfo.InvariantCulture
            );

            Console.WriteLine(
                "Player " + playerId +
                " Position: " +
                x + ", " +
                y + ", " +
                z
            );

            // Gửi vị trí cho những Player khác
            Broadcast(
                "STATE|" +
                playerId + "|" +
                x.ToString(CultureInfo.InvariantCulture) + "|" +
                y.ToString(CultureInfo.InvariantCulture) + "|" +
                z.ToString(CultureInfo.InvariantCulture) +
                "\n"
            );
        }
        catch
        {
            Console.WriteLine("Invalid MOVE message.");
        }
    }

    // Gửi message cho một client
    static void SendMessage(TcpClient client, string message)
    {
        try
        {
            NetworkStream stream = client.GetStream();

            byte[] data = Encoding.UTF8.GetBytes(message);

            stream.Write(data, 0, data.Length);
        }
        catch
        {
        }
    }

    // Gửi message cho tất cả Player
    static void Broadcast(string message)
    {
        byte[] data = Encoding.UTF8.GetBytes(message);

        lock (lockObject)
        {
            foreach (var pair in clients)
            {
                try
                {
                    NetworkStream stream =
                        pair.Value.GetStream();

                    stream.Write(
                        data,
                        0,
                        data.Length
                    );
                }
                catch
                {
                }
            }
        }
    }

    // Xóa Player khi thoát
    static void DisconnectPlayer(int playerId)
    {
        lock (lockObject)
        {
            if (clients.ContainsKey(playerId))
            {
                clients.Remove(playerId);
            }
        }

        Console.WriteLine(
            "Player " + playerId +
            " disconnected."
        );

        Console.WriteLine(
            "Player " + playerId +
            " slot is now FREE."
        );
    }
}