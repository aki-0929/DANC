using DisplayAdapterNameChanger.Models;
using DisplayAdapterNameChanger.Core;

using Spectre.Console;

namespace DisplayAdapterNameChanger;

class Program
{
    private static readonly DisplayAdapterEnumerator _adapterService = new();
    private static readonly RegistryOperator _Registry = new();
    private static readonly ConfigManager _ConfigManager = new();
    private static readonly BackupManager _BackupManager;
    private static readonly Localization _localization = new();

    static Program()
    {
        _BackupManager = new BackupManager(_ConfigManager);
    }

    static void Main(string[] args)
    {
        // Load saved language preference or auto-detect from system
        var savedLanguage = _ConfigManager.GetLanguage();
        _localization.LoadLanguage(savedLanguage);

        // Check administrator privileges
        if (!PrivilegeManager.IsAdministrator())
        {
            AnsiConsole.MarkupLine($"[red]{_localization.GetString("errors.adminRequired")}[/]");
            AnsiConsole.MarkupLine($"[yellow]{_localization.GetString("errors.runAsAdmin")}[/]");
            AnsiConsole.MarkupLine($"[dim]{_localization.GetString("errors.restartingAsAdmin")}[/]");
            
            try
            {
                PrivilegeManager.RestartAsAdministrator();
            }
            catch
            {
                // If restart fails, show error and exit
                AnsiConsole.MarkupLine($"[red]{_localization.GetString("errors.restartFailed")}[/]");
                Console.ReadKey();
            }
            
            return;
        }

        while (true)
        {
            AnsiConsole.Clear();
            
            AnsiConsole.Write(
                new FigletText("DANC")
                    .LeftJustified()
                    .Color(Color.Cyan));

            AnsiConsole.MarkupLine("[dim]Display Adapter Name Changer[/]");
            AnsiConsole.MarkupLine("[dim]Author: huandonger@gmail.com[/]\n");

            var adapters = _adapterService.EnumerateDisplayAdapters();
            
            if (adapters.Count == 0)
            {
                AnsiConsole.MarkupLine($"[red]{_localization.GetString("errors.noAdaptersFound")}[/]");
                Console.ReadKey();
                return;
            }

            var menu = new SelectionPrompt<string>()
                .Title($"[cyan]{_localization.GetString("menu.selectOperation")}[/]")
                .AddChoices(new[]
                {
                    _localization.GetString("menu.viewAdapterList"),
                    _localization.GetString("menu.modifyAdapterName"),
                    _localization.GetString("menu.backupManagement"),
                    _localization.GetString("menu.languageSettings"),
                    _localization.GetString("menu.exit")
                });

            var choice = AnsiConsole.Prompt(menu);

            switch (choice)
            {
                case var c when c == _localization.GetString("menu.viewAdapterList"):
                    ShowAdapterList(adapters);
                    break;
                case var c when c == _localization.GetString("menu.modifyAdapterName"):
                    ModifyAdapterName(adapters);
                    break;
                case var c when c == _localization.GetString("menu.backupManagement"):
                    BackupManagement();
                    break;
                case var c when c == _localization.GetString("menu.languageSettings"):
                    LanguageSettings();
                    break;
                case var c when c == _localization.GetString("menu.exit"):
                    AnsiConsole.MarkupLine($"[green]{_localization.GetString("success.goodbye")}[/]");
                    return;
            }
        }
    }

