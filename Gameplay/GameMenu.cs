using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace NewProject.Gameplay;

public enum MenuScreen
{
    Main,
    Settings
}

public enum MenuAction
{
    None,
    Play,
    Quit
}

public sealed class GameMenu
{
    private const float MinFov = 55f;
    private const float MaxFov = 100f;

    private bool _draggingFov;

    public bool IsOpen { get; private set; } = true;

    public MenuScreen Screen { get; private set; } = MenuScreen.Main;

    public float FovDegrees { get; private set; }

    public bool Fullscreen { get; private set; }

    public GameMenu(float initialFovDegrees, bool fullscreen)
    {
        FovDegrees = initialFovDegrees;
        Fullscreen = fullscreen;
    }

    public void OpenMain()
    {
        IsOpen = true;
        Screen = MenuScreen.Main;
        _draggingFov = false;
    }

    public void Close()
    {
        IsOpen = false;
        _draggingFov = false;
    }

    public MenuAction HandleInput(Viewport viewport, MouseState mouse, MouseState previousMouse)
    {
        Point mousePoint = new(mouse.X, mouse.Y);
        bool clicked = mouse.LeftButton == ButtonState.Pressed && previousMouse.LeftButton == ButtonState.Released;
        bool released = mouse.LeftButton == ButtonState.Released && previousMouse.LeftButton == ButtonState.Pressed;

        if (Screen == MenuScreen.Main)
        {
            Rectangle playButton = GetMainButton(viewport, 0);
            Rectangle settingsButton = GetMainButton(viewport, 1);
            Rectangle quitButton = GetMainButton(viewport, 2);

            if (clicked && playButton.Contains(mousePoint))
            {
                Close();
                return MenuAction.Play;
            }

            if (clicked && settingsButton.Contains(mousePoint))
            {
                Screen = MenuScreen.Settings;
            }

            if (clicked && quitButton.Contains(mousePoint))
            {
                return MenuAction.Quit;
            }

            return MenuAction.None;
        }

        Rectangle backButton = GetSettingsBackButton(viewport);
        Rectangle fullscreenToggle = GetFullscreenToggle(viewport);
        Rectangle sliderTrack = GetFovSliderTrack(viewport);
        Rectangle sliderKnob = GetFovSliderKnob(viewport);

        if (clicked && backButton.Contains(mousePoint))
        {
            Screen = MenuScreen.Main;
            _draggingFov = false;
            return MenuAction.None;
        }

        if (clicked && fullscreenToggle.Contains(mousePoint))
        {
            Fullscreen = !Fullscreen;
        }

        if (clicked && (sliderTrack.Contains(mousePoint) || sliderKnob.Contains(mousePoint)))
        {
            _draggingFov = true;
            UpdateFovFromMouse(sliderTrack, mouse.X);
        }

        if (_draggingFov && mouse.LeftButton == ButtonState.Pressed)
        {
            UpdateFovFromMouse(sliderTrack, mouse.X);
        }

        if (released)
        {
            _draggingFov = false;
        }

        return MenuAction.None;
    }

    public Rectangle GetMainPanel(Viewport viewport) => new(viewport.Width / 2 - 220, viewport.Height / 2 - 170, 440, 300);

    public Rectangle GetMainButton(Viewport viewport, int index)
    {
        Rectangle panel = GetMainPanel(viewport);
        return new Rectangle(panel.X + 60, panel.Y + 84 + index * 62, panel.Width - 120, 46);
    }

    public Rectangle GetSettingsPanel(Viewport viewport) => new(viewport.Width / 2 - 260, viewport.Height / 2 - 190, 520, 340);

    public Rectangle GetSettingsBackButton(Viewport viewport)
    {
        Rectangle panel = GetSettingsPanel(viewport);
        return new Rectangle(panel.X + 34, panel.Bottom - 66, 120, 38);
    }

    public Rectangle GetFullscreenToggle(Viewport viewport)
    {
        Rectangle panel = GetSettingsPanel(viewport);
        return new Rectangle(panel.X + 34, panel.Y + 142, 34, 34);
    }

    public Rectangle GetFovSliderTrack(Viewport viewport)
    {
        Rectangle panel = GetSettingsPanel(viewport);
        return new Rectangle(panel.X + 34, panel.Y + 96, panel.Width - 68, 10);
    }

    public Rectangle GetFovSliderKnob(Viewport viewport)
    {
        Rectangle track = GetFovSliderTrack(viewport);
        float t = (FovDegrees - MinFov) / (MaxFov - MinFov);
        int knobX = track.X + (int)(t * track.Width);
        return new Rectangle(knobX - 8, track.Y - 7, 16, 24);
    }

    private void UpdateFovFromMouse(Rectangle track, int mouseX)
    {
        float t = MathHelper.Clamp((mouseX - track.X) / (float)track.Width, 0f, 1f);
        FovDegrees = MathHelper.Lerp(MinFov, MaxFov, t);
    }
}
