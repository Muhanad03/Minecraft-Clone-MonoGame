namespace NewProject.Rendering;

public sealed class ViewModelSettings
{
    // Base camera-space placement.
    public float BaseRightOffset { get; set; } = 0.42f;
    public float BaseRightSwing { get; set; } = 0.03f;
    public float BaseUpOffset { get; set; } = -0.26f;
    public float BaseUpSettle { get; set; } = 0.015f;
    public float BaseForwardOffset { get; set; } = 0.78f;
    public float BaseForwardSwing { get; set; } = 0.03f;

    // Arm direction from camera.
    public float ArmForwardFactor { get; set; } = 0.90f;
    public float ArmRightFactor { get; set; } = -0.72f;
    public float ArmUpFactor { get; set; } = 0.08f;

    // Arm box dimensions.
    public float ArmWidth { get; set; } = 0.12f;
    public float ArmHeight { get; set; } = 0.16f;
    public float ArmLength { get; set; } = 0.52f;

    // Hand placement.
    public float HandForwardOffset { get; set; } = 0.23f;
    public float HandUpOffset { get; set; } = -0.01f;
    public float HandSideOffset { get; set; } = -0.02f;
    public float HandWidth { get; set; } = 0.10f;
    public float HandHeight { get; set; } = 0.10f;
    public float HandLength { get; set; } = 0.10f;

    // Shared item placement.
    public float ItemSideOffset { get; set; } = -0.05f;
    public float ItemUpOffset { get; set; } = -0.07f;
    public float ItemForwardOffset { get; set; } = 0.1f;

    // Tool placement.
    public float ToolForwardFactor { get; set; } = 0.30f;
    public float ToolRightFactor { get; set; } = 1.88f;
    public float ToolUpFactor { get; set; } = 0.14f;
    public float ToolSideOffset { get; set; } = 0.01f;
    public float ToolUpOffset { get; set; } = 0.01f;
    public float ToolForwardOffset { get; set; } = -0.05f;
    public float ToolWidth { get; set; } = 0.56f;
    public float ToolHeight { get; set; } = 0.76f;
    public float ToolGripRightOffset { get; set; } = 0.00f;
    public float ToolGripUpOffset { get; set; } = 0.32f;

    // Sword slash animation tuning.
    public float SwordSlashSideSwing { get; set; } = 0.12f;
    public float SwordSlashUpSwing { get; set; } = 0.04f;
    public float SwordSlashForwardSwing { get; set; } = 0.08f;
    public float SwordSlashForwardFactorBoost { get; set; } = 0.22f;
    public float SwordSlashRightFactorBoost { get; set; } = -0.16f;
    public float SwordSlashUpFactorBoost { get; set; } = -0.22f;
    public float SwordSlashSideOffset { get; set; } = -0.05f;
    public float SwordSlashUpOffset { get; set; } = -0.08f;
    public float SwordSlashForwardOffset { get; set; } = 0.04f;

    // Block placement.
    public float BlockForwardFactor { get; set; } = 0.38f;
    public float BlockRightFactor { get; set; } = 0.82f;
    public float BlockUpFactor { get; set; } = -0.02f;
    public float BlockSideOffset { get; set; } = -0.01f;
    public float BlockUpOffset { get; set; } = 0.01f;
    public float BlockForwardOffset { get; set; } = 0.01f;
    public float BlockSize { get; set; } = 0.24f;
}
