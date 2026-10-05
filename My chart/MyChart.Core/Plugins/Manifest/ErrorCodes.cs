namespace MyChart.Core.Plugins.Manifest;

/// <summary>PG1.02 / PG3.01 error codes.</summary>
public static class ErrorCodes
{
    public const string InvalidId = "E001";
    public const string DuplicateId = "E002";
    public const string DuplicateTypeId = "E003";
    public const string DuplicateToolId = "E004";
    public const string MissingIcon = "E005";
    public const string HotkeyReserved = "E006";
    public const string HotkeyConflict = "E007";
    public const string ContractIncompatible = "E008";
    public const string InvalidGeometry = "E009";
    public const string SlotInvalid = "E016";
    public const string OrderOutOfRange = "E017";
    public const string VersionInvalid = "E018";
}
