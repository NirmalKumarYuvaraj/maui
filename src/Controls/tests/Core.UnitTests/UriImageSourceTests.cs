using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using IOPath = System.IO.Path;

namespace Microsoft.Maui.Controls.Core.UnitTests
{

	public class UriImageSourceTests : BaseTestFixture
	{

		public UriImageSourceTests()
		{

			networkcalls = 0;
		}

		static Random rnd = new Random();
		static int networkcalls = 0;
		static async Task<Stream> GetStreamAsync(Uri uri, CancellationToken cancellationToken)
		{
			await Task.Delay(rnd.Next(30, 2000));
			if (cancellationToken.IsCancellationRequested)
				throw new TaskCanceledException();
			networkcalls++;
			return typeof(UriImageSourceTests).Assembly.GetManifestResourceStream(uri.LocalPath.Substring(1));
		}

		[Fact(Skip = "LoadImageFromStream")]
		public void LoadImageFromStream()
		{
			IStreamImageSource loader = new UriImageSource
			{
				Uri = new Uri("http://foo.com/Images/crimson.jpg"),
			};
			Stream s0 = loader.GetStreamAsync().Result;

			Assert.Equal(79109, s0.Length);
		}

		[Fact(Skip = "SecondCallLoadFromCache")]
		public void SecondCallLoadFromCache()
		{
			IStreamImageSource loader = new UriImageSource
			{
				Uri = new Uri("http://foo.com/Images/crimson.jpg"),
			};
			Assert.Equal(0, networkcalls);

			using (var s0 = loader.GetStreamAsync().Result)
			{
				Assert.Equal(79109, s0.Length);
				Assert.Equal(1, networkcalls);
			}

			using (var s1 = loader.GetStreamAsync().Result)
			{
				Assert.Equal(79109, s1.Length);
				Assert.Equal(1, networkcalls);
			}
		}

		[Fact(Skip = "DoNotKeepFailedRetrieveInCache")]
		public void DoNotKeepFailedRetrieveInCache()
		{
			IStreamImageSource loader = new UriImageSource
			{
				Uri = new Uri("http://foo.com/missing.png"),
			};
			Assert.Equal(0, networkcalls);

			var s0 = loader.GetStreamAsync().Result;
			Assert.Null(s0);
			Assert.Equal(1, networkcalls);

			var s1 = loader.GetStreamAsync().Result;
			Assert.Null(s1);
			Assert.Equal(2, networkcalls);
		}

		[Fact(Skip = "ConcurrentCallsOnSameUriAreQueued")]
		public void ConcurrentCallsOnSameUriAreQueued()
		{
			IStreamImageSource loader = new UriImageSource
			{
				Uri = new Uri("http://foo.com/Images/crimson.jpg"),
			};
			Assert.Equal(0, networkcalls);

			var t0 = loader.GetStreamAsync();
			var t1 = loader.GetStreamAsync();

			//var s0 = t0.Result;
			using (var s1 = t1.Result)
			{
				Assert.Equal(1, networkcalls);
				Assert.Equal(79109, s1.Length);
			}
		}

		[Fact]
		public void NullUriDoesNotCrash()
		{
			var loader = new UriImageSource();
			loader.Uri = null;
		}

		[Fact]
		public void UrlHashKeyAreTheSame()
		{
			var urlHash1 = Crc64.ComputeHashString("http://www.optipess.com/wp-content/uploads/2010/08/02_Bad-Comics6-10.png?a=bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbasdasdasdasdasasdasdasdasdasd");
			var urlHash2 = Crc64.ComputeHashString("http://www.optipess.com/wp-content/uploads/2010/08/02_Bad-Comics6-10.png?a=bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbasdasdasdasdasasdasdasdasdasd");
			Assert.True(urlHash1 == urlHash2);
		}

		[Fact]
		public void UrlHashKeyAreNotTheSame()
		{
			var urlHash1 = Crc64.ComputeHashString("http://www.optipess.com/wp-content/uploads/2010/08/02_Bad-Comics6-10.png?a=bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbasdasdasdasdasasdasdasdasdasd");
			var urlHash2 = Crc64.ComputeHashString("http://www.optipess.com/wp-content/uploads/2010/08/02_Bad-Comics6-10.png?a=bbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbasdasdasdasdasasdasda");
			Assert.True(urlHash1 != urlHash2);
		}

