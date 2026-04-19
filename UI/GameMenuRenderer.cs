using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using NewProject.Gameplay;

namespace NewProject.UI;

public sealed class GameMenuRenderer : System.IDisposable
{
    private readonly SpriteBatch _spriteBatch;
    private readonly SpriteFont _font;
    private readonly Texture2D _pixel;

    public GameMenuRenderer(GraphicsDevice graphicsDevice, ContentManager content)
    {
        _spriteBatch = new SpriteBatch(graphicsDevice);
        _font = content.Load<SpriteFont>("Fonts/DebugFont");
        _pixel = new Texture2D(graphicsDevice, 1, 1);
        _pixel.SetData([Color.White]);
    }

    public void Draw(GraphicsDevice graphicsDevice, GameMenu menu, MouseState mouse)
    {
        if (!menu.IsOpen)
        {
            return;
        }

        Viewport viewport = graphicsDevice.Viewport;
        Point mousePoint = new(mouse.X, mouse.Y);

        _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
        _spriteBatch.Draw(_pixel, new Rectangle(0, 0, viewport.Width, viewport.Height), new Color(6, 10, 16, 160));

        if (menu.Screen == MenuScreen.Main)
        {
            DrawMainMenu(viewport, mousePoint, menu);
        }
        else
        {
            DrawSettingsMenu(viewport, mousePoint, menu);
        }

        _spriteBatch.End();
    }

    private void DrawMainMenu(Viewport viewport, Point mousePoint, GameMenu menu)
    {
        Rectangle panel = menu.GetMainPanel(viewport);
        DrawPanel(panel, new Color(18, 28, 42, 230));
        _spriteBatch.DrawString(_font, "Mini Block World", new Vector2(panel.X + 88, panel.Y + 24), Color.White);

        string[] labels = ["Play", "Settings", "Quit"];
        for (int i = 0; i < labels.Length; i++)
        {
            Rectangle button = menu.GetMainButton(viewport, i);
            bool hovered = button.Contains(mousePoint);
            DrawButton(button, labels[i], hovered);
        }
    }

    private void DrawSettingsMenu(Viewport viewport, Point mousePoint, GameMenu menu)
    {
        Rectangle panel = menu.GetSettingsPanel(viewport);
        DrawPanel(panel, new Color(18, 28, 42, 235));
        _spriteBatch.DrawString(_font, "Settings", new Vector2(panel.X + 34, panel.Y + 24), Color.White);

        _spriteBatch.DrawString(_font, $"FOV: {menu.FovDegrees:0}", new Vector2(panel.X + 34, panel.Y + 68), new Color(230, 236, 245));
        Rectangle track = menu.GetFovSliderTrack(viewport);
        Rectangle knob = menu.GetFovSliderKnob(viewport);
        _spriteBatch.Draw(_pixel, track, new Color(70, 90, 120));
        _spriteBatch.Draw(_pixel, knob, new Color(255, 226, 128));

        _spriteBatch.DrawString(_font, "Fullscreen", new Vector2(panel.X + 82, panel.Y + 146), new Color(230, 236, 245));
        Rectangle toggle = menu.GetFullscreenToggle(viewport);
        _spriteBatch.Draw(_pixel, toggle, menu.Fullscreen ? new Color(96, 190, 120) : new Color(90, 96, 110));
        if (menu.Fullscreen)
        {
            _spriteBatch.Draw(_pixel, new Rectangle(toggle.X + 7, toggle.Y + 7, toggle.Width - 14, toggle.Height - 14), new Color(230, 255, 235));
        }

        Rectangle back = menu.GetSettingsBackButton(viewport);
        DrawButton(back, "Back", back.Contains(mousePoint));
    }

    private void DrawPanel(Rectangle rect, Color color)
    {
        _spriteBatch.Draw(_pixel, rect, color);
        DrawBorder(rect, new Color(110, 180, 255));
    }

    private void DrawButton(Rectangle rect, string label, bool hovered)
    {
        _spriteBatch.Draw(_pixel, rect, hovered ? new Color(56, 86, 126) : new Color(34, 52, 78));
        DrawBorder(rect, hovered ? new Color(255, 226, 128) : new Color(170, 196, 224));
        Vector2 size = _font.MeasureString(label);
        Vector2 pos = new(rect.Center.X - size.X / 2f, rect.Center.Y - size.Y / 2f);
        _spriteBatch.DrawString(_font, label, pos, Color.White);
    }

    private void DrawBorder(Rectangle rect, Color color)
    {
        _spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, rect.Width, 2), color);
        _spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Bottom - 2, rect.Width, 2), color);
        _spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, 2, rect.Height), color);
        _spriteBatch.Draw(_pixel, new Rectangle(rect.Right - 2, rect.Y, 2, rect.Height), color);
    }

    public void Dispose()
    {
        _pixel.Dispose();
        _spriteBatch.Dispose();
    }
}
