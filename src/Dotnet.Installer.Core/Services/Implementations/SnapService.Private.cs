using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text.Json;
using Dotnet.Installer.Core.Types;

namespace Dotnet.Installer.Core.Services.Implementations;

public partial class SnapService
{
    private SnapdRestClient? _snapdRestClient;

    private SnapdRestClient GetSnapdRestClient()
    {
        if (_snapdRestClient == null)
        {
            _snapdRestClient = new SnapdRestClient();
        }
        
        return _snapdRestClient;
    }
    
    /// <summary>
    /// HTTP client that interacts with the local snapd REST API via a Unix socket.  
    /// </summary>
    /// <see href="https://snapcraft.io/docs/snapd-api"/>
    private class SnapdRestClient : IDisposable
    {
        private const string SnapdUnixSocketPath = "/run/snapd.socket";
        
        private readonly HttpClient _httpClient;
        private readonly JsonSerializerOptions _jsonSerializerOptions;

        public SnapdRestClient()
        {
            if (!File.Exists(SnapdUnixSocketPath))
            {
                throw new ApplicationException($"Could not find the snapd unix-socket {SnapdUnixSocketPath}");
            }
            
            var httpMessageHandler = new SocketsHttpHandler
            {
                ConnectCallback = async (_, cancellationToken) =>
                {
                    var socket = new Socket(AddressFamily.Unix, SocketType.Stream, ProtocolType.IP);
                    var endpoint = new UnixDomainSocketEndPoint(SnapdUnixSocketPath);
                    await socket.ConnectAsync(endpoint, cancellationToken).ConfigureAwait(false);
                    return new NetworkStream(socket, ownsSocket: false);
                }
            };

            _httpClient = new HttpClient(httpMessageHandler);
            _httpClient.BaseAddress = new Uri("http://localhost");
            
            _jsonSerializerOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
            {
                PropertyNamingPolicy = JsonNamingPolicy.KebabCaseLower,
            };
        }

        public async Task<IImmutableList<SnapInfo>> GetInstalledSnapsAsync(CancellationToken cancellationToken)
        {
            try
            {
                var snapdResponse = await RequestAsync(requestUri: "/v2/snaps", cancellationToken);

                if (snapdResponse is { StatusCode: HttpStatusCode.OK })
                {
                    return snapdResponse.Deserialize<IImmutableList<SnapInfo>>(_jsonSerializerOptions);
                }

                var error = snapdResponse.Deserialize<SnapdError>(_jsonSerializerOptions);

                throw new ApplicationException(message: $"Snapd REST API responded with status code " +
                                                        $"{(int)snapdResponse.StatusCode} ({snapdResponse.Status}): " +
                                                        error.Message);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                throw new ApplicationException(
                    message: "An unexpected failure occured while requesting information about the local snaps",
                    innerException: exception);
            }
        }

        public async Task<SnapInfo?> GetInstalledSnapAsync(string snapName, CancellationToken cancellationToken = default)
        {
            try
            {
                var snapdResponse = await RequestAsync(
                    requestUri: $"/v2/snaps/{Uri.EscapeDataString(snapName)}",
                    cancellationToken);

                if (snapdResponse is { StatusCode: HttpStatusCode.OK })
                {
                    return snapdResponse.Deserialize<SnapInfo>(_jsonSerializerOptions);
                }

                var error = snapdResponse.Deserialize<SnapdError>(_jsonSerializerOptions);

                if (error is { Kind: "snap-not-found" })
                {
                    return null;
                }

                throw new ApplicationException(message: $"Snapd REST API responded with status code " +
                                                        $"{(int)snapdResponse.StatusCode} ({snapdResponse.Status}): " +
                                                        error.Message);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                throw new ApplicationException(
                    message: $"An unexpected failure occured while requesting information about the local snap \"{snapName}\"",
                    innerException: exception);
            }
        }

        /// <summary>
        /// Search for snaps whose name matches the given string.
        /// </summary>
        /// <param name="snapName">
        /// The name of the snap to search for. The match is exact
        /// (i.e. <see cref="FindSnapAsync"/> would return 0 or 1 results) unless the string ends in <c>*</c>
        /// </param>
        /// <param name="cancellationToken">The cancellation token to cancel the operation.</param>
        /// <returns>A list of snaps that matches the specified <paramref name="snapName"/></returns>
        /// <exception cref="ObjectDisposedException"></exception>
        /// <exception cref="ApplicationException"></exception>
        /// <exception cref="OperationCanceledException"></exception>
        /// <seealso href="https://snapcraft.io/docs/snapd-api#heading--find"/>
        public async Task<SnapInfo?> FindSnapAsync(string snapName, CancellationToken cancellationToken = default)
        {
            try
            {
                var snapdResponse = await RequestAsync(
                    requestUri: $"/v2/find?name={Uri.EscapeDataString(snapName)}", 
                    cancellationToken);

                if (snapdResponse is { StatusCode: HttpStatusCode.OK })
                {
                    return snapdResponse.Deserialize<SnapInfo[]>(_jsonSerializerOptions).Single();
                }

                var error = snapdResponse.Deserialize<SnapdError>(_jsonSerializerOptions); 
                
                if (error is { Kind: "snap-not-found" })
                {
                    return null;
                }

                throw new ApplicationException(message: $"Snapd REST API responded with status code " +
                                               $"{(int)snapdResponse.StatusCode} ({snapdResponse.Status}): " + 
                                               error.Message);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                throw new ApplicationException(
                    message: $"An unexpected failure occured while requesting information about the snap \"{snapName}\"", 
                    innerException: exception);
            }
        }

        private async Task<SnapdResponse> RequestAsync(string requestUri, CancellationToken cancellationToken)
        {
            var snapdResponse = await _httpClient
                .GetFromJsonAsync<SnapdResponse>(requestUri, _jsonSerializerOptions, cancellationToken)
                .ConfigureAwait(false);

            return snapdResponse ?? throw new ApplicationException("Snapd REST API response is null");
        }

        public void Dispose()
        {
            _httpClient.Dispose();
        }

        private record SnapdResponse(
            string Type,
            HttpStatusCode StatusCode,
            string Status,
            JsonElement Result)
        {
            public TResult Deserialize<TResult>(JsonSerializerOptions jsonDeserializerOptions)
            {
                return Result.Deserialize<TResult>(jsonDeserializerOptions)
                       ?? throw new ApplicationException("Snapd REST API result is null");
            }
            
            public bool TryGetResultAs<TResult>(
                JsonSerializerOptions jsonDeserializerOptions,
                [NotNullWhen(returnValue: true)]
                out TResult? result)
            {   
                try
                {
                    result = Result.Deserialize<TResult>(jsonDeserializerOptions);
                }
                catch
                {
                    result = default;
                }
                
                return result is not null;
            }
        }

        private record SnapdError(string Message, string? Kind);
    }
}
