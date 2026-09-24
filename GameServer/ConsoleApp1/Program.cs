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

    // Player ID -> Client
    static Dictionary<int, TcpClient> clients =
        new Dictionary<int, TcpClient>();

    // Player ID -> vị trí
    static Dictionary<int, string> playerPositions =
        new Dictionary<int, string>();

    static readonly object lockObject =
        new object();


    // =========================================
    // MAIN
    // =========================================

    static void Main()
    {
        int port = 5000;

        server = new TcpListener(
            IPAddress.Any,
            port
        );

        server.Start();


        Console.WriteLine("================================");
        Console.WriteLine("       GAME SERVER STARTED");
        Console.WriteLine("================================");
        Console.WriteLine("Port: " + port);
        Console.WriteLine("Waiting for players...");


        while (true)
        {
            TcpClient client =
                server.AcceptTcpClient();


            int playerId =
                GetFreePlayerId();


            // Đã đủ 2 Player
            if (playerId == -1)
            {
                Console.WriteLine(
                    "Server full! Rejecting client."
                );

                client.Close();

                continue;
            }


            lock (lockObject)
            {
                clients[playerId] = client;
            }


            Console.WriteLine(
                "Player " +
                playerId +
                " connected!"
            );


            Thread clientThread =
                new Thread(() =>
                {
                    HandleClient(
                        client,
                        playerId
                    );
                });


            clientThread.IsBackground = true;

            clientThread.Start();
        }
    }


    // =========================================
    // TÌM PLAYER ID TRỐNG
    // =========================================

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


    // =========================================
    // CLIENT
    // =========================================

    static void HandleClient(
        TcpClient client,
        int playerId
    )
    {
        try
        {
            NetworkStream stream =
                client.GetStream();


            // ---------------------------------
            // Gửi Player ID
            // ---------------------------------

            SendMessage(
                client,
                "PLAYER_ID|" +
                playerId +
                "\n"
            );


            Console.WriteLine(
                "Sent ID to Player " +
                playerId
            );


            // ---------------------------------
            // Gửi vị trí ban đầu
            // ---------------------------------

            string startPosition;


            if (playerId == 1)
            {
                startPosition =
                    "STATE|1|-3|0.5|0\n";
            }
            else
            {
                startPosition =
                    "STATE|2|3|0.5|0\n";
            }


            SendMessage(
                client,
                startPosition
            );


            // ---------------------------------
            // Gửi vị trí Player khác
            // ---------------------------------

            lock (lockObject)
            {
                foreach (
                    var pair
                    in playerPositions
                )
                {
                    SendMessage(
                        client,
                        pair.Value + "\n"
                    );
                }
            }


            // ---------------------------------
            // Đọc dữ liệu Client
            // ---------------------------------

            StreamReader reader =
                new StreamReader(
                    stream,
                    Encoding.UTF8
                );


            while (true)
            {
                string message =
                    reader.ReadLine();


                // Client disconnect
                if (message == null)
                {
                    break;
                }


                Console.WriteLine(
                    "Player " +
                    playerId +
                    ": " +
                    message
                );


                // =============================
                // MOVE
                // =============================

                if (
                    message.StartsWith(
                        "MOVE|"
                    )
                )
                {
                    HandleMove(
                        message
                    );
                }


                // =============================
                // BUTTON
                // =============================

                else if (
                    message ==
                    "BUTTON_CLICK"
                )
                {
                    HandleButtonClick(
                        playerId
                    );
                }
            }
        }
        catch
        {
            Console.WriteLine(
                "Connection error with Player " +
                playerId
            );
        }


        DisconnectPlayer(
            playerId
        );


        client.Close();
    }


    // =========================================
    // MOVE
    // =========================================

    static void HandleMove(
        string message
    )
    {
        try
        {
            string[] parts =
                message.Split('|');


            if (parts.Length < 5)
                return;


            int playerId =
                int.Parse(parts[1]);


            float x =
                float.Parse(
                    parts[2],
                    CultureInfo.InvariantCulture
                );


            float y =
                float.Parse(
                    parts[3],
                    CultureInfo.InvariantCulture
                );


            float z =
                float.Parse(
                    parts[4],
                    CultureInfo.InvariantCulture
                );


            string state =
                "STATE|" +
                playerId +
                "|" +
                x.ToString(
                    CultureInfo.InvariantCulture
                ) +
                "|" +
                y.ToString(
                    CultureInfo.InvariantCulture
                ) +
                "|" +
                z.ToString(
                    CultureInfo.InvariantCulture
                );


            // Lưu vị trí
            lock (lockObject)
            {
                playerPositions[playerId] =
                    state;
            }


            // Gửi cho tất cả Client
            Broadcast(
                state + "\n"
            );
        }
        catch
        {
            Console.WriteLine(
                "Invalid MOVE message."
            );
        }
    }


    // =========================================
    // BUTTON
    // =========================================

    static void HandleButtonClick(
        int playerId
    )
    {
        Console.WriteLine(
            "Player " +
            playerId +
            " pressed the button!"
        );


        string message =
            "BUTTON_CLICK|" +
            playerId +
            "\n";


        // Gửi cho cả 2 Client
        Broadcast(message);
    }


    // =========================================
    // SEND
    // =========================================

    static void SendMessage(
        TcpClient client,
        string message
    )
    {
        try
        {
            NetworkStream stream =
                client.GetStream();


            byte[] data =
                Encoding.UTF8.GetBytes(
                    message
                );


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


    // =========================================
    // BROADCAST
    // =========================================

    static void Broadcast(
        string message
    )
    {
        byte[] data =
            Encoding.UTF8.GetBytes(
                message
            );


        lock (lockObject)
        {
            foreach (
                var pair
                in clients
            )
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


    // =========================================
    // DISCONNECT
    // =========================================

    static void DisconnectPlayer(
        int playerId
    )
    {
        lock (lockObject)
        {
            clients.Remove(
                playerId
            );

            playerPositions.Remove(
                playerId
            );
        }


        Console.WriteLine(
            "Player " +
            playerId +
            " disconnected."
        );


        Console.WriteLine(
            "Player " +
            playerId +
            " slot is now FREE."
        );
    }
}