using Microsoft.AspNetCore.Identity;

namespace JoFit.Domain.Identity.Roles;

public class Role : IdentityRole<long>
{
    // The back-office roles and what each may do (BACKEND-BRIEF §4.3). An empty permission list means "everything
    // in the back-office set", which the seeder resolves against the permissions that exist.
    public const string Owner = "owner";
    public const string ContentEditor = "content-editor";
    public const string Support = "support";
    public const string Viewer = "viewer";

    /// <summary>The existing all-powerful role counts as an owner in the admin.</summary>
    public const string SuperAdmin = "superAdmin";

    /// <summary>
    /// Roles that used to exist, and the role their members move to. The catalogue editor is now part of the
    /// content editor. A manager moves to the content editor too, never up to owner, so nobody gains settings access.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string> Retired = new Dictionary<string, string>
    {
        ["catalogue-editor"] = ContentEditor,
        ["manager"] = ContentEditor,
        ["portalUser"] = "user"
    };

    public static readonly string[] StaffRoles = [Owner, ContentEditor, Support, Viewer];

    /// <summary>Every role that can sign in to the admin console.</summary>
    public static readonly string[] StaffRolesIncludingSuperAdmin = [.. StaffRoles, SuperAdmin];

    /// <summary>The permissions a back-office role can hold. Owner gets all of them.</summary>
    public static readonly string[] BackOfficeSet =
    [
        "Permissions.Catalogue.Read", "Permissions.Catalogue.Write",
        "Permissions.Rental.Read", "Permissions.Rental.Write",
        "Permissions.Orders.Read", "Permissions.Orders.Write",
        "Permissions.Customers.Read", "Permissions.Customers.Write",
        "Permissions.Coaches.Read", "Permissions.Coaches.Write",
        "Permissions.Education.Read", "Permissions.Education.Write",
        "Permissions.Reviews.Read", "Permissions.Reviews.Moderate",
        "Permissions.Content.Read", "Permissions.Content.Write",
        "Permissions.Quotations.Read", "Permissions.Quotations.Write",
        "Permissions.Community.Moderate",
        "Permissions.Support.Reply",
        "Permissions.Careers.Read", "Permissions.Careers.Write",
        "Permissions.Sports.Write",
        "Permissions.Settings.Write"
    ];

    public bool IsActive { get; set; }

    public ICollection<RolePermission> RolePermissions { get; } = [];

    public static IReadOnlyList<string> PermissionsFor(string role) => role switch
    {
        Owner => BackOfficeSet,
        // The catalogue editor and the content editor in one role.
        ContentEditor =>
        [
            "Permissions.Catalogue.Read", "Permissions.Catalogue.Write",
            "Permissions.Rental.Read", "Permissions.Rental.Write",
            "Permissions.Sports.Write",
            "Permissions.Content.Read", "Permissions.Content.Write",
            "Permissions.Community.Moderate",
            "Permissions.Reviews.Read",
            "Permissions.Careers.Read", "Permissions.Careers.Write"
        ],
        Support =>
        [
            "Permissions.Orders.Read", "Permissions.Orders.Write",
            "Permissions.Customers.Read", "Permissions.Customers.Write",
            "Permissions.Rental.Read",
            "Permissions.Quotations.Read", "Permissions.Quotations.Write",
            "Permissions.Reviews.Read",
            "Permissions.Support.Reply",
            "Permissions.Careers.Read"
        ],
        Viewer =>
        [
            "Permissions.Catalogue.Read", "Permissions.Rental.Read", "Permissions.Orders.Read",
            "Permissions.Customers.Read", "Permissions.Coaches.Read", "Permissions.Education.Read",
            "Permissions.Reviews.Read", "Permissions.Content.Read", "Permissions.Quotations.Read",
            "Permissions.Careers.Read"
        ],
        _ => []
    };
}
