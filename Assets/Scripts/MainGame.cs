using UnityEngine;
using System;
using System.Collections;
using System.Net;
using System.Net.Sockets;

public class MainGame: MonoBehaviour {
	
	public int player = 0; // 0: nothing, 1: Tilter, 2: Character
	private bool gameReady = false;
	private bool gameOn = false;
	private IPAddress opponentAddress;
	private UDPReceive udpReceive;
	private UDPSend udpSend;
	
	private string debugMsg="";
	
	float AccelerometerUpdateInterval = 1.0f / 60.0f;
	float LowPassKernelWidthInSeconds = 1.0f;	
	float LowPassFilterFactor;  // tweakable
	Vector3 lowPassValue = Vector3.zero;
	
	public void Start(){
		udpReceive = GetComponent<UDPReceive>();
		udpSend = GetComponent<UDPSend>();
		LowPassFilterFactor = AccelerometerUpdateInterval / LowPassKernelWidthInSeconds;
		lowPassValue = Input.acceleration;
	}
	
	void Update(){
		if (Input.GetKey(KeyCode.Escape)) Application.Quit(); // end game when Back is pressed
		//------------ Tilter Gameplay ---------------
		if(player == 1){
			
			if(gameOn){
				//-------------- Platform Orientation ----------------
				Vector3 rot = LowPassFilterAccelerometer();
				string rotX = ((rot.x)*180).ToString();
				string rotY = "0.0";
				string rotZ = ((rot.y)*180).ToString();
				string rotMsg = "("+rotX+","+rotY+","+rotZ+")";
				udpSend.sendUDP(rotMsg, opponentAddress);
				debugMsg = opponentAddress.ToString();
				//-------------- Receive Joystick Position ----------
				
				
			} else {
				udpSend.broadcastUDP("TilterOnline");
				if(udpReceive.UDPcurrent == "TiltMe"){
					gameOn = true;
					opponentAddress = udpReceive.endPointCurrent.Address;
				}
			}
			
		//------------ Character Gameplay ---------------
		} else if(player == 2){
			
			if(gameOn){
				//------------------ Receive, Parse and Update Rotation ---------------------
				char[] delim = {','};
				char[] remChar = {'(',')'};
				string coordString = udpReceive.UDPcurrent;
				String[] sCoords = coordString.TrimEnd(remChar).TrimStart(remChar).Split(delim);
				float[] realCoords = new float[3];
				for (int i=0; i<3; i++){
					realCoords[i] = float.Parse(sCoords[i]);
				}
				transform.eulerAngles = new Vector3(realCoords[0],realCoords[1],realCoords[2]);
				
				//--------------- Send Joystick Position ---------------------
				
				
			} else if(gameReady){
				udpSend.sendUDP("TiltMe", opponentAddress);
				if(udpReceive.UDPcurrent.Substring(0,1) == "(") gameOn = true;
				
			} else if(udpReceive.UDPcurrent == "TilterOnline"){
				gameReady = true;
				opponentAddress = udpReceive.endPointCurrent.Address;
			}
		}
	}
	
	private Vector3 LowPassFilterAccelerometer() {
		lowPassValue = Vector3.Lerp(lowPassValue, Input.acceleration, LowPassFilterFactor);
		return lowPassValue;
	}
	
	void OnGUI(){
		if(GUI.Button (new Rect (170,10,150,100), "Tilter")) {
			Application.LoadLevel ("tilt_mainScene");
			player = 1;
		}
		if(GUI.Button (new Rect (330,10,150,100),"Character")) {
			Application.LoadLevel ("character_mainScene");
			player = 2;
		}
		GUI.Button (new Rect (10,120,150,100), "Debug:\n"+debugMsg);
	}
}

