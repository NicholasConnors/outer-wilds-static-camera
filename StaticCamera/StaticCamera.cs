using OWML.ModHelper;
using OWML.Common;
using OWML.Utils;
using UnityEngine;
using UnityEngine.PostProcessing;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using System;
using System.Linq;
using System.Collections.Generic;

namespace StaticCamera
{
    public class StaticCamera : ModBehaviour
    {
        public static OWCamera OWCamera { get; private set; }
        private Camera _camera;
        private GameObject _cameraObject;

        private bool _initNextTick = false;
        private bool _loaded = false;
        private bool _cameraOn = false;
        private OWCamera _previousCamera;

        public static StaticCamera Instance { get; private set; }

        private void Start()
        {
            ModHelper.Console.WriteLine($"{nameof(StaticCamera)} is loaded!", MessageType.Success);

            Instance = this;

            GlobalMessenger<OWCamera>.AddListener("SwitchActiveCamera", new Callback<OWCamera>(OnSwitchActiveCamera));
            GlobalMessenger<ProbeLauncher>.AddListener("ProbeLauncherEquipped", OnProbeLauncherEquipped);
            GlobalMessenger<ProbeLauncher>.AddListener("ProbeLauncherUnequipped", OnProbeLauncherUnequipped);
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDestroy()
        {
            GlobalMessenger<OWCamera>.RemoveListener("SwitchActiveCamera", new Callback<OWCamera>(OnSwitchActiveCamera));
            GlobalMessenger<ProbeLauncher>.RemoveListener("ProbeLauncherEquipped", OnProbeLauncherEquipped);
            GlobalMessenger<ProbeLauncher>.RemoveListener("ProbeLauncherUnequipped", OnProbeLauncherUnequipped);
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name != "SolarSystem")
            {
                _loaded = false;
                return;
            }

            try
            {
                PreInit();
                _initNextTick = true;
            }
            catch(Exception e)
            {
                ModHelper.Console.WriteLine($"Failed static camera pre-initialization. {e.Message}. {e.StackTrace}.", MessageType.Fatal);
            }
        }

        private void PreInit()
        {
            _cameraObject = new GameObject();
            _cameraObject.SetActive(false);

            _camera = _cameraObject.AddComponent<Camera>();
            _camera.enabled = false;

            OWCamera = _cameraObject.AddComponent<OWCamera>();
            OWCamera.renderSkybox = true;
        }

        private void Init()
        {
            FlashbackScreenGrabImageEffect temp = _cameraObject.AddComponent<FlashbackScreenGrabImageEffect>();
            temp._downsampleShader = Locator.GetPlayerCamera().gameObject.GetComponent<FlashbackScreenGrabImageEffect>()._downsampleShader;

            PlanetaryFogImageEffect _image = _cameraObject.AddComponent<PlanetaryFogImageEffect>();
            _image.fogShader = Locator.GetPlayerCamera().gameObject.GetComponent<PlanetaryFogImageEffect>().fogShader;

            PostProcessingBehaviour _postProcessiong = _cameraObject.AddComponent<PostProcessingBehaviour>();
            _postProcessiong.profile = Locator.GetPlayerCamera().gameObject.GetAddComponent<PostProcessingBehaviour>().profile;

            _cameraObject.SetActive(true);
            _camera.CopyFrom(Locator.GetPlayerCamera().mainCamera);
            _camera.cullingMask = _camera.cullingMask & ~(1 << 27) | (1 << 22);

            _cameraObject.name = "StaticCamera";

            try
            {
                ModHelper.Interaction.GetMod("xen.DayDream").GetValue<List<OWCamera>>("Cameras").Add(OWCamera);
            }
            catch (Exception) { }
        }

