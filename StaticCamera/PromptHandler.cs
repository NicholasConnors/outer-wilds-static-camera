using UnityEngine;

namespace StaticCamera
{
    public class PromptHandler : MonoBehaviour
    {
        private static ScreenPrompt _gamepadCameraPrompt;
        private static ScreenPrompt _keyboardCameraPrompt;
        private static Texture2D _bKey;
        private static bool _initialized;

        private bool _usingGamepad;
        private bool _canUse;
        private bool _enabled;

        private void Awake()
        {
            if (!_initialized)
            {
                _bKey = StaticCamera.Instance.ModHelper.Assets.GetTexture("assets/B_Key_Dark.png");

                var bSprite = Sprite.Create(_bKey, new Rect(0, 0, _bKey.width, _bKey.height), new Vector2(_bKey.width, _bKey.height) / 2f);
                _gamepadCameraPrompt = new ScreenPrompt(InputLibrary.toolOptionRight, "Toggle Static Camera <CMD>");
                _keyboardCameraPrompt = new ScreenPrompt("Toggle Static Camera <CMD>", bSprite);

                _initialized = true;
            }

            Locator.GetPromptManager().AddScreenPrompt(_gamepadCameraPrompt, PromptPosition.UpperRight, false);
            Locator.GetPromptManager().AddScreenPrompt(_keyboardCameraPrompt, PromptPosition.UpperRight, false);

            GlobalMessenger.AddListener("GamePaused", OnGamePaused);
            GlobalMessenger.AddListener("GameUnpaused", OnGameUnpaused);
            GlobalMessenger.AddListener("WakeUp", OnWakeUp);

            var toolMode = Locator.GetToolModeSwapper().GetToolMode();

            _canUse = StaticCamera.Instance.CanUse();
            _usingGamepad = OWInput.UsingGamepad();

            UpdatePromptVisibility();
        }

        private void OnDestroy()
        {
            Locator.GetPromptManager().RemoveScreenPrompt(_gamepadCameraPrompt, PromptPosition.UpperRight);
            Locator.GetPromptManager().RemoveScreenPrompt(_keyboardCameraPrompt, PromptPosition.UpperRight);

            GlobalMessenger.RemoveListener("GamePaused", OnGamePaused);
            GlobalMessenger.RemoveListener("GameUnpaused", OnGameUnpaused);
            GlobalMessenger.RemoveListener("WakeUp", OnWakeUp);
        }

        private void Update()
        {
            if (OWInput.UsingGamepad() != _usingGamepad)
            {
                _usingGamepad = !_usingGamepad;
                UpdatePromptVisibility();
            }

            if (_canUse != StaticCamera.Instance.CanUse())
            {
                _canUse = !_canUse;
                UpdatePromptVisibility();
            }
        }

        private void OnGamePaused()
        {
            _enabled = false;
            UpdatePromptVisibility();
        }

        private void OnGameUnpaused()
        {
            _enabled = true;
            UpdatePromptVisibility();
        }

        private void OnWakeUp()
        {
            _enabled = true;
            UpdatePromptVisibility();
        }

        private void UpdatePromptVisibility()
        {
            if (_enabled)
            {
                // CanUse is only for game pad
                _gamepadCameraPrompt.SetVisibility(_usingGamepad && _canUse);
                _keyboardCameraPrompt.SetVisibility(!_usingGamepad);
            }
            else
            {
                _gamepadCameraPrompt.SetVisibility(false);
                _keyboardCameraPrompt.SetVisibility(false);
            }
        }
    }
}
