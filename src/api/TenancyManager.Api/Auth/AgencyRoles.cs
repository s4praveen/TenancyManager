namespace TenancyManager.Api.Auth;

/// <summary>Entra ID app roles (also accepted by Dev auth). Values must match the app registration.</summary>
public static class AgencyRoles
{
    public const string AgencyAdmin = "AgencyAdmin";
    public const string Agent = "Agent";
    public const string Accounts = "Accounts";
    public const string ReadOnly = "ReadOnly";

    public static readonly string[] All = [AgencyAdmin, Agent, Accounts, ReadOnly];

    /// <summary>Policy: the caller holds at least one agency role (IAM-002).</summary>
    public const string AgencyUserPolicy = "AgencyUser";

    public const string RoleClaimType = "roles";
    public const string NameClaimType = "name";
    public const string UserNameClaimType = "preferred_username";
}