        private void PlaceCameraAtPostion()
        {
            OWRigidbody relativeBody = null;

            if (Locator.GetPlayerController().IsGrounded()) relativeBody = Locator.GetPlayerController()._groundBody;
            if (relativeBody == null && (Time.time - Locator.GetPlayerController()._lastGroundedTime < 2f)) relativeBody = Locator.GetPlayerController()._lastGroundBody;
            if(relativeBody == null) relativeBody = Locator.GetReferenceFrame(true)?.GetOWRigidBody();
            if (relativeBody == null) relativeBody = Locator.GetReferenceFrame(false)?.GetOWRigidBody();

            if(relativeBody == null)
            {
                WriteError("Couldn't find something to position the camera relative to.");
                Locator.GetPlayerAudioController().PlayNegativeUISound();
                return;
            }

            Write($"Putting camera relative to {relativeBody?.name}");

            _cameraObject.transform.parent = relativeBody.transform;
            _cameraObject.transform.position = Locator.GetActiveCamera().transform.position;
            _cameraObject.transform.rotation = Locator.GetActiveCamera().transform.rotation;
            _previousCamera = Locator.GetActiveCamera();
            _previousCamera.mainCamera.enabled = false;
            _camera.enabled = true;
            GlobalMessenger<OWCamera>.FireEvent("SwitchActiveCamera", OWCamera);
            _cameraOn = true;
        }

        private void OnSwitchActiveCamera(OWCamera camera)
        {
            if (camera.Equals(OWCamera))
            {
                if(!_cameraOn)
                {
                    ShowReticule(false);
                    ShowLauncher(Locator.GetToolModeSwapper().GetToolMode() == ToolMode.Probe);
                }
            }
            else if(_cameraOn)
            {
                _cameraOn = false;
                ShowReticule(true);
                ShowLauncher(true);
            }
        }

        private void OnProbeLauncherEquipped(ProbeLauncher probeLauncher)
        {
            ShowLauncher(true);
        }

        private void OnProbeLauncherUnequipped(ProbeLauncher probeLauncher)
        {
            ShowLauncher(!_cameraOn);
        }

        private void ShowReticule(bool visible)
        {
            Write($"{(visible ? "Showing" : "Hiding")} the reticule.");
            GameObject reticule = GameObject.Find("Reticule");
            if (reticule == null)
            {
                WriteWarning("Couldn't find reticule");
                return;
            }

            if (visible)
            {
                reticule.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            }
            else
            {
                reticule.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceCamera;
                reticule.GetComponent<Canvas>().worldCamera = Locator.GetPlayerCamera().mainCamera;
            }
        }

        private void ShowLauncher(bool visible)
        {
            Write($"{(visible ? "Showing" : "Hiding")} the probe launcher");
            GameObject launcher = Locator.GetPlayerBody().GetComponentInChildren<ProbeLauncher>()?.gameObject;
            if(launcher == null)
            {
                WriteWarning("Couldn't find probe launcher");
                return;
            }
            launcher.transform.localScale = visible ? Vector3.one : Vector3.zero;
        }

        private void Update()
        {
            if(_initNextTick)
            {
                _initNextTick = false;
                try
                {
                    Init();
                }
                catch(Exception e)
                {
                    ModHelper.Console.WriteLine($"Failed static camera initialization. {e.Message}. {e.StackTrace}.", MessageType.Fatal);
                }
                _loaded = true;
            }

            if (!_loaded) return;

            bool toggleCamera = false;
            if (Keyboard.current != null)
            {
                toggleCamera |= Keyboard.current[Key.B].wasReleasedThisFrame;
            }

            if (toggleCamera)
            {
                if(_cameraOn)
                {
                    _previousCamera.mainCamera.enabled = true;
                    _camera.enabled = false;
                    GlobalMessenger<OWCamera>.FireEvent("SwitchActiveCamera", _previousCamera);
                    _cameraOn = false;
                }
                else
                {
                    PlaceCameraAtPostion();
                }
            }
        }

        public bool IsCameraOn()
        {
            return _cameraOn;
        }

        public void Write(string msg)
        {
            ModHelper.Console.WriteLine(msg, MessageType.Info);
        }

        public void WriteError(string msg)
        {
            ModHelper.Console.WriteLine(msg, MessageType.Error);
        }

        public void WriteWarning(string msg)
        {
            ModHelper.Console.WriteLine(msg, MessageType.Warning);
        }
    }
}
