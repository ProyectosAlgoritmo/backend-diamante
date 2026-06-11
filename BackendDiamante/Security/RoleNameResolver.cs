namespace BackendDiamante.Security;

public static class RoleNameResolver
{
    public const string AdministratorRoleName = "Administrador";

    private static readonly IReadOnlyDictionary<string, string> Aliases =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["admin"] = AdministratorRoleName,
            ["cliente"] = "Cliente",
            ["operario"] = "Operario",
            ["supervisor"] = "Supervisor",
        };

    public static string Resolve(string roleName) =>
        Aliases.TryGetValue(roleName, out var resolved) ? resolved : roleName;

    public static bool IsAdministrator(string roleName) =>
        string.Equals(Resolve(roleName), AdministratorRoleName, StringComparison.OrdinalIgnoreCase);
}
