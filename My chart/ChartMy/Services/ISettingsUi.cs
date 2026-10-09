namespace ChartMy.Services;

public interface ISettingsUi
{
    void CloseWindow();
    string? PickOpenJson(string title);
    string? PickSaveJson(string title, string defaultName);
    bool Confirm(string title, string message);
    void ShowMessage(string title, string message);
}
