using Avalonia.Controls;
using Avalonia.Platform.Storage;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace LtdMusicImporter;

public partial class MainWindow : Window
{
    private readonly ObservableCollection<SlotInfo> _allSlots = new();
    private readonly ObservableCollection<SlotInfo> _visibleSlots = new();
    private readonly string[] _modes =
    [
        "Replace existing slot",
        "Add new stream slot (experimental)"
    ];
    private readonly string[] _categories =
    [
        "Music Disks",
        "Background Music",
        "Games",
        "Dreams",
        "SFX"
    ];

    public MainWindow()
    {
        InitializeComponent();
        SlotsListBox.ItemsSource = _visibleSlots;
        SlotComboBox.ItemsSource = _visibleSlots;
        ModeComboBox.ItemsSource = _modes;
        ModeComboBox.SelectedIndex = 0;
        CategoryComboBox.ItemsSource = _categories;
        CategoryComboBox.SelectedIndex = 0;

        RomFsBox.Text = FindDefaultRomFs();
        OutputFolderBox.Text = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "LTD Custom Music Imports");
        ConverterBox.Text = "VGAudioCli -i {input} -o {output}";

        BrowseRomFsButton.Click += async (_, _) => await BrowseRomFsAsync();
        BrowseAudioButton.Click += async (_, _) => await BrowseAudioAsync();
        BrowseOutputButton.Click += async (_, _) => await BrowseOutputAsync();
        ReloadButton.Click += (_, _) => LoadSlots();
        ImportButton.Click += async (_, _) => await ImportAsync();
        FilterBox.TextChanged += (_, _) => ApplyFilter();
        ModeComboBox.SelectionChanged += (_, _) => UpdateModeControls();
        CategoryComboBox.SelectionChanged += (_, _) => ApplyFilter();
        SlotsListBox.SelectionChanged += (_, _) =>
        {
            if (SlotsListBox.SelectedItem is SlotInfo slot)
            {
                SlotComboBox.SelectedItem = slot;
            }
        };

