using UnityEngine;

namespace StaticCamera;

public class PromptHandler : MonoBehaviour
{
	private static ScreenPrompt _gamepadCameraPrompt;
	private static ScreenPrompt _keyboardCameraPrompt;
	private static Texture2D _bKey;
    private static bool _initialized;

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
		UpdatePromptVisibility();
	}

	private void OnGamePaused()
	{
		_enabled = false;
	}

	private void OnGameUnpaused()
	{
		_enabled = true;
	}

	private void OnWakeUp()
	{
		_enabled = true;
	}

	private void UpdatePromptVisibility()
	{
		if (_enabled && StaticCamera.Instance.CanUse() && PlayerData.GetPromptsEnabled())
		{
			_gamepadCameraPrompt.SetVisibility(OWInput.UsingGamepad());
			_keyboardCameraPrompt.SetVisibility(!OWInput.UsingGamepad());
		}
		else
		{
			_gamepadCameraPrompt.SetVisibility(false);
			_keyboardCameraPrompt.SetVisibility(false);
		}
	}
}
