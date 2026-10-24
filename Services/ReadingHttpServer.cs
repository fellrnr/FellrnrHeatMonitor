using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using FellrnrHeatMonitor.Models;

namespace FellrnrHeatMonitor.Services;

internal sealed class ReadingHttpServer : IDisposable
{
    private readonly TcpListener _listener = new(IPAddress.Any, 5080);
    private readonly Func<RestReadingSnapshot> _snapshotProvider;
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _listenerTask;

    public ReadingHttpServer(Func<RestReadingSnapshot> snapshotProvider)
    {
        _snapshotProvider = snapshotProvider;
    }

    public void Start()
    {
        if (_cancellationTokenSource is not null)
        {
            return;
        }

        _cancellationTokenSource = new CancellationTokenSource();
        _listener.Start();
        _listenerTask = Task.Run(() => ListenAsync(_cancellationTokenSource.Token));
    }

    public void Dispose()
    {
        _cancellationTokenSource?.Cancel();
        _listener.Stop();
        _cancellationTokenSource?.Dispose();
        _cancellationTokenSource = null;
    }

    private async Task ListenAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            TcpClient client;
            try
            {
                client = await _listener.AcceptTcpClientAsync(cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (SocketException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }
            catch (ObjectDisposedException) when (cancellationToken.IsCancellationRequested)
            {
                break;
            }

            _ = Task.Run(() => HandleRequestAsync(client), cancellationToken);
        }
    }

    private async Task HandleRequestAsync(TcpClient client)
    {
        using (client)
        {
            try
            {
                await using var stream = client.GetStream();
                using var reader = new StreamReader(stream, Encoding.ASCII, leaveOpen: true);
                var requestLine = await reader.ReadLineAsync();
                if (string.IsNullOrWhiteSpace(requestLine))
                {
                    return;
                }

                var requestParts = requestLine.Split(' ', 3, StringSplitOptions.RemoveEmptyEntries);
                var isReadingRequest = requestParts.Length == 3 &&
                    requestParts[0].Equals("GET", StringComparison.OrdinalIgnoreCase) &&
                    Uri.TryCreate(requestParts[1], UriKind.RelativeOrAbsolute, out var requestUri) &&
                    requestUri.IsAbsoluteUri
                        ? requestUri.AbsolutePath.Equals("/api/reading", StringComparison.OrdinalIgnoreCase)
                        : requestParts.Length == 3 && requestParts[1].Equals("/api/reading", StringComparison.OrdinalIgnoreCase);

                while (!string.IsNullOrEmpty(await reader.ReadLineAsync()))
                {
                }

                if (!isReadingRequest)
                {
                    await WriteResponseAsync(stream, HttpStatusCode.NotFound, "Not found");
                    return;
                }

                var json = JsonSerializer.Serialize(_snapshotProvider(), _jsonOptions);
                await WriteResponseAsync(stream, HttpStatusCode.OK, json, "application/json; charset=utf-8");
            }
            catch
            {
            }
        }
    }

    private static async Task WriteResponseAsync(NetworkStream stream, HttpStatusCode statusCode, string body, string contentType = "text/plain; charset=utf-8")
    {
        var bytes = Encoding.UTF8.GetBytes(body);
        var response = $"HTTP/1.1 {(int)statusCode} {statusCode}\r\n" +
                       $"Content-Type: {contentType}\r\n" +
                       $"Content-Length: {bytes.Length}\r\n" +
                       "Connection: close\r\n\r\n";
        var headerBytes = Encoding.ASCII.GetBytes(response);
        await stream.WriteAsync(headerBytes);
        await stream.WriteAsync(bytes);
    }
}
