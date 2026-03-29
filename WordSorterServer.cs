using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Text;

class WordSorterServer
{
    static TcpListener listener;

    public static void Main()
    {
        string portStr = Environment.GetEnvironmentVariable("PORT") ?? "15000";
        int port = int.Parse(portStr);

        listener = new TcpListener(IPAddress.Any, port);
        listener.Start();

        Console.WriteLine("Сервер запущен на порту " + port);

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
            Stream stream = client.GetStream();
            StreamReader sr = new StreamReader(stream);
            StreamWriter sw = new StreamWriter(stream);
            sw.AutoFlush = true;

            string firstLine = sr.ReadLine();
            if (firstLine == null) return;

            Console.WriteLine("Получен запрос: " + firstLine);

            // Если это HTTP запрос (от Render для проверки)
            if (firstLine.StartsWith("HEAD") || firstLine.StartsWith("GET"))
            {
                // Читаем оставшиеся заголовки
                while (!string.IsNullOrEmpty(sr.ReadLine())) { }

                // Отвечаем HTTP 200 OK
                sw.WriteLine("HTTP/1.1 200 OK");
                sw.WriteLine("Content-Length: 0");
                sw.WriteLine("Connection: close");
                sw.WriteLine();
                return;
            }

            // Если это POST запрос с текстом для сортировки
            if (firstLine.StartsWith("POST"))
            {
                int contentLength = 0;
                string line;

                // Читаем заголовки
                while (!string.IsNullOrEmpty(line = sr.ReadLine()))
                {
                    if (line.ToLower().StartsWith("content-length:"))
                        contentLength = int.Parse(line.Substring(15).Trim());
                }

                // Читаем тело запроса
                char[] body = new char[contentLength];
                sr.Read(body, 0, contentLength);
                string text = new string(body);

                Console.WriteLine("Текст для сортировки: " + text);

                // Сортируем слова
                string result = SortWords(text);

                // Отправляем HTTP ответ
                byte[] responseBytes = Encoding.UTF8.GetBytes(result);
                sw.WriteLine("HTTP/1.1 200 OK");
                sw.WriteLine("Content-Type: text/plain; charset=utf-8");
                sw.WriteLine("Content-Length: " + responseBytes.Length);
                sw.WriteLine("Connection: close");
                sw.WriteLine();
                sw.Write(result);
                return;
            }

            // Обычный TCP клиент (не HTTP)
            string inputText = firstLine;
            while (true)
            {
                if (string.IsNullOrEmpty(inputText)) break;

                string sorted = SortWords(inputText);
                sw.WriteLine(sorted);
                sw.WriteLine("---");

                inputText = sr.ReadLine();
            }
        }
        catch (Exception e)
        {
            Console.WriteLine("Ошибка: " + e.Message);
        }

        client.Close();
        Console.WriteLine("Клиент отключился");
    }

    static string SortWords(string text)
    {
        string[] words = text.Split(
            new char[] { ' ', ',', '.', '!', '?', ';', ':', '-', '\r', '\n' },
            StringSplitOptions.RemoveEmptyEntries
        );

        List<string> sorted = words
            .Select(w => w.ToLower())
            .Distinct()
            .OrderBy(w => w)
            .ToList();

        return string.Join("\n", sorted);
    }
}
