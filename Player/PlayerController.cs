using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using NewProject.World;

namespace NewProject.Player;

public sealed class PlayerController
{
    private const float MouseSensitivity = 0.0032f;
    private const float MoveSpeed = 6.8f;
    private const float SprintMultiplier = 1.55f;
    private const float Gravity = 28f;
    private const float JumpVelocity = 10.5f;
    private const float HalfWidth = 0.33f;
    private const float Height = 1.8f;
    private const float EyeOffset = 1.62f;
    private const float StepHeight = 1.05f;

    private Vector3 _position;
    private float _verticalVelocity;
    private float _yaw = -MathHelper.PiOver2;
    private float _pitch = -0.22f;
    private bool _isGrounded;
    private bool _mouseLookReady;

    public PlayerController(Vector3 spawnPosition)
    {
        _position = spawnPosition;
    }

    public Vector3 CameraPosition => _position + new Vector3(0f, EyeOffset, 0f);

    public Matrix ViewMatrix => Matrix.CreateLookAt(CameraPosition, CameraPosition + GetLookDirection(), Vector3.Up);

    public Matrix GetProjectionMatrix(float aspectRatio)
    {
        return Matrix.CreatePerspectiveFieldOfView(MathHelper.PiOver4, aspectRatio, 0.1f, 260f);
    }

    public void Update(GameTime gameTime, GameWindow window, bool isActive, IBlockWorld world)
    {
        if (!isActive)
        {
            _mouseLookReady = false;
            return;
        }

        UpdateMouseLook(window);

        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
        KeyboardState keyboard = Keyboard.GetState();

        Vector3 horizontalMove = GetMovementInput(keyboard) * MoveSpeed * (keyboard.IsKeyDown(Keys.LeftShift) ? SprintMultiplier : 1f) * dt;

        if (_isGrounded && keyboard.IsKeyDown(Keys.Space))
        {
            _verticalVelocity = JumpVelocity;
            _isGrounded = false;
        }

        _verticalVelocity -= Gravity * dt;
        _verticalVelocity = MathHelper.Max(_verticalVelocity, -36f);

        MoveWithCollision(new Vector3(horizontalMove.X, _verticalVelocity * dt, horizontalMove.Z), world);

        if (IsColliding(_position + new Vector3(0f, -0.08f, 0f), world))
        {
            _isGrounded = true;
            if (_verticalVelocity < 0f)
            {
                _verticalVelocity = 0f;
            }
        }
    }

    private void UpdateMouseLook(GameWindow window)
    {
        Point center = new(window.ClientBounds.Width / 2, window.ClientBounds.Height / 2);

        if (!_mouseLookReady)
        {
            Mouse.SetPosition(center.X, center.Y);
            _mouseLookReady = true;
            return;
        }

        MouseState mouse = Mouse.GetState();
        int deltaX = mouse.X - center.X;
        int deltaY = mouse.Y - center.Y;

        _yaw -= deltaX * MouseSensitivity;
        _pitch = MathHelper.Clamp(_pitch - deltaY * MouseSensitivity, -1.5f, 1.5f);

        Mouse.SetPosition(center.X, center.Y);
    }

    private Vector3 GetMovementInput(KeyboardState keyboard)
    {
        Vector3 look = GetLookDirection();
        Vector3 forward = new Vector3(look.X, 0f, look.Z);
        if (forward != Vector3.Zero)
        {
            forward.Normalize();
        }
        else
        {
            forward = Vector3.Forward;
        }

        Vector3 right = Vector3.Normalize(Vector3.Cross(Vector3.Up, forward));

        Vector3 movement = Vector3.Zero;
        if (keyboard.IsKeyDown(Keys.W))
        {
            movement += forward;
        }

        if (keyboard.IsKeyDown(Keys.S))
        {
            movement -= forward;
        }

        if (keyboard.IsKeyDown(Keys.A))
        {
            movement += right;
        }

        if (keyboard.IsKeyDown(Keys.D))
        {
            movement -= right;
        }

        if (movement != Vector3.Zero)
        {
            movement.Normalize();
        }

        return movement;
    }

    private Vector3 GetLookDirection()
    {
        Matrix rotation = Matrix.CreateFromYawPitchRoll(_yaw, _pitch, 0f);
        return Vector3.Transform(Vector3.Forward, rotation);
    }

    private void MoveWithCollision(Vector3 motion, IBlockWorld world)
    {
        int steps = Math.Max(1, (int)MathF.Ceiling(motion.Length() / 0.2f));
        Vector3 step = motion / steps;

        for (int i = 0; i < steps; i++)
        {
            MoveHorizontalAxis(new Vector3(step.X, 0f, 0f), world);
            MoveHorizontalAxis(new Vector3(0f, 0f, step.Z), world);
            MoveVerticalAxis(step.Y, world);
        }
    }

    private void MoveHorizontalAxis(Vector3 delta, IBlockWorld world)
    {
        if (delta == Vector3.Zero)
        {
            return;
        }

        Vector3 attempted = _position + delta;
        if (!IsColliding(attempted, world))
        {
            _position = attempted;
            return;
        }

        if (_isGrounded)
        {
            Vector3 stepped = _position + delta + new Vector3(0f, StepHeight, 0f);
            if (!IsColliding(stepped, world))
            {
                _position = stepped;
                return;
            }
        }
    }

    private void MoveVerticalAxis(float deltaY, IBlockWorld world)
    {
        if (MathF.Abs(deltaY) < 0.0001f)
        {
            return;
        }

        Vector3 attempted = _position + new Vector3(0f, deltaY, 0f);
        if (!IsColliding(attempted, world))
        {
            _position = attempted;
            _isGrounded = false;
            return;
        }

        _verticalVelocity = 0f;
        if (deltaY < 0f)
        {
            _isGrounded = true;
        }
    }

    private bool IsColliding(Vector3 position, IBlockWorld world)
    {
        int minX = (int)MathF.Floor(position.X - HalfWidth);
        int maxX = (int)MathF.Floor(position.X + HalfWidth);
        int minY = (int)MathF.Floor(position.Y);
        int maxY = (int)MathF.Floor(position.Y + Height - 0.001f);
        int minZ = (int)MathF.Floor(position.Z - HalfWidth);
        int maxZ = (int)MathF.Floor(position.Z + HalfWidth);

        if (minY < 0 || maxY >= world.Height)
        {
            return true;
        }

        for (int x = minX; x <= maxX; x++)
        {
            for (int y = minY; y <= maxY; y++)
            {
                for (int z = minZ; z <= maxZ; z++)
                {
                    if (world.IsSolid(x, y, z))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }
}
