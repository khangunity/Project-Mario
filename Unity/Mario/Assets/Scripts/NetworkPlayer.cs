using UnityEngine;
using UnityEngine.InputSystem;

using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public class NetworkPlayer : MonoBehaviour
{
    private TcpClient client;
    private NetworkStream stream;

    private int playerId = -1;

    public float moveSpeed = 5f;

    // Thread nhận dữ liệu từ Server
    private Thread receiveThread;

    // Vị trí của Player khác
    private Vector3 otherPlayerPosition;

    // Có nhận được vị trí Player khác chưa?
    private bool hasOtherPlayerPosition = false;


    void Start()
    {
        ConnectToServer();
    }


    void Update()
    {
        // Chưa nhận Player ID thì không cho di chuyển
        if (playerId == -1)
            return;

        // Player của chính mình
        MovePlayer();

        // Gửi vị trí cho Server
        SendPosition();

        // Cập nhật Player khác
        UpdateOtherPlayer();
    }


    // =========================
    // CONNECT SERVER
    // =========================

    void ConnectToServer()
    {
        try
        {
            client = new TcpClient();

            client.Connect("127.0.0.1", 5000);

            stream = client.GetStream();

            Debug.Log("Connected to Server!");


            // Nhận Player ID
            ReceivePlayerID();


            // Bắt đầu Thread nhận dữ liệu
            receiveThread = new Thread(ReceiveData);

            receiveThread.IsBackground = true;

            receiveThread.Start();
        }
        catch (Exception e)
        {
            Debug.LogError(
                "Cannot connect to Server: " +
                e.Message
            );
        }
    }


    // =========================
    // RECEIVE PLAYER ID
    // =========================

    void ReceivePlayerID()
    {
        byte[] buffer = new byte[1024];

        int bytes = stream.Read(
            buffer,
            0,
            buffer.Length
        );

        string message =
            Encoding.UTF8.GetString(
                buffer,
                0,
                bytes
            );

        Debug.Log("Server: " + message);


        string[] parts =
            message.Trim().Split('|');


        if (parts.Length >= 2)
        {
            playerId =
                int.Parse(parts[1]);

            Debug.Log(
                "My Player ID = " +
                playerId
            );
        }
    }


    // =========================
    // RECEIVE DATA
    // =========================

    void ReceiveData()
    {
        byte[] buffer = new byte[1024];

        while (client != null && client.Connected)
        {
            try
            {
                int bytes = stream.Read(
                    buffer,
                    0,
                    buffer.Length
                );

                if (bytes <= 0)
                    break;


                string message =
                    Encoding.UTF8.GetString(
                        buffer,
                        0,
                        bytes
                    );


                string[] messages =
                    message.Split(
                        new[] { '\n' },
                        StringSplitOptions.RemoveEmptyEntries
                    );


                foreach (string msg in messages)
                {
                    HandleMessage(msg.Trim());
                }
            }
            catch
            {
                break;
            }
        }
    }


    // =========================
    // HANDLE SERVER MESSAGE
    // =========================

    void HandleMessage(string message)
    {
        if (!message.StartsWith("STATE|"))
            return;


        string[] parts =
            message.Split('|');


        if (parts.Length < 5)
            return;


        int receivedPlayerId =
            int.Parse(parts[1]);


        // Không xử lý vị trí của chính mình
        if (receivedPlayerId == playerId)
            return;


        float x =
            float.Parse(
                parts[2],
                System.Globalization.CultureInfo.InvariantCulture
            );

        float y =
            float.Parse(
                parts[3],
                System.Globalization.CultureInfo.InvariantCulture
            );

        float z =
            float.Parse(
                parts[4],
                System.Globalization.CultureInfo.InvariantCulture
            );


        otherPlayerPosition =
            new Vector3(x, y, z);


        hasOtherPlayerPosition = true;
    }


    // =========================
    // UPDATE OTHER PLAYER
    // =========================

    void UpdateOtherPlayer()
    {
        if (!hasOtherPlayerPosition)
            return;


        // Tìm Cube của Player khác
        GameObject otherPlayer =
            GameObject.Find(
                playerId == 1
                    ? "Player2"
                    : "Player1"
            );


        if (otherPlayer != null)
        {
            otherPlayer.transform.position =
                Vector3.Lerp(
                    otherPlayer.transform.position,
                    otherPlayerPosition,
                    Time.deltaTime * 10f
                );
        }
    }


    // =========================
    // MOVE
    // =========================

    void MovePlayer()
    {
        float x = 0;
        float z = 0;


        if (Keyboard.current.wKey.isPressed)
            z = 1;

        if (Keyboard.current.sKey.isPressed)
            z = -1;

        if (Keyboard.current.aKey.isPressed)
            x = -1;

        if (Keyboard.current.dKey.isPressed)
            x = 1;


        Vector3 direction =
            new Vector3(x, 0, z);


        transform.position +=
            direction *
            moveSpeed *
            Time.deltaTime;
    }


    // =========================
    // SEND POSITION
    // =========================

    void SendPosition()
    {
        if (stream == null)
            return;


        string message =
            "MOVE|" +
            playerId + "|" +
            transform.position.x.ToString(
                System.Globalization.CultureInfo.InvariantCulture
            ) + "|" +
            transform.position.y.ToString(
                System.Globalization.CultureInfo.InvariantCulture
            ) + "|" +
            transform.position.z.ToString(
                System.Globalization.CultureInfo.InvariantCulture
            ) +
            "\n";


        byte[] data =
            Encoding.UTF8.GetBytes(message);


        try
        {
            stream.Write(
                data,
                0,
                data.Length
            );
        }
        catch
        {
            Debug.LogError(
                "Cannot send position!"
            );
        }
    }


    // =========================
    // CLOSE CONNECTION
    // =========================

    void OnApplicationQuit()
    {
        try
        {
            if (stream != null)
                stream.Close();

            if (client != null)
                client.Close();

            if (receiveThread != null &&
                receiveThread.IsAlive)
            {
                receiveThread.Abort();
            }
        }
        catch
        {
        }
    }
}