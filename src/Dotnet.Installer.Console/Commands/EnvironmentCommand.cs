using System.CommandLine;
using System.Text;
using Dotnet.Installer.Core.Types;

namespace Dotnet.Installer.Console.Commands;

public class EnvironmentCommand : Command
{
    public EnvironmentCommand() : base("environment", "Gets information about the current environment.")
    {
        this.IsHidden = true;
        
        this.SetHandler(Handle);
    }

    private static void Handle()
    {
        var environment = new EnvironmentInformation
        {
            EffectiveUserId = Native.GetCurrentEffectiveUserId()
        };
        
        System.Console.Write(environment);
    }
    
    private class EnvironmentInformation
    {
        public int EffectiveUserId { get; init; }

        public override string ToString()
        {
            var stringBuilder = new StringBuilder();

            stringBuilder.AppendLine($"Process+{nameof(EffectiveUserId)}={EffectiveUserId}");

            return stringBuilder.ToString();
        }
    }
}

