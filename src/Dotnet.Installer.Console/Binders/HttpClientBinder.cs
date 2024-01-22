using System.CommandLine.Binding;

namespace Dotnet.Installer.Console.Binders;

public class HttpClientBinder : BinderBase<HttpClient>
{
    protected override HttpClient GetBoundValue(BindingContext bindingContext)
        => GetHttpClient(bindingContext);

    HttpClient GetHttpClient(BindingContext bindingContext)
    {
        var client = new HttpClient
        {
            BaseAddress = new Uri("http://10.83.58.1:3000/")
        };

        return client;
    }
}
