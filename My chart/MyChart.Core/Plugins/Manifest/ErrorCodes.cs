namespace MyChart.Core.Plugins.Manifest;

/// <summary>PG1.02 / PG2 / PG3 error codes (full list E001–E025).</summary>
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
    public const string DependencyMissing = "E010";
    public const string DependencyCycle = "E011";
    public const string HasDependents = "E012";
    public const string PathOutOfScope = "E013";
    public const string UserFileWouldBeOverwritten = "E014";
    public const string ManifestSchemaError = "E015";
    public const string SlotInvalid = "E016";
    public const string OrderOutOfRange = "E017";
    public const string VersionInvalid = "E018";
    public const string DefinitionSchemaError = "E019";
    public const string ExpressionError = "E020";
    public const string VocabularyUnsupported = "E021";
    public const string LimitExceeded = "E022";
    public const string CodeToolInUserRoot = "E023";
    public const string GitNotClean = "E024";
    public const string GitUnavailable = "E025";
}
