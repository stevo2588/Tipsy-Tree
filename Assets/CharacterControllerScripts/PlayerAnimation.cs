using UnityEngine;
using System;

[AddComponentMenu ("Third Person Player/Third Person Player Animation")]

public class PlayerAnimation: MonoBehaviour {
	
	public float runSpeedScale = 1.0f;
	public float walkSpeedScale = 1.0f;
	private Character_MainGame characterMainGame;
	
	public void Start (){
		// By default loop all animations
		GetComponent<Animation>().wrapMode = WrapMode.Loop;
	
		GetComponent<Animation>()["run"].layer = -1;
		GetComponent<Animation>()["walk"].layer = -1;
		GetComponent<Animation>()["idle"].layer = -2;
		GetComponent<Animation>().SyncLayer(-1);
	
		GetComponent<Animation>()["ledgefall"].layer = 9;	
		GetComponent<Animation>()["ledgefall"].wrapMode = WrapMode.Loop;
	
		// The jump animation is clamped and overrides all others
		GetComponent<Animation>()["jump"].layer = 10;
		GetComponent<Animation>()["jump"].wrapMode = WrapMode.ClampForever;
	
		GetComponent<Animation>()["jumpfall"].layer = 10;	
		GetComponent<Animation>()["jumpfall"].wrapMode = WrapMode.ClampForever;
	
		GetComponent<Animation>()["jumpland"].layer = 10;	
		GetComponent<Animation>()["jumpland"].wrapMode = WrapMode.Once;
	
		// We are in full control here - don't let any other animations play when we start
		GetComponent<Animation>().Stop();
		GetComponent<Animation>().Play("idle");
		
		characterMainGame = GameObject.Find("Cube").GetComponent<Character_MainGame>();
	}
	
	public void Update (){
		float currentSpeed = characterMainGame.getSpeed();
		
		//characterMainGame.debugMsg = "";
	
		if (currentSpeed > characterMainGame.getWalkSpeed()){ // Fade in run
			GetComponent<Animation>().CrossFade("run");
			GetComponent<Animation>().Blend("jumpland", 0); // We fade out jumpland quick otherwise we get sliding feet
			//characterMainGame.debugMsg = characterMainGame.getWalkSpeed().ToString();
		}
		else if (currentSpeed > 0.1){ // Fade in walk
			GetComponent<Animation>().CrossFade("walk");
			GetComponent<Animation>().Blend("jumpland", 0); // We fade out jumpland really quick otherwise we get sliding feet
			//characterMainGame.debugMsg = currentSpeed.ToString();
		}
		else{ // Fade out walk and run
			GetComponent<Animation>().Blend("walk", 0.0f, 0.3f);
			GetComponent<Animation>().Blend("run", 0.0f, 0.3f);
			GetComponent<Animation>().Blend("run", 0.0f, 0.3f);
			//characterMainGame.debugMsg = "fadeOutWalkAndRun";
		}
		GetComponent<Animation>()["run"].normalizedSpeed = runSpeedScale;
		GetComponent<Animation>()["walk"].normalizedSpeed = walkSpeedScale;
		
		if (characterMainGame.getIsJumping()){
			//characterMainGame.debugMsg = "jumping";
			if (characterMainGame.getHasJumpReachedApex()) {
				GetComponent<Animation>().CrossFade("jumpfall", 0.2f);
			}
			else {
				GetComponent<Animation>().CrossFade("jump", 0.2f);
			}
		}
		else if (!characterMainGame.getIsGroundedWithTimeout()){ // We fell down somewhere
			GetComponent<Animation>().CrossFade("ledgefall", 0.2f);
			//characterMainGame.debugMsg = "groundedWithTimeout";
		}
		else{ // We are not falling down anymore
			GetComponent<Animation>().Blend("ledgefall", 0.0f, 0.2f);
			//characterMainGame.debugMsg = "notFallingDownAnymore";
		}
		
		if (characterMainGame.getDidLand()) DidLand();
	}
	
	private void DidLand () {
		GetComponent<Animation>().Play("jumpland");
	}
}