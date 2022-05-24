using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace StaticCamera
{
    public class PromptHandler : MonoBehaviour
    {
        private ScreenPrompt _gamepadCameraPrompt = new ScreenPrompt(InputLibrary.toolOptionRight, "Toggle Static Camera <CMD>");
        private ScreenPrompt _keyboardCameraPrompt = new ScreenPrompt("Toggle Static Camera [B]");

        private bool _usingGamepad;
        private bool _canUse;
        private bool _enabled;

        private void Awake()
        {
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
            if(_enabled && _canUse)
            {
                _gamepadCameraPrompt.SetVisibility(_usingGamepad);
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
