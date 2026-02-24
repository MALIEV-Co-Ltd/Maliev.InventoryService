using Maliev.Aspire.ServiceDefaults.IAM;
using Maliev.InventoryService.Api.Authorization;

namespace Maliev.InventoryService.Api.Services;

/// <summary>
/// Background service to register permissions and roles with the IAM service on startup.
/// </summary>
public class InventoryIAMRegistrationService : IAMRegistrationService
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InventoryIAMRegistrationService"/> class.
    /// </summary>
    /// <param name="configuration">The configuration.</param>
    /// <param name="logger">The logger.</param>
    public InventoryIAMRegistrationService(IConfiguration configuration, ILogger<InventoryIAMRegistrationService> logger)
        : base(configuration, logger, "inventory")
    {
    }

    /// <inheritdoc/>
    protected override IEnumerable<PermissionRegistration> GetPermissions()
    {
        return InventoryPermissions.AllWithDescriptions.Select(p => new PermissionRegistration
        {
            PermissionId = p.Key,
            Description = p.Value
        });
    }

    /// <inheritdoc/>
    protected override IEnumerable<RoleRegistration> GetPredefinedRoles()
    {
        return InventoryPredefinedRoles.All.Select(r => new RoleRegistration
        {
            RoleId = r.RoleId,
            Description = r.Description,
            PermissionIds = r.Permissions.ToList(),
            IsCustom = false
        });
    }
}
