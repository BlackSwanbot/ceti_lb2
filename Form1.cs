using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace UDPMessageExample
{
    public class UDPMessage
    {
        public bool IsCheck { get; set; } // Флаг проверки соединения
        public int Length { get; set; }   // Длина сообщения
        public byte[] Message { get; set; } // Текстовое сообщение
    }

    class Program
    {
        static string localIP;
        static int localPort = 11000;
        static string serverIp = "127.0.0.1"; // Замените на реальный IP-адрес сервера
        static int serverPort = 12000;

        static void Main(string[] args)
        {
            // Получаем локальный IP адрес
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
            {
                socket.Connect("8.8.8.8", 65530);
                IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                localIP = endPoint.Address.ToString();
            }

            Console.WriteLine($"Локальный IP: {localIP}");

            // Запускаем серверную часть
            Task.Run(() =>
            {
                ServerThread();
            });

            // Отображаем меню и запускаем клиентскую часть
            ShowMenu();
        }

        static void ShowMenu()
        {
            Console.WriteLine("Меню:");
            Console.WriteLine("1. Отправить сообщение");
            Console.WriteLine("2. Проверить соединение");
            Console.WriteLine("3. Выход");

            Console.Write("Выберите действие: ");
            string choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    SendMessage();
                    break;
                case "2":
                    CheckConnection();
                    break;
                case "3":
                    Environment.Exit(0); // Завершение программы
                    break;
                default:
                    Console.WriteLine("Неверный выбор. Попробуйте снова.");
                    ShowMenu(); // Повторное отображение меню
                    break;
            }
        }

        static void SendMessage()
        {
            Console.Write("Введите сообщение: ");
            string input = Console.ReadLine();

            UDPMessage message = new UDPMessage
            {
                IsCheck = false,
                Length = Encoding.UTF8.GetByteCount(input),
                Message = Encoding.UTF8.GetBytes(input)
            };

            SendUdpMessage(message);

            ShowMenu(); // Возвращаемся в меню после отправки
        }

        static void CheckConnection()
        {
            UDPMessage checkMessage = new UDPMessage
            {
                IsCheck = true,
                Length = 0,
                Message = null
            };

            SendUdpMessage(checkMessage);

            ShowMenu(); // Возвращаемся в меню после отправки
        }

        static void SendUdpMessage(UDPMessage message)
        {
            try
            {
                using (var client = new UdpClient())
                {
                    client.Connect(IPAddress.Parse(serverIp), serverPort);

                    // Сериализация объекта UDPMessage в байтовый массив
                    byte[] serializedMessage = Serialize(message);
                    client.Send(serializedMessage, serializedMessage.Length);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при отправке сообщения: {ex.Message}");
            }
        }

        static byte[] Serialize(UDPMessage message)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write(message.IsCheck);
                    writer.Write(message.Length);
                    writer.Write(message.Message);
                }
                return stream.ToArray();
            }
        }

        static UDPMessage Deserialize(byte[] bytes)
        {
            using (MemoryStream stream = new MemoryStream(bytes))
            {
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    UDPMessage message = new UDPMessage
                    {
                        IsCheck = reader.ReadBoolean(),
                        Length = reader.ReadInt32(),
                        Message = reader.ReadBytes(reader.BaseStream.Length - sizeof(bool) - sizeof(int))
                    };
                    return message;
                }
            }
        }

        static void ServerThread()
        {
            UdpClient udpServer = new UdpClient(localPort);

            while (true)
            {
                IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
                var data = udpServer.Receive(ref remoteEP);

                UDPMessage receivedMessage = Deserialize(data);

                if (receivedMessage.IsCheck)
                {
                    // Ответ на проверку соединения
                    UDPMessage response = new UDPMessage
                    {
                        IsCheck = false,
                        Length = Encoding.UTF8.GetByteCount("Проверка пройдена"),
                        Message = Encoding.UTF8.GetBytes("Проверка пройдена")
                    };

                    SendUdpMessage(response);
                }
                else
                {
                    // Вывод принятого сообщения
                    string decodedMessage = Encoding.UTF8.GetString(receivedMessage.Message);

                    if (receivedMessage.Length != receivedMessage.Message.Length)
                    {
                        Console.WriteLine("Сообщение повреждено!");
                    }
                    else
                    {
                        Console.WriteLine($"Получено сообщение: {decodedMessage}");
                    }
                }
            }
        }
    }
}