    static void ShowAdapterList(List<DisplayAdapter> adapters)
    {
        var table = new Table();
        table.AddColumn($"[cyan]{_localization.GetString("table.no")}[/]");
        table.AddColumn($"[cyan]{_localization.GetString("table.deviceId")}[/]");
        table.AddColumn($"[cyan]{_localization.GetString("table.currentName")}[/]");

        for (int i = 0; i < adapters.Count; i++)
        {
            table.AddRow(
                (i + 1).ToString(),
                adapters[i].DeviceId,
                adapters[i].CurrentName
            );
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[dim]{_localization.GetString("common.pressAnyKey")}[/]");
        Console.ReadKey();
        AnsiConsole.Clear();
    }

    static void ModifyAdapterName(List<DisplayAdapter> adapters)
    {
        if (adapters.Count == 0)
        {
            AnsiConsole.MarkupLine($"[red]{_localization.GetString("errors.noAdaptersAvailable")}[/]");
            AnsiConsole.MarkupLine($"[dim]{_localization.GetString("common.pressAnyKey")}[/]");
            Console.ReadKey();
            AnsiConsole.Clear();
            return;
        }

        // Create selection list
        var choices = adapters.Select((a, i) => 
            $"[[{i + 1}]] {a.CurrentName}").ToList();
        choices.Add(_localization.GetString("menu.back"));

        var selection = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title($"[cyan]{_localization.GetString("adapter.selectToModify")}[/]")
                .AddChoices(choices));

        if (selection == _localization.GetString("menu.back"))
        {
            AnsiConsole.Clear();
            return;
        }

        var index = choices.IndexOf(selection);
        if (index < 0 || index >= adapters.Count)
        {
            AnsiConsole.Clear();
            return;
        }

        var adapter = adapters[index];

        AnsiConsole.MarkupLine($"[green]{_localization.GetString("adapter.currentName")}[/] {adapter.CurrentName}");
        
        var newName = AnsiConsole.Ask<string>($"[cyan]{_localization.GetString("adapter.enterNewName")}[/]");

        if (string.IsNullOrWhiteSpace(newName))
        {
            AnsiConsole.MarkupLine($"[yellow]{_localization.GetString("errors.nameEmpty")}[/]");
            AnsiConsole.MarkupLine($"[dim]{_localization.GetString("common.pressAnyKey")}[/]");
            Console.ReadKey();
            AnsiConsole.Clear();
            return;
        }

        // Confirm operation (default: No)
        if (!AnsiConsole.Confirm($"[yellow]{_localization.GetString("adapter.confirmChange", newName)}[/]", false))
        {
            AnsiConsole.MarkupLine($"[dim]{_localization.GetString("adapter.operationCancelled")}[/]");
            AnsiConsole.MarkupLine($"[dim]{_localization.GetString("common.pressAnyKey")}[/]");
            Console.ReadKey();
            AnsiConsole.Clear();
            return;
        }

        // Create backup before modification (only if not exists)
        var (backupId, isNew) = _BackupManager.CreateBackup(adapter);
        if (backupId == null)
        {
            AnsiConsole.MarkupLine($"[yellow]{_localization.GetString("adapter.backupFailed")}[/]");
            if (!AnsiConsole.Confirm($"[yellow]{_localization.GetString("adapter.continueWithoutBackup")}[/]", false))
            {
                AnsiConsole.MarkupLine($"[dim]{_localization.GetString("adapter.operationCancelled")}[/]");
                AnsiConsole.MarkupLine($"[dim]{_localization.GetString("common.pressAnyKey")}[/]");
                Console.ReadKey();
                AnsiConsole.Clear();
                return;
            }
        }
        else
        {
            if (isNew)
            {
                AnsiConsole.MarkupLine($"[green]{_localization.GetString("adapter.backupCreated", backupId)}[/]");
            }
            else
            {
                AnsiConsole.MarkupLine($"[green]{_localization.GetString("adapter.usingExistingBackup")}[/]");
            }
        }

        // Execute modification
        if (_Registry.ModifyDeviceName(adapter, newName))
        {
            AnsiConsole.MarkupLine($"[green]{_localization.GetString("adapter.nameModified")}[/]");
            AnsiConsole.MarkupLine($"[yellow]{_localization.GetString("adapter.noteRestart")}[/]");
        }
        else
        {
            AnsiConsole.MarkupLine($"[red]{_localization.GetString("errors.modificationFailed")}[/]");
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[dim]{_localization.GetString("common.pressAnyKey")}[/]");
        Console.ReadKey();
        AnsiConsole.Clear();
    }

    static void BackupManagement()
    {
        var backups = _BackupManager.GetAllBackups();

        if (backups.Count == 0)
        {
            AnsiConsole.MarkupLine($"[yellow]{_localization.GetString("errors.noBackupsFound")}[/]");
            AnsiConsole.MarkupLine($"[dim]{_localization.GetString("common.pressAnyKey")}[/]");
            Console.ReadKey();
            AnsiConsole.Clear();
            return;
        }

        var menu = new SelectionPrompt<string>()
            .Title($"[cyan]{_localization.GetString("backup.management")}[/]")
            .AddChoices(new[]
            {
                _localization.GetString("backup.viewAll"),
                _localization.GetString("backup.restore"),
                _localization.GetString("backup.delete"),
                _localization.GetString("menu.back")
            });

        var choice = AnsiConsole.Prompt(menu);

        switch (choice)
        {
            case var c when c == _localization.GetString("backup.viewAll"):
                ShowBackupList(backups);
                break;
            case var c when c == _localization.GetString("backup.restore"):
                RestoreFromBackup(backups);
                break;
            case var c when c == _localization.GetString("backup.delete"):
                DeleteBackup(backups);
                break;
            case var c when c == _localization.GetString("menu.back"):
                AnsiConsole.Clear();
                return;
        }
    }

    static void ShowBackupList(List<BackupInfo> backups)
    {
        var table = new Table();
        table.AddColumn($"[cyan]{_localization.GetString("table.no")}[/]");
        table.AddColumn($"[cyan]{_localization.GetString("table.backupId")}[/]");
        table.AddColumn($"[cyan]{_localization.GetString("table.deviceId")}[/]");
        table.AddColumn($"[cyan]{_localization.GetString("table.originalName")}[/]");
        table.AddColumn($"[cyan]{_localization.GetString("table.createdTime")}[/]");

        for (int i = 0; i < backups.Count; i++)
        {
            table.AddRow(
                (i + 1).ToString(),
                backups[i].BackupId,
                backups[i].DeviceId,
                backups[i].OriginalName,
                backups[i].CreatedTime.ToString("yyyy-MM-dd HH:mm:ss")
            );
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[dim]{_localization.GetString("common.pressAnyKey")}[/]");
        Console.ReadKey();
        AnsiConsole.Clear();
    }

    static void RestoreFromBackup(List<BackupInfo> backups)
    {
        if (backups.Count == 0)
        {
            AnsiConsole.MarkupLine($"[red]{_localization.GetString("errors.noBackupsAvailable")}[/]");
            AnsiConsole.MarkupLine($"[dim]{_localization.GetString("common.pressAnyKey")}[/]");
            Console.ReadKey();
            AnsiConsole.Clear();
            return;
        }

        var choices = backups.Select((b, i) => 
            $"[[{i + 1}]] {b.BackupId} - {b.OriginalName} ({b.CreatedTime:yyyy-MM-dd HH:mm:ss})").ToList();
        choices.Add(_localization.GetString("menu.back"));

        var selection = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title($"[cyan]{_localization.GetString("backup.selectToRestore")}[/]")
                .AddChoices(choices));

        if (selection == _localization.GetString("menu.back"))
        {
            AnsiConsole.Clear();
            return;
        }

        var index = choices.IndexOf(selection);
        if (index < 0 || index >= backups.Count)
        {
            AnsiConsole.Clear();
            return;
        }

        var backup = backups[index];

        if (!AnsiConsole.Confirm($"[yellow]{_localization.GetString("confirm.restoreBackup", backup.BackupId)}[/]", false))
        {
            AnsiConsole.MarkupLine($"[dim]{_localization.GetString("adapter.operationCancelled")}[/]");
            AnsiConsole.MarkupLine($"[dim]{_localization.GetString("common.pressAnyKey")}[/]");
            Console.ReadKey();
            AnsiConsole.Clear();
            return;
        }

        var backupData = _BackupManager.GetBackup(backup.BackupId);
        if (backupData == null)
        {
            AnsiConsole.MarkupLine($"[red]{_localization.GetString("errors.backupNotFound")}[/]");
        }
        else if (_BackupManager.RestoreBackup(backup.BackupId))
        {
            AnsiConsole.MarkupLine($"[green]{_localization.GetString("success.backupRestored")}[/]");
            AnsiConsole.MarkupLine($"[yellow]{_localization.GetString("adapter.noteRestart")}[/]");
        }
        else
        {
            AnsiConsole.MarkupLine($"[red]{_localization.GetString("errors.deviceRemoved")}[/]");
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[dim]{_localization.GetString("common.pressAnyKey")}[/]");
        Console.ReadKey();
        AnsiConsole.Clear();
    }

    static void DeleteBackup(List<BackupInfo> backups)
    {
        if (backups.Count == 0)
        {
            AnsiConsole.MarkupLine($"[red]{_localization.GetString("errors.noBackupsAvailable")}[/]");
            AnsiConsole.MarkupLine($"[dim]{_localization.GetString("common.pressAnyKey")}[/]");
            Console.ReadKey();
            AnsiConsole.Clear();
            return;
        }

        var choices = backups.Select((b, i) => 
            $"[[{i + 1}]] {b.BackupId} - {b.OriginalName} ({b.CreatedTime:yyyy-MM-dd HH:mm:ss})").ToList();
        choices.Add(_localization.GetString("menu.back"));

        var selection = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title($"[cyan]{_localization.GetString("backup.selectToDelete")}[/]")
                .AddChoices(choices));

        if (selection == _localization.GetString("menu.back"))
        {
            AnsiConsole.Clear();
            return;
        }

        var index = choices.IndexOf(selection);
        if (index < 0 || index >= backups.Count)
        {
            AnsiConsole.Clear();
            return;
        }

        var backup = backups[index];

        if (!AnsiConsole.Confirm($"[red]{_localization.GetString("confirm.deleteBackup", backup.BackupId)}[/]", false))
        {
            AnsiConsole.MarkupLine($"[dim]{_localization.GetString("adapter.operationCancelled")}[/]");
            AnsiConsole.MarkupLine($"[dim]{_localization.GetString("common.pressAnyKey")}[/]");
            Console.ReadKey();
            AnsiConsole.Clear();
            return;
        }

        if (_BackupManager.DeleteBackup(backup.BackupId))
        {
            AnsiConsole.MarkupLine($"[green]{_localization.GetString("success.backupDeleted")}[/]");
        }
        else
        {
            AnsiConsole.MarkupLine($"[red]{_localization.GetString("errors.deleteFailed")}[/]");
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"[dim]{_localization.GetString("common.pressAnyKey")}[/]");
        Console.ReadKey();
        AnsiConsole.Clear();
    }

    static void LanguageSettings()
    {
        var availableLanguages = _localization.GetAvailableLanguages();
        var choices = availableLanguages.Select(lang => 
            $"{_localization.GetLanguageDisplayName(lang)} ({lang})").ToList();
        choices.Add(_localization.GetString("menu.back"));

        var selection = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title($"[cyan]{_localization.GetString("menu.languageSettings")}[/]")
                .AddChoices(choices));

        if (selection == _localization.GetString("menu.back"))
        {
            AnsiConsole.Clear();
            return;
        }

        var selectedIndex = choices.IndexOf(selection);
        if (selectedIndex >= 0 && selectedIndex < availableLanguages.Count)
        {
            var selectedLanguage = availableLanguages[selectedIndex];
            _localization.LoadLanguage(selectedLanguage);
            _ConfigManager.SetLanguage(selectedLanguage);
            AnsiConsole.MarkupLine($"[green]{_localization.GetString("success.languageChanged", _localization.GetLanguageDisplayName(selectedLanguage))}[/]");
            AnsiConsole.MarkupLine($"[dim]{_localization.GetString("common.pressAnyKey")}[/]");
            Console.ReadKey();
            AnsiConsole.Clear();
        }
    }
}