        LoadSlots();
        UpdateModeControls();
    }

    private static string FindWorkspaceRoot()
    {
        var current = AppContext.BaseDirectory;
        while (!string.IsNullOrWhiteSpace(current))
        {
            if (Directory.Exists(Path.Combine(current, "RomFS")))
            {
                return current;
            }

            var parent = Directory.GetParent(current);
            if (parent is null)
            {
                break;
            }

            current = parent.FullName;
        }

        return Directory.GetCurrentDirectory();
    }

    private static string FindDefaultRomFs()
    {
        var candidate = Path.Combine(FindWorkspaceRoot(), "RomFS");
        return Directory.Exists(candidate) ? candidate : string.Empty;
    }

    private async Task BrowseRomFsAsync()
    {
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select extracted RomFS folder",
            AllowMultiple = false
        });

        if (folders.Count == 0)
        {
            return;
        }

        RomFsBox.Text = folders[0].Path.LocalPath;
        LoadSlots();
    }

    private async Task BrowseAudioAsync()
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select custom music",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("Audio files")
                {
                    Patterns = ["*.bwav", "*.wav", "*.flac", "*.ogg", "*.mp3", "*.m4a"]
                },
                FilePickerFileTypes.All
            ]
        });

        if (files.Count > 0)
        {
            SourceAudioBox.Text = files[0].Path.LocalPath;
        }
    }

    private async Task BrowseOutputAsync()
    {
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select output folder",
            AllowMultiple = false
        });

        if (folders.Count > 0)
        {
            OutputFolderBox.Text = folders[0].Path.LocalPath;
        }
    }

    private void LoadSlots()
    {
        _allSlots.Clear();
        var romFs = RomFsBox.Text?.Trim() ?? string.Empty;
        var streamRoot = Path.Combine(romFs, "Sound", "Resource", "Stream");

        if (!Directory.Exists(streamRoot))
        {
            Log($"Stream folder not found: {streamRoot}");
            ApplyFilter();
            return;
        }

        var slots = Directory.EnumerateFiles(streamRoot, "*.bwav", SearchOption.TopDirectoryOnly)
            .Select(Path.GetFileName)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Cast<string>()
            .Select(name => new SlotInfo(name, CategorizeSlot(name)))
            .OrderBy(slot => CategorySort(slot.Category))
            .ThenBy(slot => slot.FileName, StringComparer.OrdinalIgnoreCase);

        foreach (var slot in slots)
        {
            _allSlots.Add(slot);
        }

        ApplyFilter();
        if (_visibleSlots.Count > 0 && SlotComboBox.SelectedItem is null)
        {
            SlotComboBox.SelectedIndex = 0;
        }

        Log($"Loaded {_allSlots.Count} stream slots.");
    }

    private void ApplyFilter()
    {
        var filter = FilterBox.Text?.Trim() ?? string.Empty;
        var category = CategoryComboBox.SelectedItem as string ?? "Music Disks";
        var previous = SlotComboBox.SelectedItem as SlotInfo;

        _visibleSlots.Clear();
        foreach (var slot in _allSlots.Where(slot =>
            slot.Category == category &&
            (string.IsNullOrWhiteSpace(filter) ||
             slot.FileName.Contains(filter, StringComparison.OrdinalIgnoreCase))))
        {
            _visibleSlots.Add(slot);
        }

        if (previous is not null && _visibleSlots.Contains(previous))
        {
            SlotComboBox.SelectedItem = previous;
        }
        else if (_visibleSlots.Count > 0)
        {
            SlotComboBox.SelectedIndex = 0;
        }
    }

    private async Task ImportAsync()
    {
        try
        {
            var romFs = RequiredDirectory(RomFsBox.Text, "RomFS root");
            var source = RequiredFile(SourceAudioBox.Text, "Custom audio");
            var outputRoot = EnsureDirectory(OutputFolderBox.Text, "Output folder");
            var isNewSlot = IsNewSlotMode();
            var category = CategoryComboBox.SelectedItem as string ?? "Music Disks";
            var slot = isNewSlot
                ? CreateNewSlotInfo(category, NewSlotNameBox.Text)
                : SlotComboBox.SelectedItem as SlotInfo;
            if (slot is null)
            {
                throw new InvalidOperationException("Choose a replacement slot.");
            }

            var modName = SanitizeModName(ModNameBox.Text);
            var modRoot = Path.Combine(outputRoot, modName);
            var streamOutput = Path.Combine(modRoot, "romfs", "Sound", "Resource", "Stream");
            Directory.CreateDirectory(streamOutput);

            var targetFile = Path.Combine(streamOutput, slot.FileName);
            var extension = Path.GetExtension(source);
            var converter = ConverterBox.Text?.Trim() ?? string.Empty;

            if (extension.Equals(".bwav", StringComparison.OrdinalIgnoreCase))
            {
                File.Copy(source, targetFile, overwrite: true);
                Log($"Copied BWAV into {slot.Category} slot {slot.FileName}.");
            }
            else
            {
                await ConvertToBwavAsync(converter, source, targetFile);
                if (!File.Exists(targetFile))
                {
                    throw new InvalidOperationException("Converter finished but did not create the expected BWAV output.");
                }
            }

            if (CopySourceCheckBox.IsChecked == true)
            {
                var sourceDir = Path.Combine(modRoot, "source-audio");
                Directory.CreateDirectory(sourceDir);
                File.Copy(source, Path.Combine(sourceDir, Path.GetFileName(source)), overwrite: true);
            }

            WriteManifest(modRoot, romFs, source, slot, targetFile, converter);
            if (isNewSlot)
            {
                WriteNewSlotNotes(modRoot, slot);
                Log($"Added experimental new stream slot {slot.FileName}.");
            }
            Log($"Wrote mod overlay: {modRoot}");
        }
        catch (Exception ex)
        {
            Log($"Import failed: {ex.Message}");
        }
    }

    private static string RequiredDirectory(string? path, string label)
    {
        path = path?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(path) || !Directory.Exists(path))
        {
            throw new InvalidOperationException($"{label} is not a valid folder.");
        }

        return path;
    }

    private static string EnsureDirectory(string? path, string label)
    {
        path = path?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new InvalidOperationException($"{label} is required.");
        }

        Directory.CreateDirectory(path);
        return path;
    }

    private void UpdateModeControls()
    {
        var isNewSlot = IsNewSlotMode();
        SlotComboBox.IsEnabled = !isNewSlot;
        SlotsListBox.IsEnabled = !isNewSlot;
        FilterBox.IsEnabled = !isNewSlot;
        NewSlotPanel.IsVisible = isNewSlot;
    }

    private bool IsNewSlotMode() =>
        (ModeComboBox.SelectedItem as string)?.StartsWith("Add new", StringComparison.OrdinalIgnoreCase) == true;

    private static string RequiredFile(string? path, string label)
    {
        path = path?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            throw new InvalidOperationException($"{label} is not a valid file.");
        }

        return path;
    }

    private static string SanitizeModName(string? name)
    {
        var raw = string.IsNullOrWhiteSpace(name) ? "CustomMusicImport" : name.Trim();
        var invalid = Path.GetInvalidFileNameChars().ToHashSet();
        var safe = new string(raw.Select(ch => invalid.Contains(ch) ? '_' : ch).ToArray());
        return string.IsNullOrWhiteSpace(safe) ? "CustomMusicImport" : safe;
    }

    private static SlotInfo CreateNewSlotInfo(string category, string? displayName)
    {
        var stem = SanitizeIdentifier(displayName);
        if (string.IsNullOrWhiteSpace(stem))
        {
            stem = "CustomSong";
        }

        var fileName = category switch
        {
            "Music Disks" => $"SE_Record_Custom_{stem}.bwav",
            "Background Music" => $"BGM_Custom_{stem}.bwav",
            "Games" => $"SE_VideoGame_Custom_{stem}.bwav",
            "Dreams" => $"BGM_Demo_Dream_Custom_{stem}.bwav",
            "SFX" => $"SE_Custom_{stem}.bwav",
            _ => $"BGM_Custom_{stem}.bwav"
        };

        return new SlotInfo(fileName, category);
    }

    private static string SanitizeIdentifier(string? value)
    {
        value = value?.Trim() ?? string.Empty;
        var builder = new System.Text.StringBuilder();
        var capitalizeNext = true;
        foreach (var ch in value)
        {
            if (char.IsLetterOrDigit(ch))
            {
                builder.Append(capitalizeNext ? char.ToUpperInvariant(ch) : ch);
                capitalizeNext = false;
            }
            else
            {
                capitalizeNext = true;
            }
        }

        return builder.ToString();
    }

    private async Task ConvertToBwavAsync(string converterTemplate, string input, string output)
    {
        if (string.IsNullOrWhiteSpace(converterTemplate))
        {
            throw new InvalidOperationException("WAV imports need a BWAV converter command, such as VGAudioCli -i {input} -o {output}.");
        }

        var extension = Path.GetExtension(input);
        var converterInput = input;
        string? tempDirectory = null;

        try
        {
            if (!extension.Equals(".wav", StringComparison.OrdinalIgnoreCase))
            {
                tempDirectory = Path.Combine(Path.GetTempPath(), "ltd-music-import-" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(tempDirectory);
                converterInput = Path.Combine(tempDirectory, Path.GetFileNameWithoutExtension(input) + ".wav");
                await RunFfmpegToPcmWavAsync(input, converterInput);
            }

            await RunConverterAsync(converterTemplate, converterInput, output);
            Log($"Converted WAV into BWAV slot file {Path.GetFileName(output)}.");
        }
        finally
        {
            if (tempDirectory is not null && Directory.Exists(tempDirectory))
            {
                Directory.Delete(tempDirectory, recursive: true);
            }
        }
    }

    private async Task RunFfmpegToPcmWavAsync(string input, string output)
    {
        if (!CommandExists("ffmpeg"))
        {
            throw new InvalidOperationException("ffmpeg was not found. Install ffmpeg or provide a WAV file.");
        }

        var command = $"ffmpeg -y -i {Quote(input)} -acodec pcm_s16le {Quote(output)}";
        Log($"Normalizing source audio with ffmpeg: {command}");
        await RunShellCommandAsync(command);
    }

    private async Task RunConverterAsync(string template, string input, string output)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(output)!);
        var command = template
            .Replace("{input}", Quote(input), StringComparison.Ordinal)
            .Replace("{output}", Quote(output), StringComparison.Ordinal);

        Log($"Running converter: {command}");
        await RunShellCommandAsync(command);
    }

    private async Task RunShellCommandAsync(string command)
    {
        var shell = OperatingSystem.IsWindows() ? "cmd.exe" : "/bin/sh";
        var args = OperatingSystem.IsWindows() ? $"/C {command}" : $"-lc {Quote(command)}";
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo(shell, args)
            {
                RedirectStandardError = true,
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };

        process.Start();
        var stdout = await process.StandardOutput.ReadToEndAsync();
        var stderr = await process.StandardError.ReadToEndAsync();
        await process.WaitForExitAsync();

        if (!string.IsNullOrWhiteSpace(stdout))
        {
            Log(stdout.Trim());
        }

        if (!string.IsNullOrWhiteSpace(stderr))
        {
            Log(stderr.Trim());
        }

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"Converter exited with code {process.ExitCode}.");
        }
    }

    private static bool CommandExists(string command)
    {
        var probe = OperatingSystem.IsWindows()
            ? $"where {command}"
            : $"command -v {command}";
        try
        {
            using var process = new Process
            {
                StartInfo = new ProcessStartInfo(
                    OperatingSystem.IsWindows() ? "cmd.exe" : "/bin/sh",
                    OperatingSystem.IsWindows() ? $"/C {probe}" : $"-lc {Quote(probe)}")
                {
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            process.Start();
            process.WaitForExit(2000);
            return process.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private static string CategorizeSlot(string fileName)
    {
        if (fileName.StartsWith("SE_Record_", StringComparison.OrdinalIgnoreCase))
        {
            return "Music Disks";
        }

        if (fileName.Contains("VideoGame", StringComparison.OrdinalIgnoreCase) ||
            fileName.Contains("Minigame", StringComparison.OrdinalIgnoreCase) ||
            fileName.Contains("Game_", StringComparison.OrdinalIgnoreCase))
        {
            return "Games";
        }

        if (fileName.Contains("Dream", StringComparison.OrdinalIgnoreCase) ||
            fileName.Contains("InsideHead", StringComparison.OrdinalIgnoreCase))
        {
            return "Dreams";
        }

        if (fileName.StartsWith("BGM_", StringComparison.OrdinalIgnoreCase))
        {
            return "Background Music";
        }

        return "SFX";
    }

    private static int CategorySort(string category) => category switch
    {
        "Music Disks" => 0,
        "Background Music" => 1,
        "Games" => 2,
        "Dreams" => 3,
        "SFX" => 4,
        _ => 5
    };

    private static void WriteManifest(string modRoot, string romFs, string source, SlotInfo slot, string targetFile, string converter)
    {
        var manifest = new MusicImportManifest(
            ModName: Path.GetFileName(modRoot),
            CreatedAtUtc: DateTimeOffset.UtcNow,
            RomFsRoot: romFs,
            SourceAudio: source,
            ReplacementCategory: slot.Category,
            ReplacementSlot: slot.FileName,
            OverlayPath: Path.GetRelativePath(modRoot, targetFile),
            ConverterCommandTemplate: string.IsNullOrWhiteSpace(converter) ? null : converter);

        var json = JsonSerializer.Serialize(manifest, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(Path.Combine(modRoot, "music-import.json"), json + Environment.NewLine);
        File.WriteAllText(
            Path.Combine(modRoot, "README.md"),
            $"# {manifest.ModName}{Environment.NewLine}{Environment.NewLine}" +
            $"Replaces `{slot.FileName}` in `{slot.Category}` with a custom music file.{Environment.NewLine}{Environment.NewLine}" +
            $"Copy this folder's `romfs` directory into your emulator or CFW mod directory for the title.{Environment.NewLine}");
    }

    private static void WriteNewSlotNotes(string modRoot, SlotInfo slot)
    {
        File.WriteAllText(
            Path.Combine(modRoot, "NEW_SLOT_EXPERIMENTAL.md"),
            "# Experimental New Stream Slot" + Environment.NewLine + Environment.NewLine +
            $"This overlay adds a new stream file: `{slot.FileName}`." + Environment.NewLine + Environment.NewLine +
            "The raw stream file is present in `romfs/Sound/Resource/Stream`, but the stock game may not show it in music selection menus until the relevant goods/BGM/UI tables are patched to reference it." + Environment.NewLine + Environment.NewLine +
            "Use replacement mode for fully verified in-game playback today. Use this mode when another mod, table patch, or executable hook can reference the new stream name." + Environment.NewLine);
    }

    private static string Quote(string value)
    {
        if (OperatingSystem.IsWindows())
        {
            return $"\"{value.Replace("\"", "\\\"", StringComparison.Ordinal)}\"";
        }

        return "'" + value.Replace("'", "'\"'\"'", StringComparison.Ordinal) + "'";
    }

    private void Log(string message)
    {
        var line = $"[{DateTime.Now:HH:mm:ss}] {message}";
        LogBox.Text = string.IsNullOrWhiteSpace(LogBox.Text)
            ? line
            : LogBox.Text + Environment.NewLine + line;
        LogBox.CaretIndex = LogBox.Text.Length;
    }

    private sealed record MusicImportManifest(
        string ModName,
        DateTimeOffset CreatedAtUtc,
        string RomFsRoot,
        string SourceAudio,
        string ReplacementCategory,
        string ReplacementSlot,
        string OverlayPath,
        string? ConverterCommandTemplate);

    private sealed record SlotInfo(string FileName, string Category)
    {
        public override string ToString() => $"{Category} / {FileName}";
    }
}
