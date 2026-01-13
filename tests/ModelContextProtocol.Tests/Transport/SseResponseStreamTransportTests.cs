using ModelContextProtocol.Server;
using ModelContextProtocol.Tests.Utils;
using System.IO.Pipelines;

namespace ModelContextProtocol.Tests.Transport;

public class SseResponseStreamTransportTests(ITestOutputHelper testOutputHelper) : LoggedTest(testOutputHelper)
{
    [Fact]
    public async Task Can_Customize_MessageEndpoint()
    {
        var responsePipe = new Pipe();

        await using var transport = new SseResponseStreamTransport(responsePipe.Writer.AsStream(), "/my-message-endpoint");
        var transportRunTask = transport.RunAsync(TestContext.Current.CancellationToken);

        using var responseStreamReader = new StreamReader(responsePipe.Reader.AsStream());
        var firstLine = await responseStreamReader.ReadLineAsync(
#if NET
            TestContext.Current.CancellationToken
#endif
        );
        Assert.Equal("event: endpoint", firstLine);

        var secondLine = await responseStreamReader.ReadLineAsync(
#if NET
            TestContext.Current.CancellationToken
#endif
        );
        Assert.Equal("data: /my-message-endpoint", secondLine);

        responsePipe.Reader.Complete();
        responsePipe.Writer.Complete();
    }

    [Fact]
    public async Task Sends_Heartbeats_When_Idle()
    {
        var responsePipe = new Pipe();
        var heartbeatInterval = TimeSpan.FromMilliseconds(100);

        await using var transport = new SseResponseStreamTransport(
            responsePipe.Writer.AsStream(),
            heartbeatInterval: heartbeatInterval);
        
        var transportRunTask = transport.RunAsync(TestContext.Current.CancellationToken);

        using var responseStreamReader = new StreamReader(responsePipe.Reader.AsStream());
        
        // Skip endpoint event
        await responseStreamReader.ReadLineAsync(
#if NET
            TestContext.Current.CancellationToken
#endif
        );
        await responseStreamReader.ReadLineAsync(
#if NET
            TestContext.Current.CancellationToken
#endif
        );
        await responseStreamReader.ReadLineAsync(
#if NET
            TestContext.Current.CancellationToken
#endif
        );

        // Wait for first heartbeat
        var eventLine = await responseStreamReader.ReadLineAsync(
#if NET
            TestContext.Current.CancellationToken
#endif
        );
        Assert.Equal("event: ping", eventLine);
        
        var dataLine = await responseStreamReader.ReadLineAsync(
#if NET
            TestContext.Current.CancellationToken
#endif
        );
        Assert.Equal("data: ", dataLine);

        responsePipe.Reader.Complete();
        responsePipe.Writer.Complete();
    }
}
