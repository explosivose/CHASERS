﻿using UnityEngine;
using System.Collections;

// get brighter as player gets closer
// when player is close enough, whiteout and load lobby 

[RequireComponent(typeof(ScreenFade))]
public class Artefact : MonoBehaviour {

	public float closestDistance = 5f;
	public float farthestDistance = 10f;

	public bool				_revealed;
	private Transform 		_player;
	private ScreenFade		_fade;
	private ParticleSystem 	_particles;
	private Collider		_collider;
	private Renderer		_renderer;
	
	void Start() {
		_player = Player.localPlayer.transform;
		_fade = GetComponent<ScreenFade>();
		_particles = GetComponentInChildren<ParticleSystem>();
		_collider = GetComponentInChildren<Collider>();
		_renderer = GetComponentInChildren<Renderer>();
	}
	
	public void Reveal() {
		_revealed = true;
		rigidbody.isKinematic = false;
		_collider.enabled = true;
		_renderer.enabled = true;
		_particles.Play();
		audio.Play();
	}
	
	// Update is called once per frame
	void Update () {
		
		if (!_revealed) return;
		
		float playerDistance = Vector3.Distance(_player.position, transform.position);
		
		float farDist = farthestDistance - closestDistance;
		playerDistance -= closestDistance;
		
		float t = (farDist - playerDistance)/farDist;
		
		if (playerDistance < 0f) {
			//GameManager.Instance.SceneComplete();
		}
		
		if(t < 1f ) {
			Color screenOverlay = Color.Lerp (Color.clear, Color.cyan, t);
			_fade.SetScreenOverlayColor(screenOverlay);
		}
	}
}
