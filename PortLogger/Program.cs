using System;
using System.Buffers;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using CommandLine;

namespace PortLogger
{
    internal static class Program
    {
        private static async Task Main(string[] args)
        {
            await Parser.Default.ParseArguments<Config>(args).WithParsedAsync(Run);
        }

        private static async Task Run(Config config)
        {
            var sessionId = DateTime.Now.ToString("yyyy-MM-dd HH.mm.ss");
            var folder = Path.Combine(config.DestinationFolder, sessionId);
            Directory.CreateDirectory(folder);
            
            await ListenAsync(config, sessionId);
        }

        private static async Task ListenAsync(Config config, string sessionId)
        {
            TcpListener server = null;
            try
            {
                server = new TcpListener(IPAddress.Any, config.IncomingPort);
                server.Start();

                var cts = new CancellationTokenSource();
                var task = ListenIncomingRequestsAsync(config, server, sessionId, cts.Token);
                
                Console.WriteLine("Press enter to stop.");
                Console.ReadLine();

                // Notify to stop active connection(s) and wait a small delay.
                cts.Cancel();
                await Task.Delay(100, CancellationToken.None);

                // Stop accepting new connection and make sure the task has completed.
                server.Stop();
                await task;
            }
            finally
            {
                server?.Stop();
            }
        }

        private static async Task ListenIncomingRequestsAsync(
            Config config,
            TcpListener server,
            string sessionId,
            CancellationToken ct)
        {
            var connectionId = 0;

            while (true)
            {
                TcpClient client;
                try
                {
                    client = await server.AcceptTcpClientAsync();
                }
                catch (ObjectDisposedException)
                {
                    break;
                }

                connectionId++;

                // Fire & forget call.
                HandleConnectionAsync(client, connectionId, sessionId, config, ct);
            }
        }

        private static async void HandleConnectionAsync(TcpClient client,
            int connectionId,
            string sessionId,
            Config config,
            CancellationToken ct)
        {
            Console.WriteLine(DateTimeOffset.Now + ": New connection #" + connectionId);
            try
            {
                await using var streamClient = client.GetStream();
                using var server = new TcpClient();
                await server.ConnectAsync(config.OutgoingHost, config.OutgoingPort);
                await using var streamServer = server.GetStream();

                await Task.WhenAll(
                    CopyToAsync(streamClient, streamServer, true, sessionId, connectionId, config, ct),
                    CopyToAsync(streamServer, streamClient, false, sessionId, connectionId, config, ct)
                );
            }
            catch (OperationCanceledException)
            {
                // Ignore. We are closing the connection.
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fatal exception ignored in HandleConnectionAsync to prevent stopping the app.");
                Console.WriteLine(ex);
            }

            Console.WriteLine(DateTimeOffset.Now + ": Closing connection #" + connectionId);
        }

        private static async Task CopyToAsync(Stream origin,
            Stream destination,
            bool isClient,
            string sessionId,
            int connectionId,
            Config config,
            CancellationToken ct)
        {
            var folder = Path.Combine(config.DestinationFolder, sessionId);
            var filename = connectionId + "_" + (isClient ? "client" : "server") + ".txt";
            var path = Path.Combine(folder, filename);

            const int bufferSize = 1024;
            var pool = ArrayPool<byte>.Shared;
            var buffer = pool.Rent(bufferSize);
            try
            {
                while (true)
                {
                    var bytesRead = await origin.ReadAsync(buffer, ct);
                    if (bytesRead == 0)
                        break;
                    
                    try
                    {
                        await using var stream = new FileStream(path, FileMode.Append);
                        await stream.WriteAsync(buffer, 0, bytesRead, ct);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("An error occured when appending to the file: " + path);
                        Console.WriteLine(ex);
                    }

                    await destination.WriteAsync(buffer, 0, bytesRead, ct);
                }
            }
            finally
            {
                pool.Return(buffer);
            }
        }
    }
}