		[Fact]
		public async Task CachingEnabled_ReturnsCachedStreamWithinValidity()
		{
			using var server = new LoopbackServer("first");

			IStreamImageSource loader = new UriImageSource
			{
				Uri = server.Uri,
				CachingEnabled = true,
				CacheValidity = TimeSpan.FromMinutes(5)
			};

			using var first = await loader.GetStreamAsync();
			var firstPayload = await ReadToEndAsync(first);

			server.Payload = "second";

			using var second = await loader.GetStreamAsync();
			var secondPayload = await ReadToEndAsync(second);

			Assert.Equal("first", firstPayload);
			Assert.Equal("first", secondPayload);
			Assert.Equal(1, server.RequestCount);
		}

		[Fact]
		public async Task CacheValidityExpired_ReloadsFromNetwork()
		{
			using var server = new LoopbackServer("first");

			IStreamImageSource loader = new UriImageSource
			{
				Uri = server.Uri,
				CachingEnabled = true,
				CacheValidity = TimeSpan.FromMilliseconds(100)
			};

			using var first = await loader.GetStreamAsync();
			var firstPayload = await ReadToEndAsync(first);

			await Task.Delay(200);
			server.Payload = "second";

			using var second = await loader.GetStreamAsync();
			var secondPayload = await ReadToEndAsync(second);

			Assert.Equal("first", firstPayload);
			Assert.Equal("second", secondPayload);
			Assert.Equal(2, server.RequestCount);
		}

		[Fact]
		public async Task CachingDisabled_AlwaysReloadsFromNetwork()
		{
			using var server = new LoopbackServer("first");

			IStreamImageSource loader = new UriImageSource
			{
				Uri = server.Uri,
				CachingEnabled = false,
				CacheValidity = TimeSpan.FromMinutes(5)
			};

			using var first = await loader.GetStreamAsync();
			var firstPayload = await ReadToEndAsync(first);

			server.Payload = "second";

			using var second = await loader.GetStreamAsync();
			var secondPayload = await ReadToEndAsync(second);

			Assert.Equal("first", firstPayload);
			Assert.Equal("second", secondPayload);
			Assert.Equal(2, server.RequestCount);
		}

		static async Task<string> ReadToEndAsync(Stream stream)
		{
			if (stream is null)
				return null;

			using var reader = new StreamReader(stream, Encoding.UTF8, true, 1024, leaveOpen: true);
			return await reader.ReadToEndAsync();
		}

		sealed class LoopbackServer : IDisposable
		{
			readonly HttpListener _listener;
			readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
			readonly Task _serveTask;
			int _requestCount;
			string _payload;

			public LoopbackServer(string initialPayload)
			{
				_payload = initialPayload;
				var port = GetOpenPort();
				Uri = new Uri($"http://127.0.0.1:{port}/image");

				_listener = new HttpListener();
				_listener.Prefixes.Add($"http://127.0.0.1:{port}/");
				_listener.Start();

				_serveTask = Task.Run(() => ServeAsync(_cancellationTokenSource.Token));
			}

			public Uri Uri { get; }

			public int RequestCount => _requestCount;

			public string Payload
			{
				get => _payload;
				set => _payload = value;
			}

			async Task ServeAsync(CancellationToken cancellationToken)
			{
				while (!cancellationToken.IsCancellationRequested)
				{
					HttpListenerContext context;
					try
					{
						context = await _listener.GetContextAsync();
					}
					catch (HttpListenerException)
					{
						if (cancellationToken.IsCancellationRequested)
							break;

						throw;
					}
					catch (ObjectDisposedException)
					{
						if (cancellationToken.IsCancellationRequested)
							break;

						throw;
					}

					Interlocked.Increment(ref _requestCount);

					var bytes = Encoding.UTF8.GetBytes(_payload);
					context.Response.ContentType = "application/octet-stream";
					context.Response.ContentLength64 = bytes.Length;
					await context.Response.OutputStream.WriteAsync(bytes, 0, bytes.Length);
					context.Response.OutputStream.Close();
				}
			}

			public void Dispose()
			{
				_cancellationTokenSource.Cancel();
				_listener.Stop();
				_listener.Close();
				try
				{
					_serveTask.GetAwaiter().GetResult();
				}
				catch
				{
				}

				_cancellationTokenSource.Dispose();
			}

			static int GetOpenPort()
			{
				using var tcpListener = new TcpListener(System.Net.IPAddress.Loopback, 0);
				tcpListener.Start();
				var endpoint = (IPEndPoint)tcpListener.LocalEndpoint;
				return endpoint.Port;
			}
		}

	}
}
