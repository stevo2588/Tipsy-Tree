using UnityEngine;
using System;

public class InstructionsScreen: MonoBehaviour {
	
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
		
		//Background image
		//GUI.Label(new Rect((w_center-711),0,1422, 889), "", bgStyle);
		GUI.Label(new Rect(0,0,Screen.width, Screen.height), "", bgStyle);
		
		//Draw button
		if(GUI.Button (new Rect ((w_center-(Screen.width/2)),(Screen.height-(Screen.height/6)),(Screen.width/6),100), "Back")) {
			Application.LoadLevel ("mainMenuScreen");
		}
	}
}

