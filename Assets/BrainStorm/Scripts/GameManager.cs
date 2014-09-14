﻿using UnityEngine;
using System.Collections;

[RequireComponent(typeof(ScreenFade))]
public class GameManager : MonoBehaviour {

	public static GameManager Instance;	
	
	[System.Flags]
	public enum WinStates {
		None 	= 0x00,
		Grief 	= 0x01,
		Rage	= 0x02,
		Terror 	= 0x04,
		All		= 0xFF
	}
	
	// if you press R then the current render settings are copied
	// to the current scene in scenes[] to be saved manually
	// by the game designer
	public bool copyRenderSettings = false;
	[EnumMask]
	public WinStates winState = WinStates.None;
	public Scene[] scenes;
	public Scene copyScene;
	private bool _paused;
	private bool _levelTeardown;
	private float _sceneChangeTime = -999f;
	private Scene _activeScene;
	private Quaternion _camRotationBeforePause;
	private GUIText _header;
	private ScreenFade _fade;
	
	public bool paused {
		get { return _paused; }
		set {
			_paused = value;
			if (_paused) {
				_camRotationBeforePause = Camera.main.transform.localRotation;
				Time.timeScale = 0f;
				AudioListener.volume = 0f;
				Screen.lockCursor = false;
				CTRL.Instance.ShowPauseMenu();
			}

			else {
				Camera.main.transform.localRotation = _camRotationBeforePause;
				Time.timeScale = 1f;
				AudioListener.volume = 1f;
				Screen.lockCursor = true;
				CTRL.Instance.HidePauseMenu();
			}
		}
	}
	
	public bool levelTeardown {
		get { return _levelTeardown; }
	}
	
	public Transform activeScene {
		get {
			if (_activeScene != null) 
				return _activeScene.instance;
			else 
				return null;
		}
	}
	
	public float timeSinceSceneChange {
		get {
			return Time.time - _sceneChangeTime;
		}
	}
	
	public bool griefComplete {
		get {
			return (
				(winState & WinStates.Grief) == WinStates.Grief &&
				winState != WinStates.None
			);
		}
		set {
			if (value) 
				winState |= WinStates.Grief;
			else 
				winState &= ~WinStates.Grief;
		}
	}
	
	public bool rageComplete {
		get {
			return (
				(winState & WinStates.Rage) == WinStates.Rage &&
				winState != WinStates.None
			);
		}
		set {
			if (value) 
				winState |= WinStates.Rage;
			else 
				winState &= ~WinStates.Rage;
		}
	}
	
	public bool terrorComplete {
		get {
			return  (
				(winState & WinStates.Terror) == WinStates.Terror &&
				winState != WinStates.None
			);
		}
		set {
			if (value)
				winState |= WinStates.Terror;
			else
				winState &= ~WinStates.Terror;
		}
	}

	void Awake() {
		if (Instance == null) {
			Instance = this;
		}
		else {
			Destroy(this.gameObject);
		}
		_header = transform.Find("Header").guiText;
		_header.text = Strings.gameVersion;
		_fade = GetComponent<ScreenFade>();
		transform.position = Vector3.zero;
	}
	
	
	void Start () {
		DontDestroyOnLoad(this);
		StartGame();
	}
	
	// this is called when the game is restarted
	// Application.LoadLevel(Application.loadedLevel);
	void OnLevelWasLoaded() {
		StartGame();
	}
	
	void StartGame() {
		Screen.lockCursor = true;
		if (Application.loadedLevelName == "brainstorm") {
			ChangeScene(Scene.Tag.Lobby);
		}
		paused = false;
	}
	
	
	void Update () {
		if (!Screen.lockCursor && !paused) {
			paused = true;
		}
		if (Input.GetKeyUp(KeyCode.Escape) && !paused) {
			paused = true;
		}
		if (Application.isEditor & copyRenderSettings) {
			CopyRenderSettings();
		}
	}
	
	void CopyRenderSettings() {
		if (Input.GetKeyUp(KeyCode.R)) {
			copyScene = new Scene();
			copyScene.ambientLight = RenderSettings.ambientLight;
			copyScene.fog = RenderSettings.fog;
			copyScene.fogColor = RenderSettings.fogColor;
			copyScene.fogDensity = RenderSettings.fogDensity;
			copyScene.skybox = RenderSettings.skybox;
		}
	}
	
	public void SceneComplete() {
		switch (_activeScene.tag) {
		case Scene.Tag.Grief:
			ChangeScene(Scene.Tag.Joy);
			break;
		case Scene.Tag.Rage:
			ChangeScene(Scene.Tag.Calm);
			break;
		case Scene.Tag.Terror:
			ChangeScene(Scene.Tag.Safety);
			break;
		}
	}
	
	public void ChangeScene(Scene.Tag scene) {
		if (timeSinceSceneChange < 1f) return;
		paused = false;
		_sceneChangeTime = Time.time;
		StartCoroutine( ChangeSceneRoutine(scene) );
	}
	
	private IEnumerator ChangeSceneRoutine( Scene.Tag scene ) {
		_fade.StartFade(Color.black, 0.5f);
		yield return new WaitForSeconds(0.5f);
		yield return new WaitForEndOfFrame();
		_levelTeardown = true;
		// unload active scene
		if (_activeScene != null)
			_activeScene.Unload();
		// spawn next scene
		foreach(Scene s in scenes) {
			if (s.tag == scene) {
				_activeScene = s;
				_activeScene.Load();
				RenderSettings.ambientLight = _activeScene.ambientLight;
				RenderSettings.fog = _activeScene.fog;
				RenderSettings.fogColor = _activeScene.fogColor;
				RenderSettings.fogDensity = _activeScene.fogDensity;
				RenderSettings.skybox = _activeScene.skybox;
				break;
			}
		}
		_levelTeardown = false;
		yield return new WaitForEndOfFrame();
		_fade.StartFade(Color.clear, 0.5f);
		yield return new WaitForSeconds(0.5f);
	}
	
}
