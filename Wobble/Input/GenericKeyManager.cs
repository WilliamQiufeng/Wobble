using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework.Input;
using Wobble.Helpers;
using Wobble.Logging;

namespace Wobble.Input
{
    public class GenericKeyManager
    {
        public static bool IsDown(GenericKey key)
        {
            if (key.KeyboardKey != null)
                return KeyboardManager.CurrentState.IsKeyDown((Keys)key.KeyboardKey);
            if (key.JoystickKey != null)
                return (int)key.JoystickKey < JoystickManager.CurrentState.Buttons.Length &&
                       JoystickManager.CurrentState.Buttons[(int)key.JoystickKey] == ButtonState.Pressed;
            if (key.MouseButton != null)
                return MouseManager.IsPressed(key.MouseButton.Value);
            if (key.ScrollDirection != null)
                return key.ScrollDirection.Value == MouseScrollDirection.Up
                    ? MouseManager.IsScrollingUp()
                    : MouseManager.IsScrollingDown();

            Logger.Error("Both keyboard and joystick key is null in GenericKey", LogType.Runtime);
            return false;
        }

        public static bool IsUp(GenericKey key) => !IsDown(key);

        public static bool IsUniquePress(GenericKey key)
        {
            if (key.KeyboardKey != null)
                return KeyboardManager.IsUniqueKeyPress((Keys)key.KeyboardKey);
            if (key.JoystickKey != null)
                return JoystickManager.IsUniqueButtonPress((int)key.JoystickKey);
            if (key.MouseButton != null)
                return MouseManager.IsUniquePress(key.MouseButton.Value);
            if (key.ScrollDirection != null)
                return false;

            Logger.Error("Both keyboard and joystick key is null in GenericKey", LogType.Runtime);
            return false;
        }

        public static bool IsUniqueRelease(GenericKey key)
        {
            if (key.KeyboardKey != null)
                return KeyboardManager.IsUniqueKeyRelease((Keys)key.KeyboardKey);
            if (key.JoystickKey != null)
                return JoystickManager.IsUniqueButtonRelease((int)key.JoystickKey);
            if (key.MouseButton != null)
                return MouseManager.IsUniqueClick(key.MouseButton.Value);
            if (key.ScrollDirection != null)
                return false;

            Logger.Error("Both keyboard and joystick key is null in GenericKey", LogType.Runtime);
            return false;
        }

        public static List<GenericKey> GetPressedKeys()
        {
            var keys = new List<GenericKey>();

            foreach (var key in KeyboardManager.CurrentState.GetPressedKeys())
                keys.Add(new GenericKey { KeyboardKey = key });

            for (var key = 0; key < JoystickManager.CurrentState.Buttons.Length; key++)
                if (JoystickManager.CurrentState.Buttons[key] == ButtonState.Pressed)
                    keys.Add(new GenericKey { JoystickKey = key });

            foreach (MouseButton mouseButton in Enum.GetValues(typeof(MouseButton)))
            {
                if (MouseManager.IsPressed(mouseButton))
                    keys.Add(new GenericKey { MouseButton = mouseButton });
            }

            if (MouseManager.IsScrollingUp())
                keys.Add(new GenericKey { ScrollDirection = MouseScrollDirection.Up });

            if (MouseManager.IsScrollingDown())
                keys.Add(new GenericKey { ScrollDirection = MouseScrollDirection.Down });

            return keys;
        }
    }
}