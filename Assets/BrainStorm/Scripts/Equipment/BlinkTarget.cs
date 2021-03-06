﻿using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Equipment))]
[AddComponentMenu("Player/Equipment/Blink")]
public class BlinkTarget : Photon.MonoBehaviour {

	public Transform 	blinkTargetPrefab;
	public Transform	blinkStartEffect;
	public Transform	blinkStopEffect;
	public float 		blinkSpeed;
	public float 		maxRange;				// how long is our raycast?
	public float 		rechargeTime;			// how long to get from 0 to maxRange?
	public float 		cooldown;				// how long before we can use it again?
	public LayerMask 	raycastMask;			// which layers can we hit?
	
	private Transform 	_blinkTarget;
	private bool 		_blinking;
	private float		_range;
	private float 		_lastUseTime = -999f;
	private Transform	_player;
	private Equipment   _equipment;
	
	public bool canBlink {
		get {
			if (!_equipment.equipped) return false;
			return _lastUseTime + cooldown < Time.time;
		}
	}
	
	void Awake() {
		blinkTargetPrefab.CreatePool();
		blinkStartEffect.CreatePool();
		blinkStopEffect.CreatePool();
		_equipment = GetComponent<Equipment>();
	}
	
	void OnEquip() {
		if (_equipment.owner) {
			if (_equipment.owner.isLocalPlayer) {
				_player = _equipment.owner.transform;
				_equipment.owner.inventory.hasBlink = true;
			}
		}
	}
	
	void OnDrop() {
		if (_equipment.owner) {
			if (_equipment.owner.isLocalPlayer) {
				_equipment.owner.inventory.hasBlink = false;
			}
		}
			
		if (_blinkTarget) _blinkTarget.Recycle();
		_blinkTarget = null;
	}
	
	// Update is called once per frame
	void Update () {
		if (!_equipment.equipped) return;
		if (GameManager.Instance.paused) {
			if(_blinkTarget)
				_blinkTarget.Recycle();
			_blinkTarget = null;
			return;
		};
		_equipment.energy = _range/maxRange;
		// recharge
		_range += Time.deltaTime * maxRange/rechargeTime;
		_range = Mathf.Min (_range, maxRange);
		_range = Mathf.Max (0f, _range);
		if (_blinking) {
			Blink();
			return;
		}
		
		
		
		if (Input.GetButtonDown("Sprint") && canBlink) {
			if (!_blinkTarget)
				_blinkTarget = blinkTargetPrefab.Spawn();
		}
		
		if (_blinkTarget) {
			PositionTarget();
		}
		
		if (Input.GetButtonUp("Sprint") && _blinkTarget ) {
			StartBlink();
		}


	}
	
	void StartBlink() {
		_blinking = true;
		_equipment.owner.motor.enabled = false;
		photonView.RPC("StartBlinkRPC", PhotonTargets.All);
		_equipment.owner.photonView.RPC ("OnBlinkStartRPC", PhotonTargets.All);
		_blinkTarget.Recycle();
		_lastUseTime = Time.time;
	}
	
	[RPC]
	void StartBlinkRPC() {
		blinkStartEffect.Spawn(transform.position);
	}

	void StopBlink() {
		_blinking = false;
		_blinkTarget = null;
		_equipment.owner.motor.movement.velocity = Vector3.zero;
		_equipment.owner.motor.enabled = true;
		photonView.RPC ("StopBlinkRPC", PhotonTargets.All);
		_equipment.owner.photonView.RPC("OnBlinkStopRPC", PhotonTargets.All);
	}
	
	[RPC]
	void StopBlinkRPC() {
		blinkStopEffect.Spawn(transform.position);
	}
	
	void Blink() {
		if (!_blinkTarget) {
			StopBlink();
			return;
		}
		Vector3 previousPosition = _player.position;
		_player.position = Vector3.Lerp(
			_player.position, 
			_blinkTarget.position,
			Time.deltaTime * blinkSpeed
		);
		float dist = Vector3.Distance(previousPosition, _player.position);
		_range -= dist;
		if (Vector3.Distance(_player.position, _blinkTarget.position) < 1f) {
			StopBlink();
		}
	}

	void PositionTarget() {
		Vector3 screenPoint = new Vector3 (
			x: Screen.width/2f,
			y: Screen.height/2f
			);
		Ray ray = Camera.main.ScreenPointToRay(screenPoint);
		RaycastHit hit;
		if (Physics.Raycast(ray, out hit, _range, raycastMask)) {
			_blinkTarget.position = hit.point + hit.normal * 2f;
		}
		else {
			_blinkTarget.position = transform.position +
				ray.direction * _range;
		}
	}
}
