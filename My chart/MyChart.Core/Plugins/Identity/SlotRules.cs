using MyChart.Core.Models.Plugins;

namespace MyChart.Core.Plugins.Identity;

public static class SlotRules
{
    public static bool IsValid(Slot slot) => Enum.IsDefined(slot);

    public static bool IsValidName(string? name) =>
        !string.IsNullOrEmpty(name) && Enum.TryParse<Slot>(name, true, out _);

    public static string? ValidateOrder(int order) =>
        order is >= 0 and <= 999 ? null : Manifest.ErrorCodes.OrderOutOfRange;
}
