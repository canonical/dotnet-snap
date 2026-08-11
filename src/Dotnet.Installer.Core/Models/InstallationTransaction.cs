using Dotnet.Installer.Core.Services.Contracts;

namespace Dotnet.Installer.Core.Models;

/// <summary>
/// Tracks changes performed during a <see cref="Dotnet.Installer.Core.Models.Component.Install()"/> so that they can be
/// rolled back if any step fails. Only changes made by the current installation are rolled
/// back; successfully installed dependencies are left untouched.
/// </summary>
public class InstallationTransaction(string componentKey)
{
    private readonly string _componentKey = componentKey;
    private bool _snapInstallAttempted;
    private bool _mountUnitsPlacementAttempted;
    private bool _pathUnitsPlacementAttempted;
    private bool _linkageFilePlacementAttempted;
    private bool _linkageFileExistedBeforePlacement;

    /// <summary>
    /// Marks that the content snap installation was attempted. If the installation fails,
    /// rollback will try to remove the snap if it is present.
    /// </summary>
    public void MarkSnapInstallAttempted()
    {
        _snapInstallAttempted = true;
    }

    /// <summary>
    /// Marks that the linkage file placement was attempted. If the placement fails,
    /// rollback will try to remove the linkage file only if it was created during this transaction.
    /// </summary>
    /// <param name="existedBeforePlacement">
    /// <see langword="true"/> if the linkage file already existed before the current installation attempt;
    /// <see langword="false"/> if this transaction created it.
    /// </param>
    public void MarkLinkageFilePlacementAttempted(bool existedBeforePlacement)
    {
        _linkageFilePlacementAttempted = true;
        _linkageFileExistedBeforePlacement = existedBeforePlacement;
    }

    /// <summary>
    /// Marks that the mount unit placement was attempted. If the placement fails,
    /// rollback will try to remove any mount units that may have been partially placed.
    /// </summary>
    public void MarkMountUnitsPlacementAttempted()
    {
        _mountUnitsPlacementAttempted = true;
    }

    /// <summary>
    /// Marks that the path unit placement was attempted. If the placement fails,
    /// rollback will try to remove any path units that may have been partially placed.
    /// </summary>
    public void MarkPathUnitsPlacementAttempted()
    {
        _pathUnitsPlacementAttempted = true;
    }

    public async Task Rollback(IFileService fileService, IManifestService manifestService,
        ISnapService snapService, ISystemdService systemdService, ILogger? logger = null,
        CancellationToken cancellationToken = default)
    {
        logger?.LogDebug($"Rolling back installation of {_componentKey}");

        if (_mountUnitsPlacementAttempted)
        {
            try
            {
                var component = new Component
                {
                    Key = _componentKey,
                    Name = string.Empty,
                    Description = string.Empty,
                    MajorVersion = 0,
                    IsLts = false,
                    Dependencies = []
                };
                await component.RemoveMountUnits(fileService, manifestService, systemdService, logger);
            }
            catch (Exception ex)
            {
                logger?.LogWarning($"Could not remove mount units during rollback: {ex.Message}");
            }
        }

        if (_pathUnitsPlacementAttempted)
        {
            try
            {
                var component = new Component
                {
                    Key = _componentKey,
                    Name = string.Empty,
                    Description = string.Empty,
                    MajorVersion = 0,
                    IsLts = false,
                    Dependencies = []
                };
                await component.RemovePathUnits(fileService, systemdService, logger);
            }
            catch (Exception ex)
            {
                logger?.LogWarning($"Could not remove path units during rollback: {ex.Message}");
            }
        }

        if (_linkageFilePlacementAttempted && !_linkageFileExistedBeforePlacement)
        {
            try
            {
                fileService.RemoveLinkageFile(_componentKey);
            }
            catch (Exception ex)
            {
                logger?.LogWarning($"Could not remove linkage file during rollback: {ex.Message}");
            }
        }

        if (_snapInstallAttempted && snapService.IsSnapInstalled(_componentKey))
        {
            try
            {
                var result = await snapService.Remove(_componentKey, purge: true, cancellationToken);
                if (!result.IsSuccess)
                {
                    logger?.LogWarning($"Could not remove snap during rollback: {result.StandardError}");
                }
            }
            catch (Exception ex)
            {
                logger?.LogWarning($"Could not remove snap during rollback: {ex.Message}");
            }
        }
    }
}
