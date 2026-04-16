using System.Collections.Generic;
using Microsoft.Xna.Framework.Input;
using NewProject.World;

namespace NewProject.Gameplay;

public sealed class HotbarState
{
    private readonly Dictionary<BlockType, int> _inventory = new();

    public HotbarState(int initialBlockCount = 48)
    {
        foreach (HotbarEntry entry in HotbarDefinitions.Entries)
        {
            if (entry.Kind == HotbarItemKind.Block)
            {
                _inventory[entry.Block] = initialBlockCount;
            }
        }
    }

    public int SelectedSlot { get; private set; }

    public IReadOnlyList<HotbarEntry> Entries => HotbarDefinitions.Entries;

    public HotbarEntry SelectedEntry => HotbarDefinitions.Entries[SelectedSlot];

    public void HandleInput(KeyboardState keyboard, KeyboardState previousKeyboard, MouseState mouse, MouseState previousMouse)
    {
        Keys[] numberKeys = [Keys.D1, Keys.D2, Keys.D3, Keys.D4, Keys.D5, Keys.D6, Keys.D7, Keys.D8, Keys.D9];
        for (int i = 0; i < numberKeys.Length && i < HotbarDefinitions.Entries.Length; i++)
        {
            if (keyboard.IsKeyDown(numberKeys[i]) && !previousKeyboard.IsKeyDown(numberKeys[i]))
            {
                SelectedSlot = i;
            }
        }

        int wheelDelta = mouse.ScrollWheelValue - previousMouse.ScrollWheelValue;
        if (wheelDelta == 0)
        {
            return;
        }

        int direction = wheelDelta > 0 ? -1 : 1;
        SelectedSlot = (SelectedSlot + direction + HotbarDefinitions.Entries.Length) % HotbarDefinitions.Entries.Length;
    }

    public int GetBlockCount(BlockType block) => _inventory.TryGetValue(block, out int amount) ? amount : 0;

    public bool HasBlock(BlockType block) => GetBlockCount(block) > 0;

    public void AddBlock(BlockType block)
    {
        AddBlock(block, 1);
    }

    public void AddBlock(BlockType block, int amount)
    {
        if (_inventory.ContainsKey(block))
        {
            _inventory[block] += amount;
        }
    }

    public bool TryConsumeBlock(BlockType block)
    {
        if (!_inventory.TryGetValue(block, out int amount) || amount <= 0)
        {
            return false;
        }

        _inventory[block] = amount - 1;
        return true;
    }
}
