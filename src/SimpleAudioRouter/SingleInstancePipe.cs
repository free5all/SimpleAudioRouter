using System.IO;
using System.IO.Pipes;

namespace SimpleAudioRouter;

internal static class SingleInstancePipe
{
    public static void SignalShowWindow()
    {
        try
        {
            using var client = new NamedPipeClientStream(".", Program.ShowPipeName, PipeDirection.Out);
            client.Connect(500);
            using var writer = new StreamWriter(client) { AutoFlush = true };
            writer.WriteLine("SHOW");
        }
        catch
        {
            // First instance may not be listening yet.
        }
    }

    public static void StartServer(Action onShowRequested, CancellationToken cancellationToken)
    {
        _ = Task.Run(async () =>
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await using var server = new NamedPipeServerStream(
                        Program.ShowPipeName,
                        PipeDirection.In,
                        1,
                        PipeTransmissionMode.Byte,
                        PipeOptions.Asynchronous);

                    await server.WaitForConnectionAsync(cancellationToken).ConfigureAwait(false);
                    using var reader = new StreamReader(server);
                    var message = await reader.ReadLineAsync(cancellationToken).ConfigureAwait(false);
                    if (string.Equals(message, "SHOW", StringComparison.OrdinalIgnoreCase))
                        onShowRequested();
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch
                {
                    await Task.Delay(250, cancellationToken).ConfigureAwait(false);
                }
            }
        }, cancellationToken);
    }
}
