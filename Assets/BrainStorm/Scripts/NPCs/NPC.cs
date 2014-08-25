﻿using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[AddComponentMenu("Character/NPC")]
public class NPC : MonoBehaviour {

	public enum Type {
		None		= 0x00,
		Native 		= 0x01,		// healthy NPCs (friendly to player)
		Infected 	= 0x02,		// (faction NPCs, Spider, etc)
		Virus		= 0x04,		// Virus' are friendly to one another, hostile to most other things
				//	= 0x08
				//	= 0x10
				//	= 0x20
				//	= 0x40
				//	= 0x80
		All			= 0xFF
	}
	
	public enum TargetSearchRange {
		NearRange,
		FarRange,
		FarthestRange
	}
	
	public enum TargetProximity {
		Here,
		Near,
		Far,
		OutOfRange,
	}
	
	[System.Serializable]
	public class TargetSearchCriteria {
		public float 			nearRange = 1f;
		public float 			farRange = 15f;
		public float			farthestRange = 30f;
		
		public bool					targetPlayer;
		public List<string>			validTargetTags = new List<string>();
		[BitMask(typeof(NPC.Type))]
		public Type					valid_NPC_Targets;
		public LayerMask			targetSearchMask;
		public TargetSearchRange	searchForTargetsIn = TargetSearchRange.FarthestRange;
		public float				timeBetweenTargetSearches = 2f;
		public float				timeBtwnSrchsRandomness = 1f;
	}
	
	// properties exposed to the unity inspector
	// -----------------------------------------
	public bool				drawDebug;
	public Type 			type;
	public int 				maxHealth = 100;
	public bool 			invulnerable = false;
	public int 				attackDamage = 5;
	public float			attackRate = 2f;
	
	public TargetSearchCriteria targetSearch = new TargetSearchCriteria();
	
	
	// other properties in the c# get/set style
	// ----------------------------------------
	 
	public int health {
		get { return _health; }
	}
	
	public bool isDead {
		get { return _health <= 0; }
	}
	
	// if set true, start search routine
	protected bool searchForTargets {
		get { return _searchingForTarget; }
		set {
			_searchingForTarget = value;
			// if we weren't already searching, start search routine
			if (_searchingForTarget && !_searchRoutineActive)
				StartCoroutine( FindTarget() );
		}
	}
	
	// well, do we hasTarget?
	public bool hasTarget {
		get { return _target != null; }
	}
	
	public TargetProximity targetProximity {
		get {
			if (!hasTarget) return TargetProximity.OutOfRange;
			float distance = Vector3.Distance(transform.position, _target.position);
			if (distance < _search.nearRange)
				return TargetProximity.Here;
			if (distance >= _search.nearRange && distance < _search.farRange)
				return TargetProximity.Near;
			if (distance >= _search.farRange && distance < _search.farthestRange)
				return TargetProximity.Far;
			//else
			return TargetProximity.OutOfRange;
		}
	}
	
	// target distance less than nearRange
	public bool targetIsHere {
		get {
			if (!hasTarget) return false;
			float distance = Vector3.Distance(transform.position, _target.position);
			return (distance < _search.nearRange);
		}
	}
	
	// target distance between nearRange and farRange
	public bool targetIsNear {
		get {
			if (!hasTarget) return false;
			float distance = Vector3.Distance(transform.position, _target.position);
			return (distance > _search.nearRange && distance < _search.farRange);
		}
	}
	
	// target distance between farRange and farthestRange
	public bool targetIsFar {
		get {
			if (!hasTarget) return false;
			float distance = Vector3.Distance(transform.position, _target.position);
			return (distance > _search.farRange && distance < _search.farthestRange);
		}
	}
	
	// target distance greater than farthestRange
	public bool targetIsOutOfRange {
		get {
			if (!hasTarget) return false;
			float distance = Vector3.Distance(transform.position, _target.position);
			return (distance > _search.farthestRange);
		}
	}
	
