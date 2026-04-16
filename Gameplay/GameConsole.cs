using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using NewProject.Player;
using NewProject.World;

namespace NewProject.Gameplay;

public sealed class GameConsole
{
    private readonly HotbarState _hotbar;
    private readonly PlayerController _player;
    private readonly InfiniteWorld _world;
    private readonly Func<float> _getTime;
    private readonly Action<string> _setTimePreset;
    private readonly List<string> _history = new();
    private readonly List<string> _lines = new();
    private string _input = "/";
    private int _historyIndex = -1;
    private int _scrollOffset;

    public GameConsole(HotbarState hotbar, PlayerController player, InfiniteWorld world, Func<float> getTime, Action<string> setTimePreset)
    {
        _hotbar = hotbar;
        _player = player;
        _world = world;
        _getTime = getTime;
        _setTimePreset = setTimePreset;
        AddLine("Console ready. Type 'help' for commands.");
    }

    public bool IsOpen { get; private set; }

    public string Input => _input;

    public IReadOnlyList<string> Lines => _lines;

    public int ScrollOffset => _scrollOffset;

    public bool HandleToggle(KeyboardState keyboard, KeyboardState previousKeyboard)
    {
        if (!IsNewKeyPress(Keys.OemTilde, keyboard, previousKeyboard) &&
            !IsNewKeyPress(Keys.Oem8, keyboard, previousKeyboard))
        {
            return false;
        }

        IsOpen = !IsOpen;
        _historyIndex = -1;
        _scrollOffset = 0;
        _input = "/";
        return true;
    }

    public void HandleInput(KeyboardState keyboard, KeyboardState previousKeyboard, MouseState mouse, MouseState previousMouse)
    {
        if (!IsOpen)
        {
            return;
        }

        if (IsNewKeyPress(Keys.Escape, keyboard, previousKeyboard))
        {
            IsOpen = false;
            _historyIndex = -1;
            _scrollOffset = 0;
            return;
        }

        if (IsNewKeyPress(Keys.Enter, keyboard, previousKeyboard))
        {
            Execute(_input);
            _input = "/";
            _historyIndex = -1;
            return;
        }

        if (IsNewKeyPress(Keys.Back, keyboard, previousKeyboard) && _input.Length > 1)
        {
            _input = _input[..^1];
        }

        if (IsNewKeyPress(Keys.Up, keyboard, previousKeyboard))
        {
            NavigateHistory(-1);
        }
        else if (IsNewKeyPress(Keys.Down, keyboard, previousKeyboard))
        {
            NavigateHistory(1);
        }

        if (IsNewKeyPress(Keys.PageUp, keyboard, previousKeyboard))
        {
            ScrollLines(3);
        }
        else if (IsNewKeyPress(Keys.PageDown, keyboard, previousKeyboard))
        {
            ScrollLines(-3);
        }

        int wheelDelta = mouse.ScrollWheelValue - previousMouse.ScrollWheelValue;
        if (wheelDelta != 0)
        {
            ScrollLines(wheelDelta > 0 ? 2 : -2);
        }

        bool shift = keyboard.IsKeyDown(Keys.LeftShift) || keyboard.IsKeyDown(Keys.RightShift);

        foreach (Keys key in keyboard.GetPressedKeys())
        {
            if (previousKeyboard.IsKeyDown(key))
            {
                continue;
            }

            if (TryMapKeyToChar(key, shift, out char character))
            {
                _input += character;
            }
        }
    }

    private void Execute(string rawInput)
    {
        string commandLine = rawInput.Trim();
        if (string.IsNullOrWhiteSpace(commandLine))
        {
            return;
        }

        AddLine(commandLine);
        _history.Add(commandLine);

        string normalizedInput = commandLine.StartsWith('/') ? commandLine[1..] : commandLine;
        string[] parts = normalizedInput.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return;
        }

        string command = parts[0].ToLowerInvariant();

