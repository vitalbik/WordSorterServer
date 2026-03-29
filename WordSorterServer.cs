using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;
using System.Linq;

class WordSorterServer
{
    static TcpListener listener;

    public static void Main()
    {
        Int32 port = 15000;
        IPAddress localAddr = IPAddress.Parse("127.0.0.1");

        listener = new TcpListener(localAddr, port);
        listener.Start();

        Console.WriteLine("Сервер запущен на порту 15000");
        Console.WriteLine("Ожидание подключений...\n");

        while (true)
        {
            Thread t = new Thread(HandleClient);
            t.IsBackground = true;
            t.Start(listener.AcceptTcpClient());
        }
    }

    static void HandleClient(object obj)
    {
        TcpClient client = (TcpClient)obj;
        Console.WriteLine("Клиент подключился: " + client.Client.RemoteEndPoint);

        try
        {
            StreamReader sr = new StreamReader(client.GetStream());
            StreamWriter sw = new StreamWriter(client.GetStream());
            sw.AutoFlush = true;

            while (true)
            {
                // Читаем текст от клиента
                string text = sr.ReadLine();

                // Пустая строка или null = клиент отключился
                if (string.IsNullOrEmpty(text)) break;

                Console.WriteLine("Получен текст: " + text);

                // Разбиваем на слова, убираем повторения, сортируем
                string[] words = text.Split(
                    new char[] { ' ', ',', '.', '!', '?', ';', ':', '-' },
                    StringSplitOptions.RemoveEmptyEntries
                );

                List<string> sorted = words
                    .Select(w => w.ToLower())   // всё в нижний регистр
                    .Distinct()                  // убираем повторения
                    .OrderBy(w => w)             // сортируем по алфавиту
                    .ToList();

                // Отправляем слова клиенту — каждое с новой строки
                string result = string.Join("\n", sorted);
                sw.WriteLine(result);
                sw.WriteLine("---"); // маркер конца ответа
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Ошибка: " + e.Message);
        }

        Console.WriteLine("Клиент отключился: " + client.Client.RemoteEndPoint);
        client.Close();
    }
}