	// protected class members
	// -----------------------
	protected Transform 			_target;
	protected DamageInstance 		_damage = new DamageInstance();
	protected TargetSearchCriteria	_search;
	
	// private class members
	// ---------------------
	private int 			_health;
	private bool			_searchingForTarget = false;
	private bool 			_searchRoutineActive = false;
	
	protected virtual void Awake() {
		_search = targetSearch;
	}
	
	protected virtual void OnEnable() {
		// this scene is being loaded
		if (GameManager.Instance.levelTeardown) {
			if (_searchingForTarget) 
				StartCoroutine( FindTarget() );
		}
		// NPC has been enabled by some other means
		else {
			tag = "NPC";
			_health = maxHealth;
			_damage.source = this.transform;
			_damage.damage = attackDamage;
		}
	}
	
	protected virtual void OnDisable() {
		// this scene is being unloaded
		if (GameManager.Instance.levelTeardown) {

		}
		// NPC has been disabled by some other means
		else {
			tag = "Untagged";
			_target = null;
		}
	}
	
	
	protected virtual void Damage(DamageInstance damage) {
		if (invulnerable) return;
		if (_health <= 0) return;
		if (damage.source == this.transform) return;
		
		_health -= damage.damage;
		if (_health <= 0) {
			_health = 0;
			damage.source.SendMessage("Killed", this.transform);
		}
	}
	
	protected virtual void Killed(Transform victim) {
		
	}
	
	IEnumerator FindTarget() {
		_searchRoutineActive = true;
		while(_searchingForTarget == true) {
				
			if(_target) {
				// null target if it has changed to invalid tag
				// this happens if, for example, the target is killed
				bool targetStillValid = false;
				foreach(string s in _search.validTargetTags) 
					if (s == _target.tag)
						targetStillValid = true;
				
				if (!targetStillValid) _target = null;
			}

			// set search range for new target search
			float searchRange = 100f;
			switch (_search.searchForTargetsIn) {
			case TargetSearchRange.NearRange:
				searchRange = _search.nearRange;
				break;
			case TargetSearchRange.FarRange:
				searchRange = _search.farRange;
				break;
			case TargetSearchRange.FarthestRange:
				searchRange = _search.farthestRange;
				break;	
			}
			
			// perform search and inspect teh loot
			Collider[] colliders = Physics.OverlapSphere(transform.position, searchRange, _search.targetSearchMask);
			foreach(Collider c in colliders) {
				if ( c.isTrigger ) continue;
				if ( c.transform == this.transform) continue;
				
				foreach(string s in _search.validTargetTags) {
					// is tag valid?
					if (s == c.tag) {
						// is it an NPC?
						if (c.tag == "NPC") {
							// do i want to target this NPC?
							NPC npc = c.GetComponent<NPC>();
							if ((npc.type & _search.valid_NPC_Targets) == npc.type && 
								npc.type > 0) {
								_target = CompareTargets(_target, c.transform);
							}
						}
						// is it the player and can I target the player?
						else if (c.tag == "Player" && _search.targetPlayer) {
							_target = CompareTargets(_target, c.transform);
						}
						// for everything else, we just go for it
						else {
							_target = CompareTargets(_target, c.transform);
						}
					}
				}
			}
			
			if (_target && drawDebug)
				Debug.DrawLine(transform.position, _target.position, Color.red);
			
			float time = _search.timeBetweenTargetSearches;
			time += Random.Range(-_search.timeBtwnSrchsRandomness, 
			                     _search.timeBtwnSrchsRandomness);
			yield return new WaitForSeconds(time);
		}
		_searchRoutineActive = false;
	}
	
	Transform CompareTargets(Transform target1, Transform target2) {
		if (target1 == target2) return target1;
		if (target1 == null) return target2; // target2 won't be null because of previous line
		if (target2 == null) return target1;
		
		// choose closest target
		// at a later date could implement LOS priority
		float d1 = Vector3.Distance(transform.position, target1.position);
		float d2 = Vector3.Distance(transform.position, target2.position);
		if (d1 < d2) return target1;
		else return target2;
	}
}