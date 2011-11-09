using UnityEngine;
using System;
using System.Collections;
using System.Net;
using System.Net.Sockets;

public class Tilter_MainGame: MonoBehaviour {
	
	private bool gameOn = false;
	private IPAddress opponentAddress;
	private UDPReceive udpReceive;
	private UDPSend udpSend;
	private ThirdPersonController2 thirdPersonController;
	
	public string debugMsg="";
	
	float AccelerometerUpdateInterval = 1.0f / 60.0f;
	float LowPassKernelWidthInSeconds = 1.0f;
	float LowPassFilterFactor;
	Vector3 lowPassValue = Vector3.zero;
	
	GameObject mainPlayer;
	private SpawnFruits spawnFruits;
	public Vector2 joystickPosition;
	public Vector3 ARCameraForward;
	
	public int collectedFruits = 0;
	
	public bool jump=false;
	public bool didLand=false;
	
	public void Start(){
		udpReceive = GetComponent<UDPReceive>();
		udpSend = GetComponent<UDPSend>();
		LowPassFilterFactor = AccelerometerUpdateInterval / LowPassKernelWidthInSeconds;
		lowPassValue = Input.acceleration;
		mainPlayer = GameObject.Find("Raccoon");
		thirdPersonController = mainPlayer.GetComponent<ThirdPersonController2>();
		spawnFruits = GameObject.Find("All Fruit Spawn Pts").GetComponent<SpawnFruits>();
	}
	
	void Update(){
		if (Input.GetKey(KeyCode.Escape)) Application.Quit(); // end game when Back is pressed
		//------------ Tilter Gameplay ---------------
		if(gameOn){
			String currentMsg = udpReceive.UDPcurrent;
			char indicator = currentMsg.ToCharArray()[0];
			
			//-------------- Update Platform --------------------------
			Vector3 rot = LowPassFilterAccelerometer();
			float rotBound = 30.0f;
			float platX = rot.x * 180;
			platX = (platX > rotBound) ? rotBound : platX;
			platX = (platX < -rotBound) ? -rotBound : platX;
			float platZ = rot.y * 180;
			platZ = (platZ > rotBound) ? rotBound : platZ;
			platZ = (platZ < -rotBound) ? -rotBound : platZ;
			transform.eulerAngles = new Vector3(platX,0,platZ);
			
			//-------------- Package Platform Position ----------------
			string rotX = platX.ToString();
			string rotY = "0.0";
			string rotZ = platZ.ToString();
			string rotMsg = rotX+","+rotY+","+rotZ;

			//-------------- Receive Joystick, Camera Position, Jump & didLand Confirmation ----------
			bool landConf = false;
			if(indicator == 'j'){
				char[] delim = {','};
				char[] remChar = {'j'};
				String[] sCoords = currentMsg.TrimStart(remChar).Split(delim);
				float[] realCoords = new float[5];
				for (int i=0; i<5; i++){
					realCoords[i] = float.Parse(sCoords[i]);
				}
				joystickPosition = new Vector2(realCoords[0],realCoords[1]);
				ARCameraForward = new Vector3(realCoords[2],realCoords[3],realCoords[4]);
				if(sCoords[5] == "Down") jump = true;
				if(sCoords[6] == "landTrue") landConf = true;
			}
			
			//------------- Package Player Position ----------------------
				
			char[] remChar2 = {'(',')'};
			String mpPos = mainPlayer.transform.position.ToString().TrimStart(remChar2)
			               .TrimEnd(remChar2).Replace( " ", "" );
			String mpRot = mainPlayer.transform.eulerAngles.ToString().TrimStart(remChar2)
			               .TrimEnd(remChar2).Replace( " ", "" );
			String mpTrans = mpPos + "," + mpRot;
			
			//-------------- Package Player Actions --------------------
			if(landConf) didLand = false; // didLand can only become false once we've received confirmation
			String actions = thirdPersonController.GetSpeed().ToString()+","+
							thirdPersonController.walkSpeed.ToString()+","+
							thirdPersonController.IsJumping().ToString()+","+
							thirdPersonController.HasJumpReachedApex().ToString()+","+
							thirdPersonController.IsGroundedWithTimeout().ToString()+","+
							didLand.ToString();
			
			//-------------- Package Fruit Positions ------------------------
			String fruitPos = "";
			GameObject[] fruits = GameObject.FindGameObjectsWithTag("Fruit");
			foreach(GameObject f in fruits) {
				fruitPos += f.transform.position.ToString().TrimStart(remChar2).TrimEnd(remChar2).Replace( " ", "" ) + "," +
					f.transform.eulerAngles.ToString().TrimStart(remChar2).TrimEnd(remChar2).Replace( " ", "" ) + ",";
			}
			fruitPos = fruitPos.TrimEnd(new char[]{','});
			
			//------------- Package Collected Fruits ------------------------
			String colFruits = collectedFruits.ToString();
			debugMsg = colFruits;
			
			//------------- Send All Updates ----------------------
			udpSend.sendUDP(rotMsg+","+mpTrans+","+actions+","+colFruits+","+fruitPos, opponentAddress);
				
		} 
		else {
			udpSend.broadcastUDP("TilterOnline");
			if(udpReceive.UDPcurrent == "TiltMe"){
				gameOn = true;
				opponentAddress = udpReceive.endPointCurrent.Address;
			}
		}
	}
	
	private Vector3 LowPassFilterAccelerometer() {
		lowPassValue = Vector3.Lerp(lowPassValue, Input.acceleration, LowPassFilterFactor);
		return lowPassValue;
	}
	
	/*void OnGUI(){
		GUI.Button (new Rect (10,120,150,100), "Debug:\n"+debugMsg);
	}*/
	void OnGUI(){
		if(GUI.Button(new Rect(10,10,150,100), "Main Menu"))
				Application.LoadLevel ("mainMenuScreen");
	}
}

