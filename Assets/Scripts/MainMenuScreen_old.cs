using UnityEngine;
using System;

public class MainMenuScreen_old: MonoBehaviour {
	
	private string debugMsg="";
	
	public void Update(){
		if (Input.GetKey(KeyCode.Escape)) Application.Quit(); // end game when Back is pressed
	}
		
	void OnGUI(){
		//Resolution[] res = Screen.resolutions;
		
		if(GUI.Button (new Rect (10,10,150,100), "Tilter")) {
			Application.LoadLevel ("TestScene");
		}
		if(GUI.Button (new Rect (200,10,150,100),"Character")) {
			Application.LoadLevel ("character_mainScene");
		}
		//GUI.Button (new Rect (10,120,150,100), "Debug:\n"+debugMsg);
	}
}

