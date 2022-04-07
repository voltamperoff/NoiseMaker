using System.Net;
using System.Net.Sockets;

class Program
{
    private static readonly Random random = new();
    private static readonly object locker = new();

    private static IPAddress GetRandomPublicIPAddress()
    {
        lock (locker)
        {
            byte[] b = new byte[4];

            while (true)
            {
                random.NextBytes(b);

                if (b[0] == 0 || b[0] == 10 || b[0] == 127 || b[0] >= 240) continue;

                if (b[0] == 100 && b[1] >= 64 && b[1] <= 127) continue;

                if (b[0] == 169 && b[1] == 254) continue;

                if (b[0] == 172 && b[1] >= 16 && b[1] <= 31) continue;

                if (b[0] == 192 && b[1] == 0 && (b[2] == 0 || b[2] == 2)) continue;

                if (b[0] == 192 && b[1] == 88 && b[2] == 99) continue;

                if (b[0] == 192 && b[1] == 168) continue;

                if (b[0] == 198 && b[1] >= 18 && b[1] <= 19) continue;

                if (b[0] == 198 && b[1] == 51 && b[2] == 100) continue;

                if (b[0] == 203 && b[1] == 0 && b[2] == 113) continue;

                if (b[0] >= 224 && b[0] <= 239) continue;

                if (b[0] == 233 && b[1] == 252 && b[2] == 0) continue;

                break;
            }

            return new IPAddress(b);
        }
    }

    private static async Task ConnectAsync(IPAddress address)
    {
        // Ignore self-signed certificates
        var handler = new HttpClientHandler
        {
            ClientCertificateOptions = ClientCertificateOption.Manual,
            ServerCertificateCustomValidationCallback = (message, cert, chain, policy) => true
        };

        using var client = new HttpClient(handler);

        client.Timeout = TimeSpan.FromSeconds(5);

        try
        {
            Console.WriteLine($">>> Connecting to {address}");

            using var response = await client.GetAsync($"https://{address}/");

            try
            {
                var content = await response.Content.ReadAsStringAsync();

                Console.WriteLine($"<<< Got {content.Length} from {address} ({response.StatusCode})");
            }
            catch
            {
                Console.WriteLine($"--- Cannot get response from {address}");
            }
        }
        catch
        {
            Console.WriteLine($"--- Cannot connect to {address}");
        }
    }

    private static async Task ScanAsync()
    {
        while (true)
        {
            try
            {
                using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                var address = GetRandomPublicIPAddress();

                socket.ConnectAsync(new IPEndPoint(address, 443)).Wait(TimeSpan.FromMilliseconds(200));

                if (socket.Connected)
                {
                    await ConnectAsync(address);
                }

                socket.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine($"--- Socket error: {e.Message}");
            }
        }
    }

    public static void Main()
    {
        int count = 10;

        var tasks = new Task[count];

        for (int i = 0; i < count; i++)
        {
            tasks[i] = Task.Run(ScanAsync);
        }

        Task.WaitAll(tasks);
    }
}