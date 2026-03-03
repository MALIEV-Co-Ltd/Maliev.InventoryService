using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Maliev.InventoryService.Api.Services;

namespace Maliev.InventoryService.Tests.Services;

/// <summary>
/// Tests for the InventoryIAMRegistrationService.
/// </summary>
public class InventoryIAMRegistrationServiceTests
{
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly Mock<ILogger<InventoryIAMRegistrationService>> _loggerMock;

    /// <summary>
    /// Initializes a new instance of the <see cref="InventoryIAMRegistrationServiceTests"/> class.
    /// </summary>
    public InventoryIAMRegistrationServiceTests()
    {
        _configurationMock = new Mock<IConfiguration>();
        _loggerMock = new Mock<ILogger<InventoryIAMRegistrationService>>();
    }

    /// <summary>
    /// Verifies that GetPermissionsForPublish returns all defined permissions.
    /// </summary>
    [Fact]
    public void GetPermissionsForPublish_ReturnsAllPermissions()
    {
        // Arrange
        var service = new InventoryIAMRegistrationService(
            _configurationMock.Object,
            _loggerMock.Object);

        // Act
        var permissions = service.GetPermissionsForPublish().ToList();

        // Assert
        Assert.NotNull(permissions);
        Assert.Equal(4, permissions.Count);
        
        var permissionIds = permissions.Select(p => p.PermissionId).ToList();
        Assert.Contains("inventory.stock.read", permissionIds);
        Assert.Contains("inventory.stock.write", permissionIds);
        Assert.Contains("inventory.batches.read", permissionIds);
        Assert.Contains("inventory.batches.write", permissionIds);
    }

    /// <summary>
    /// Verifies that GetRolesForPublish returns all predefined roles.
    /// </summary>
    [Fact]
    public void GetRolesForPublish_ReturnsAllRoles()
    {
        // Arrange
        var service = new InventoryIAMRegistrationService(
            _configurationMock.Object,
            _loggerMock.Object);

        // Act
        var roles = service.GetRolesForPublish().ToList();

        // Assert
        Assert.NotNull(roles);
        Assert.Equal(3, roles.Count);
        
        var roleIds = roles.Select(r => r.RoleId).ToList();
        Assert.Contains("roles.inventory.admin", roleIds);
        Assert.Contains("roles.inventory.manager", roleIds);
        Assert.Contains("roles.inventory.viewer", roleIds);
    }

    /// <summary>
    /// Verifies that service name is correctly set.
    /// </summary>
    [Fact]
    public void ServiceName_IsCorrect()
    {
        // Arrange
        var service = new InventoryIAMRegistrationService(
            _configurationMock.Object,
            _loggerMock.Object);

        // Assert
        Assert.Equal("inventory", service.ServiceName);
    }

    /// <summary>
    /// Verifies that admin role has all permissions.
    /// </summary>
    [Fact]
    public void GetRolesForPublish_AdminRole_HasAllPermissions()
    {
        // Arrange
        var service = new InventoryIAMRegistrationService(
            _configurationMock.Object,
            _loggerMock.Object);

        // Act
        var roles = service.GetRolesForPublish().ToList();
        var adminRole = roles.First(r => r.RoleId == "roles.inventory.admin");

        // Assert
        Assert.NotNull(adminRole);
        Assert.Equal(4, adminRole.PermissionIds.Count);
    }

    /// <summary>
    /// Verifies that manager role has correct permissions.
    /// </summary>
    [Fact]
    public void GetRolesForPublish_ManagerRole_HasCorrectPermissions()
    {
        // Arrange
        var service = new InventoryIAMRegistrationService(
            _configurationMock.Object,
            _loggerMock.Object);

        // Act
        var roles = service.GetRolesForPublish().ToList();
        var managerRole = roles.First(r => r.RoleId == "roles.inventory.manager");

        // Assert
        Assert.NotNull(managerRole);
        Assert.Equal(4, managerRole.PermissionIds.Count);
    }

    /// <summary>
    /// Verifies that viewer role has only read permissions.
    /// </summary>
    [Fact]
    public void GetRolesForPublish_ViewerRole_HasReadOnlyPermissions()
    {
        // Arrange
        var service = new InventoryIAMRegistrationService(
            _configurationMock.Object,
            _loggerMock.Object);

        // Act
        var roles = service.GetRolesForPublish().ToList();
        var viewerRole = roles.First(r => r.RoleId == "roles.inventory.viewer");

        // Assert
        Assert.NotNull(viewerRole);
        Assert.Equal(2, viewerRole.PermissionIds.Count);
        Assert.Contains("inventory.stock.read", viewerRole.PermissionIds);
        Assert.Contains("inventory.batches.read", viewerRole.PermissionIds);
    }
}
