using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;

public class Character_MainGame: MonoBehaviour {
	
	public Transform ARCamera;
	public Texture jumpButtonTex;
	public Texture fruitCount;
	public Texture lifeCount;
	
	public GUISkin skin;
	public Font font;
	
	private bool gameReady = false;
	private bool gameOn = false;
	private IPAddress opponentAddress;
	private UDPReceive udpReceive;
	private UDPSend udpSend;
	
	public string debugMsg="";
	
	public MyJoystick moveJoystick;
	GameObject mainPlayer;
	
	private float speed=0.0f;
	private float walkSpeed=0.0f;
	private bool isJumping=false;
	private bool hasJumpReachedApex=false;
	private bool isGroundedWithTimeout=false;
	private bool didLand=false;
	
	private int hackCount=5; // start at 5 so if-statement isn't used
	
	private String jumpButtonState="";
	
	public GameObject fruitToCollect;
	private List<GameObject> fruits;
	private int numCollectedFruits=0;
	
	public void Start(){
		udpReceive = GetComponent<UDPReceive>();
		udpSend = GetComponent<UDPSend>();
		mainPlayer = GameObject.Find("Raccoon");
		fruits = new List<GameObject>();
	}
	
	void Update(){
		if (Input.GetKey(KeyCode.Escape)) Application.Quit(); // end game when Back is pressed
			
		if(gameOn){
			String currentMsg = udpReceive.UDPcurrent;
			char indicator = currentMsg.ToCharArray()[0];
			//------------------ Parse Current Packet ---------------------
			char[] delim = {','};
			String[] sCoords = currentMsg.Split(delim);
			
			float[] realCoords = new float[9];
			for (int i=0; i<9; i++){
				realCoords[i] = float.Parse(sCoords[i]);
			}
			float[] speeds = new float[2];
			for (int i=9; i<11; i++){
				speeds[i-9] = float.Parse(sCoords[i]);
			}
			bool[] booleans = new bool[4];
			for (int i=11; i<15; i++){
				booleans[i-11] = bool.Parse(sCoords[i]);
			}
			numCollectedFruits = int.Parse(sCoords[15]);
			debugMsg = numCollectedFruits.ToString();
			
			List<float> fruitPos = new List<float>();
			for(int i=16; i < sCoords.Length; ++i){
				fruitPos.Add(float.Parse(sCoords[i]));
			}
			
			//------------------ Update Platform ---------------------
			transform.eulerAngles = new Vector3(realCoords[0],realCoords[1],realCoords[2]);
			
			//----------------- Update Character ---------------------
			mainPlayer.transform.position = new Vector3(realCoords[3],realCoords[4],realCoords[5]);
			mainPlayer.transform.eulerAngles = new Vector3(realCoords[6],realCoords[7],realCoords[8]);
			
			//----------------- Update Character Actions -------------
			speed = speeds[0];
			walkSpeed = speeds[1];
			isJumping = booleans[0];
			hasJumpReachedApex = booleans[1];
			isGroundedWithTimeout = booleans[2];
			didLand = booleans[3];
			
			//----------------- Update Fruit Positions -----------------
			for(int i=0; i<fruits.Count; i++) Destroy(fruits[i]); // destroy previous fruits before new ones are created
			fruits.Clear();
			
			for(int i=0; i<fruitPos.Count; i+=6){
				fruits.Add((GameObject)Instantiate(fruitToCollect, new Vector3(fruitPos[i],fruitPos[i+1],fruitPos[i+2]), 
				            Quaternion.Euler(fruitPos[i+3],fruitPos[i+4],fruitPos[i+5])));
			}
				
			//--------------- Send Joystick, Camera Position, and Jump ---------------------
			char[] remChar2 = {'(',')'};
			String joyPos = moveJoystick.position.ToString().TrimStart(remChar2).TrimEnd(remChar2).Replace( " ", "" );
			String camPos = ARCamera.forward.ToString().TrimStart(remChar2).TrimEnd(remChar2).Replace( " ", "" );
			//--------------- Send confirmation of received didLand ------------------------
			String land = "landFalse";
			if(didLand) hackCount = 0;
			if(hackCount < 5) { // HACK: send message 5 times so it is sure to be received
				land = "landTrue"; 
				hackCount++;
			}
			
			joyPos = "j" + joyPos + "," + camPos + "," + jumpButtonState + "," + land;
			udpSend.sendUDP(joyPos, opponentAddress);
				
		} else if(gameReady){
			udpSend.sendUDP("TiltMe", opponentAddress);
			char check = udpReceive.UDPcurrent.ToCharArray()[0];
			if(Char.IsNumber(check) || (check == '-') || (check == '.')) gameOn = true;
				
		} else if(udpReceive.UDPcurrent == "TilterOnline"){
			gameReady = true;
			opponentAddress = udpReceive.endPointCurrent.Address;
		}
	}
	
	public float getSpeed(){return speed;}
	public float getWalkSpeed(){return walkSpeed;}
	public bool getIsJumping(){return isJumping;}
	public bool getHasJumpReachedApex(){return hasJumpReachedApex;}
	public bool getIsGroundedWithTimeout(){return isGroundedWithTimeout;}
	public bool getDidLand(){return didLand;}
	
	void OnGUI(){
		
		
		//GUI.Button(new Rect(10,120,150,100), "Debug:\n"+debugMsg);
		if(GUI.Button(new Rect(10,10,150,100), "Main Menu"))
			Application.LoadLevel ("mainMenuScreen");
		if(GUI.Button(new Rect(Screen.width-200,Screen.height-200,100,100),jumpButtonTex))
			jumpButtonState = "Down";
		else jumpButtonState = "";
		
		GUI.skin = skin;
		
		GUI.Label(new Rect((Screen.width-260),0,270,158), fruitCount);
		GUI.Label(new Rect((Screen.width-70),20,270,158), numCollectedFruits.ToString());  //add variable for fruit count
		
		GUI.Label(new Rect(0,0,270,158), lifeCount);  //add variable for life count
		GUI.Label(new Rect(20,10,270,158), "1");
		
	}
	
		
}
