var runSpeedScale = 1.0;
var walkSpeedScale = 1.0;

function Start (){
	// By default loop all animations
	GetComponent.<Animation>().wrapMode = WrapMode.Loop;

	GetComponent.<Animation>()["run"].layer = -1;
	GetComponent.<Animation>()["walk"].layer = -1;
	GetComponent.<Animation>()["idle"].layer = -2;
	GetComponent.<Animation>().SyncLayer(-1);

	GetComponent.<Animation>()["ledgefall"].layer = 9;	
	GetComponent.<Animation>()["ledgefall"].wrapMode = WrapMode.Loop;

	// The jump animation is clamped and overrides all others
	GetComponent.<Animation>()["jump"].layer = 10;
	GetComponent.<Animation>()["jump"].wrapMode = WrapMode.ClampForever;

	GetComponent.<Animation>()["jumpfall"].layer = 10;	
	GetComponent.<Animation>()["jumpfall"].wrapMode = WrapMode.ClampForever;

	GetComponent.<Animation>()["jumpland"].layer = 10;	
	GetComponent.<Animation>()["jumpland"].wrapMode = WrapMode.Once;

	// We are in full control here - don't let any other animations play when we start
	GetComponent.<Animation>().Stop();
	GetComponent.<Animation>().Play("idle");
}

function Update (){
	var playerController : ThirdPersonController = GetComponent(ThirdPersonController);
	var currentSpeed = playerController.GetSpeed();

	if (currentSpeed > playerController.walkSpeed){ // Fade in run
		GetComponent.<Animation>().CrossFade("run");
		GetComponent.<Animation>().Blend("jumpland", 0); // We fade out jumpland quick otherwise we get sliding feet
	}
	else if (currentSpeed > 0.1){ // Fade in walk
		GetComponent.<Animation>().CrossFade("walk");
		GetComponent.<Animation>().Blend("jumpland", 0); // We fade out jumpland realy quick otherwise we get sliding feet
	}
	else{ // Fade out walk and run
		GetComponent.<Animation>().Blend("walk", 0.0, 0.3);
		GetComponent.<Animation>().Blend("run", 0.0, 0.3);
		GetComponent.<Animation>().Blend("run", 0.0, 0.3);
	}
	GetComponent.<Animation>()["run"].normalizedSpeed = runSpeedScale;
	GetComponent.<Animation>()["walk"].normalizedSpeed = walkSpeedScale;
	
	if (playerController.IsJumping ()){
		if (playerController.HasJumpReachedApex ())
		{
			GetComponent.<Animation>().CrossFade ("jumpfall", 0.2);
		}
		else
		{
			GetComponent.<Animation>().CrossFade ("jump", 0.2);
		}
	}
	else if (!playerController.IsGroundedWithTimeout()){ // We fell down somewhere
		GetComponent.<Animation>().CrossFade ("ledgefall", 0.2);
	}
	else{ // We are not falling down anymore
		GetComponent.<Animation>().Blend ("ledgefall", 0.0, 0.2);
	}
}

function DidLand () {
	GetComponent.<Animation>().Play("jumpland");
}

function DidButtStomp () {
	GetComponent.<Animation>().CrossFade("buttstomp", 0.1);
	GetComponent.<Animation>().CrossFadeQueued("jumpland", 0.2);
}

function Slam () {
	GetComponent.<Animation>().CrossFade("buttstomp", 0.2);
	var playerController : ThirdPersonController = GetComponent(ThirdPersonController);
	while(!playerController.IsGrounded()){
		yield;
	}
	GetComponent.<Animation>().Blend("buttstomp", 0, 0);
}

@script AddComponentMenu ("Third Person Player/Third Person Player Animation")