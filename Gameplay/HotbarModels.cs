using NewProject.World;

namespace NewProject.Gameplay;

public enum ToolType
{
    Pickaxe,
    Axe,
    Sword
}

public enum HotbarItemKind
{
    Tool,
    Block
}

public readonly record struct HotbarEntry(HotbarItemKind Kind, ToolType Tool, BlockType Block, string Label);

public static class HotbarDefinitions
{
    public static readonly HotbarEntry[] Entries =
    [
        new(HotbarItemKind.Tool, ToolType.Pickaxe, BlockType.Air, "Pick"),
        new(HotbarItemKind.Tool, ToolType.Axe, BlockType.Air, "Axe"),
        new(HotbarItemKind.Tool, ToolType.Sword, BlockType.Air, "Sword"),
        new(HotbarItemKind.Block, ToolType.Pickaxe, BlockType.Grass, "Grass"),
        new(HotbarItemKind.Block, ToolType.Pickaxe, BlockType.Dirt, "Dirt"),
        new(HotbarItemKind.Block, ToolType.Pickaxe, BlockType.Stone, "Stone"),
        new(HotbarItemKind.Block, ToolType.Pickaxe, BlockType.Trunk, "Log"),
        new(HotbarItemKind.Block, ToolType.Pickaxe, BlockType.Leaves, "Leaf")
    ];
}
