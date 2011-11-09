using UnityEngine;
using System;

public class EndWindGameScreen: MonoBehaviour {
	
	private string debugMsg="";	
	public GUISkin skin;
	public Texture2D happyBackdrop;
	public Texture2D sadBackdrop;
	GUIStyle bgStyle = new GUIStyle();
	
	private bool winner = false;
	
	public void Update(){
		if (Input.GetKey(KeyCode.Escape)) Application.Quit(); // end game when Back is pressed
	}
	
	void OnGUI(){
		GUI.skin = skin;
		
		//Switch background image based on winner
		if (winner == true){bgStyle.normal.background = sadBackdrop;}
		else bgStyle.normal.background = happyBackdrop;
		
		
		int w_center = (Screen.width/2);
		int h_center = (Screen.height/2);
		int w_double = (Screen.width*2);
		
		//Background image
		//GUI.Label(new Rect((w_center-711),0,1422, 889), "", bgStyle);
		GUI.Label(new Rect(0,0,Screen.width, Screen.height), "", bgStyle);
		
		//Game over message
		if (winner == true){GUI.Label (new Rect ((w_center-(w_double/5)), (h_center-(Screen.height/4)), w_center, (Screen.height/16)), "You win!");}
		else GUI.Label (new Rect ((w_center), (h_center-(Screen.height/8)), w_center, (Screen.height/16)), "You lose!");
		
	}
}

