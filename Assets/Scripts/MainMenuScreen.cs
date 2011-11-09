using UnityEngine;
using System;

public class MainMenuScreen: MonoBehaviour {
	
	private string debugMsg="";	
	public GUISkin skin;
	public Texture2D backdrop;
	GUIStyle bgStyle = new GUIStyle();
	
	public void Update(){
		if (Input.GetKey(KeyCode.Escape)) Application.Quit(); // end game when Back is pressed
	}
	
	void OnGUI(){
		GUI.skin = skin;
		bgStyle.normal.background = backdrop;
		
		int w_center = (Screen.width/2);
		int h_center = (Screen.height/2);
		int w_double = (2*(Screen.width));
		int h_triple = (3*(Screen.height));
		
		//Background image
		//GUI.Label(new Rect((w_center-711),0,1422, 889), "", bgStyle);
		GUI.Label(new Rect(0,0,Screen.width, Screen.height), "", bgStyle);
		
		//Draw buttons
		if(GUI.Button (new Rect ((w_center-(Screen.width/5)),(h_center-(Screen.height/8)),(w_double/5) ,75), "Wind")) {
			//Application.LoadLevel ("tilt_mainScene");
			Application.LoadLevel ("TestScene");
		}
		if(GUI.Button (new Rect ((w_center-(Screen.width/5)),(h_center+(Screen.height/16)),(w_double/5) ,75),"Raccoon")) {
			Application.LoadLevel ("character_mainScene");
		}
		if(GUI.Button (new Rect ((w_center-(Screen.width/5)),(h_center+(Screen.height/4)),(w_double/5) ,75),"Instructions")) {
			Application.LoadLevel ("InstructionsScreen");
		}
		
		//GUI.Button (new Rect (10,120,150,100), "Debug:\n"+debugMsg);
	}
}