        switch (command)
        {
            case "help":
                AddLine("/help, /clear, /pos, /tp <x> <y> <z>, /give <block> [amount], /time <day|night>");
                break;

            case "clear":
                _lines.Clear();
                AddLine("Console cleared.");
                break;

            case "pos":
            {
                Vector3 position = _player.Position;
                AddLine($"Position: {position.X:0.00}, {position.Y:0.00}, {position.Z:0.00}");
                break;
            }

            case "tp":
                HandleTeleport(parts);
                break;

            case "give":
                HandleGive(parts);
                break;

            case "time":
                HandleTime(parts);
                break;

            default:
                AddLine($"Unknown command: {command}");
                break;
        }
    }

    private void HandleTeleport(string[] parts)
    {
        if (parts.Length != 4 ||
            !float.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float x) ||
            !float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float y) ||
            !float.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out float z))
        {
            AddLine("Usage: tp <x> <y> <z>");
            return;
        }

        _world.EnsureChunksAround((int)MathF.Floor(x), (int)MathF.Floor(z), 2);
        _player.Teleport(new Vector3(x, y, z));
        AddLine($"Teleported to {x:0.00}, {y:0.00}, {z:0.00}");
    }

    private void HandleGive(string[] parts)
    {
        if (parts.Length < 2)
        {
            AddLine("Usage: give <block> [amount]");
            return;
        }

        if (!TryParseBlock(parts[1], out BlockType block) || block == BlockType.Air || block == BlockType.Water)
        {
            AddLine($"Unknown or unsupported block: {parts[1]}");
            return;
        }

        int amount = 1;
        if (parts.Length >= 3 && !int.TryParse(parts[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out amount))
        {
            AddLine("Amount must be a number.");
            return;
        }

        amount = Math.Clamp(amount, 1, 999);
        _hotbar.AddBlock(block, amount);
        AddLine($"Added {amount} {block}.");
    }

    private void HandleTime(string[] parts)
    {
        if (parts.Length < 2)
        {
            AddLine($"Current time value: {_getTime():0.00}");
            AddLine("Usage: /time <day|night>");
            return;
        }

        string preset = parts[1].ToLowerInvariant();
        if (preset != "day" && preset != "night")
        {
            AddLine("Usage: /time <day|night>");
            return;
        }

        _setTimePreset(preset);
        AddLine($"Set time to {preset}.");
    }

    private void NavigateHistory(int direction)
    {
        if (_history.Count == 0)
        {
            return;
        }

        if (_historyIndex == -1)
        {
            _historyIndex = _history.Count;
        }

        _historyIndex = Math.Clamp(_historyIndex + direction, 0, _history.Count);
        _input = _historyIndex >= 0 && _historyIndex < _history.Count ? _history[_historyIndex] : "/";
    }

    private void AddLine(string line)
    {
        _lines.Add(line);
        if (_lines.Count > 128)
        {
            _lines.RemoveAt(0);
        }

        _scrollOffset = 0;
    }

    private void ScrollLines(int delta)
    {
        int maxOffset = Math.Max(0, _lines.Count - 1);
        _scrollOffset = Math.Clamp(_scrollOffset + delta, 0, maxOffset);
    }

    private static bool IsNewKeyPress(Keys key, KeyboardState current, KeyboardState previous) =>
        current.IsKeyDown(key) && !previous.IsKeyDown(key);

    private static bool TryParseBlock(string value, out BlockType block)
    {
        string normalized = value.Trim().Replace("-", string.Empty, StringComparison.Ordinal).Replace("_", string.Empty, StringComparison.Ordinal).ToLowerInvariant();

        foreach (BlockType candidate in Enum.GetValues<BlockType>())
        {
            string name = candidate.ToString().Replace("_", string.Empty, StringComparison.Ordinal).ToLowerInvariant();
            if (name == normalized)
            {
                block = candidate;
                return true;
            }
        }

        block = BlockType.Air;
        return false;
    }

    private static bool TryMapKeyToChar(Keys key, bool shift, out char character)
    {
        if (key >= Keys.A && key <= Keys.Z)
        {
            character = (char)((shift ? 'A' : 'a') + (key - Keys.A));
            return true;
        }

        if (key >= Keys.D0 && key <= Keys.D9)
        {
            const string normalDigits = "0123456789";
            const string shiftedDigits = ")!@#$%^&*(";
            int index = key - Keys.D0;
            character = shift ? shiftedDigits[index] : normalDigits[index];
            return true;
        }

        switch (key)
        {
            case Keys.Space:
                character = ' ';
                return true;
            case Keys.OemPeriod:
                character = shift ? '>' : '.';
                return true;
            case Keys.OemComma:
                character = shift ? '<' : ',';
                return true;
            case Keys.OemMinus:
                character = shift ? '_' : '-';
                return true;
            case Keys.OemPlus:
                character = shift ? '+' : '=';
                return true;
            case Keys.OemQuestion:
                character = shift ? '?' : '/';
                return true;
            case Keys.OemSemicolon:
                character = shift ? ':' : ';';
                return true;
            default:
                character = '\0';
                return false;
        }
    }
}
