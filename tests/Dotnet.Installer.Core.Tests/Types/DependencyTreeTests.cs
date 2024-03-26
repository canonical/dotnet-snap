using Dotnet.Installer.Core.Models;
using Dotnet.Installer.Core.Types;

namespace Dotnet.Installer.Core.Tests.Types;

public class DependencyTreeTests
{
    [Fact]
    public void GetReverseDependencies_WhenCalled_ShouldIdentifyReverseDependencies()
    {
        // Arrange
        var component1 = new Component
        {
            Key = "key1",
            Name = "component1",
            Description = "Component 1",
            Version = new DotnetVersion(8, 0, 100),
            BaseUrl = new Uri("http://test.com"),
            Packages = [new Package { Name = "package1", Version = "2.0" }],
            Dependencies = []
        };
        
        var component2 = new Component
        {
            Key = "key2",
            Name = "component2",
            Description = "Component 2",
            Version = new DotnetVersion(8, 0, 100),
            BaseUrl = new Uri("http://test.com"),
            Packages = [new Package { Name = "package1", Version = "2.0" }],
            Dependencies = [ "key1" ]
        };
        
        var component3 = new Component
        {
            Key = "key3",
            Name = "component3",
            Description = "Component 3",
            Version = new DotnetVersion(8, 0, 100),
            BaseUrl = new Uri("http://test.com"),
            Packages = [new Package { Name = "package1", Version = "2.0" }],
            Dependencies = [ "key2" ]
        };

        var dependencyTree = new DependencyTree([component1, component2, component3]);
        
        // Act
        var reverseDependencies1 = dependencyTree.GetReverseDependencies("key1");
        var reverseDependencies2 = dependencyTree.GetReverseDependencies("key2");
        var reverseDependencies3 = dependencyTree.GetReverseDependencies("key3");
        
        // Assert
        Assert.Equal(2, reverseDependencies1.Count);
        Assert.Single(reverseDependencies2);
        Assert.Empty(reverseDependencies3);
    }
}
