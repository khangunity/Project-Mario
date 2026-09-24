using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;


public class NetworkPlayer : MonoBehaviour
{
    // =========================================
    // NETWORK
    // =========================================

    private TcpClient client;

    private NetworkStream stream;

    private Thread receiveThread;

    private int playerId = -1;


    // =========================================
    // PLAYER
    // =========================================

    public float moveSpeed = 5f;


    // =========================================
    // OTHER PLAYER
    // =========================================

    private GameObject otherPlayer;

    private Vector3 otherPlayerPosition;

    private bool hasOtherPlayerPosition = false;


    // =========================================
    // UI
    // =========================================

    public TMP_Text messageText;


    // =========================================
    // START
    // =========================================

    void Start()
    {
        ConnectToServer();
    }


    // =========================================
    // UPDATE
    // =========================================

    void Update()
    {
        if (playerId == -1)
            return;


        // Điều khiển Player của mình
        MovePlayer();


        // Gửi vị trí
        SendPosition();


        // Cập nhật Player còn lại
        UpdateOtherPlayer();
    }


    // =========================================
    // CONNECT
    // =========================================

    void ConnectToServer()
    {
        try
        {
            client =
                new TcpClient();


            client.Connect(
                "192.168.1.4",
                5000
            );


            stream =
                client.GetStream();


            Debug.Log(
                "Connected to Server!"
            );


            receiveThread =
                new Thread(
                    ReceiveData
                );


            receiveThread.IsBackground =
                true;


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


    // =========================================
    // RECEIVE DATA
    // =========================================

    void ReceiveData()
    {
        byte[] buffer =
            new byte[4096];


        while (
            client != null &&
            client.Connected
        )
        {
            try
            {
                int bytes =
                    stream.Read(
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


                foreach (
                    string msg
                    in messages
                )
                {
                    HandleMessage(
                        msg.Trim()
                    );
                }
            }
            catch
            {
                break;
            }
        }
    }


    // =========================================
    // HANDLE MESSAGE
    // =========================================

    void HandleMessage(
        string message
    )
    {
        Debug.Log(
            "Server: " +
            message
        );


        // =====================================
        // PLAYER ID
        // =====================================

        if (
            message.StartsWith(
                "PLAYER_ID|"
            )
        )
        {
            string[] parts =
                message.Split('|');


            if (parts.Length >= 2)
            {
                playerId =
                    int.Parse(
                        parts[1]
                    );


                Debug.Log(
                    "My Player ID = " +
                    playerId
                );


                // Đặt vị trí ban đầu
                if (playerId == 1)
                {
                    transform.position =
                        new Vector3(
                            -3f,
                            0.5f,
                            0f
                        );
                }
                else
                {
                    transform.position =
                        new Vector3(
                            3f,
                            0.5f,
                            0f
                        );
                }
            }
        }


        // =====================================
        // PLAYER STATE
        // =====================================

        else if (
            message.StartsWith(
                "STATE|"
            )
        )
        {
            string[] parts =
                message.Split('|');


            if (parts.Length < 5)
                return;


            int receivedPlayerId =
                int.Parse(
                    parts[1]
                );


            // Không xử lý chính mình
            if (
                receivedPlayerId ==
                playerId
            )
            {
                return;
            }


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
                new Vector3(
                    x,
                    y,
                    z
                );


            hasOtherPlayerPosition =
                true;
        }


        // =====================================
        // BUTTON
        // =====================================

        else if (
            message.StartsWith(
                "BUTTON_CLICK|"
            )
        )
        {
            string[] parts =
                message.Split('|');


            if (parts.Length < 2)
                return;


            int pressedPlayerId =
                int.Parse(
                    parts[1]
                );


            ShowButtonMessage(
                pressedPlayerId
            );
        }
    }


    // =========================================
    // MOVE
    // =========================================

    void MovePlayer()
    {
        float x = 0;

        float z = 0;


        if (
            Keyboard.current.wKey.isPressed
        )
        {
            z = 1;
        }


        if (
            Keyboard.current.sKey.isPressed
        )
        {
            z = -1;
        }


        if (
            Keyboard.current.aKey.isPressed
        )
        {
            x = -1;
        }


        if (
            Keyboard.current.dKey.isPressed
        )
        {
            x = 1;
        }


        Vector3 direction =
            new Vector3(
                x,
                0,
                z
            );


        transform.position +=
            direction *
            moveSpeed *
            Time.deltaTime;
    }


    // =========================================
    // SEND POSITION
    // =========================================

    void SendPosition()
    {
        if (
            stream == null ||
            !stream.CanWrite
        )
        {
            return;
        }


        string message =
            "MOVE|" +
            playerId +
            "|" +
            transform.position.x.ToString(
                System.Globalization.CultureInfo.InvariantCulture
            ) +
            "|" +
            transform.position.y.ToString(
                System.Globalization.CultureInfo.InvariantCulture
            ) +
            "|" +
            transform.position.z.ToString(
                System.Globalization.CultureInfo.InvariantCulture
            ) +
            "\n";


        byte[] data =
            Encoding.UTF8.GetBytes(
                message
            );


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


    // =========================================
    // UPDATE OTHER PLAYER
    // =========================================

    void UpdateOtherPlayer()
    {
        if (
            !hasOtherPlayerPosition
        )
        {
            return;
        }


        // Nếu chưa có Player kia
        // thì tạo Cube
        if (
            otherPlayer == null
        )
        {
            otherPlayer =
                GameObject.CreatePrimitive(
                    PrimitiveType.Cube
                );


            if (playerId == 1)
            {
                otherPlayer.name =
                    "Player2";
            }
            else
            {
                otherPlayer.name =
                    "Player1";
            }


            otherPlayer.transform.position =
                otherPlayerPosition;


            otherPlayer.transform.localScale =
                new Vector3(
                    1f,
                    1f,
                    1f
                );
        }


        // Di chuyển mượt
        otherPlayer.transform.position =
            Vector3.Lerp(
                otherPlayer.transform.position,
                otherPlayerPosition,
                Time.deltaTime * 10f
            );
    }


    // =========================================
    // BUTTON
    // =========================================

    public void OnButtonClick()
    {
        if (
            stream == null ||
            !stream.CanWrite
        )
        {
            Debug.LogError(
                "Not connected to Server!"
            );

            return;
        }


        string message =
            "BUTTON_CLICK\n";


        byte[] data =
            Encoding.UTF8.GetBytes(
                message
            );


        try
        {
            stream.Write(
                data,
                0,
                data.Length
            );


            Debug.Log(
                "Button clicked!"
            );
        }
        catch
        {
            Debug.LogError(
                "Cannot send button click!"
            );
        }
    }


    // =========================================
    // SHOW BUTTON MESSAGE
    // =========================================

    void ShowButtonMessage(
        int pressedPlayerId
    )
    {
        string message =
            "Player " +
            pressedPlayerId +
            " đã nhấn nút!";


        Debug.Log(
            message
        );


        // Gửi việc cập nhật UI về Main Thread
        UnityMainThreadDispatcher.Enqueue(
            () =>
            {
                if (
                    messageText != null
                )
                {
                    messageText.text =
                        message;
                }
            }
        );
    }


    // =========================================
    // QUIT
    // =========================================

    void OnApplicationQuit()
    {
        try
        {
            if (stream != null)
            {
                stream.Close();
            }


            if (client != null)
            {
                client.Close();
            }
        }
        catch
        {
        }
    }
}