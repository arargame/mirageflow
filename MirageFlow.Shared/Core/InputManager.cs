using Microsoft.Xna.Framework.Input;

namespace MirageFlow.Shared.Core
{
    public static class InputManager
    {
        public static MouseState CurrentMouseState { get; private set; }
        public static MouseState PreviousMouseState { get; private set; }

        public static void Update()
        {
            PreviousMouseState = CurrentMouseState;
            CurrentMouseState = Mouse.GetState();
        }

        public static bool IsMouseClicked()
        {
            return CurrentMouseState.LeftButton == ButtonState.Pressed && 
                   PreviousMouseState.LeftButton == ButtonState.Released;
        }
    }
}
