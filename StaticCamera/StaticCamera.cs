using OWML.ModHelper;
using OWML.Common;
using OWML.Utils;
using UnityEngine;
using UnityEngine.PostProcessing;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using System;
using System.Linq;

namespace StaticCamera
{
    public class StaticCamera : ModBehaviour
    {
        public static OWCamera OWCamera { get; private set; }
        private Camera _camera;
        private GameObject _cameraObject;

        private bool _loaded = false;
        private bool _cameraOn = false;
        private OWCamera _previousCamera;

        private void Start()
        {
            ModHelper.Console.WriteLine($"My mod {nameof(StaticCamera)} is loaded!", MessageType.Success);

            GlobalMessenger<OWCamera>.AddListener("SwitchActiveCamera", new Callback<OWCamera>(OnSwitchActiveCamera));

            ModHelper.Events.Subscribe<Flashlight>(Events.AfterStart);
            SceneManager.sceneLoaded += OnSceneLoaded;
            ModHelper.Events.Event += OnEvent;
        }

        private void OnDestroy()
        {
            GlobalMessenger<OWCamera>.RemoveListener("SwitchActiveCamera", new Callback<OWCamera>(OnSwitchActiveCamera));
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
            }
            catch(Exception e)
            {
                ModHelper.Console.WriteLine($"Failed pre-initialization. {e.Message}. {e.StackTrace}.", MessageType.Fatal);
            }
        }

        private void OnEvent(MonoBehaviour behaviour, Events ev)
        {
            if (!(behaviour.GetType() == typeof(Flashlight) && ev == Events.AfterStart)) return;
            try
            {
                Init();
                _loaded = true;
            }
            catch (Exception e)
            {
                ModHelper.Console.WriteLine($"Failed initialization. {e.Message}. {e.StackTrace}.", MessageType.Fatal);
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
            _camera.cullingMask &= ~(1 << 27) | (1 << 22);

            _cameraObject.name = "StaticCamera";
        }

        private void PlaceCameraAtPostion()
        {
            /*
            OWRigidbody relativeBody;

            var playerController = Locator.GetPlayerController();

            // What is the player standing on?
            if (playerController.IsGrounded())
            {
                relativeBody = playerController._groundBody;
                Write($"Player is grounded");
            }
            else
            {
                // If we were on the ground within the last few seconds go with that
                if(Time.time - playerController._lastGroundedTime < 2f)
                {
                    relativeBody = playerController._lastGroundBody;
                }
                else
                {
                    // What has the strongest gravity
                    var gravityVolumes = Locator.GetPlayerController()._forceDetector._activeVolumes.Where((x) => x is GravityVolume).Cast<GravityVolume>().ToArray();
                    GravityVolume strongestVolume = gravityVolumes[0];
                    float strongestGravitySqr = strongestVolume.CalculateForceAccelerationOnBody(Locator.GetPlayerBody()).sqrMagnitude;
                    for (int i = 1; i < gravityVolumes.Length; i++)
                    {
                        var gravityVolume = gravityVolumes[i];
                        float gravitySqr = gravityVolume.CalculateForceAccelerationOnBody(Locator.GetPlayerBody()).sqrMagnitude;
                        if (gravitySqr > strongestGravitySqr)
                        {
                            strongestGravitySqr = gravitySqr;
                            strongestVolume = gravityVolume;
                        }
                    }
                    relativeBody = strongestVolume._attachedBody;
                    Write($"The strongest gravity is {Mathf.Sqrt(strongestGravitySqr)}");
                }
            }
            
            Write($"Putting camera relative to {relativeBody.name}");
            */

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
                }
            }
            else if(_cameraOn)
            {
                _cameraOn = false;
                ShowReticule(true);
            }
        }

        private void ShowReticule(bool visible)
        {
            GameObject reticule = GameObject.Find("Reticule");
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

        private void Update()
        {
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
                    _cameraOn = false;
                    GlobalMessenger<OWCamera>.FireEvent("SwitchActiveCamera", _previousCamera);
                }
                else
                {
                    PlaceCameraAtPostion();
                }
            }
        }

        private void Write(string msg)
        {
            ModHelper.Console.WriteLine(msg, MessageType.Info);
        }

        private void WriteError(string msg)
        {
            ModHelper.Console.WriteLine(msg, MessageType.Error);
        }
    }
}
