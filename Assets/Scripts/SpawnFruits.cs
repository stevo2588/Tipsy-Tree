using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class SpawnFruits: MonoBehaviour	{
	
	GameObject[] sp;
	public int fruitMax=5;
	private int fruitCount=0;
	private int spawnCount=0;
	
	public Rigidbody fruitToCollect;
	
	public void Start(){
		sp = GameObject.FindGameObjectsWithTag("FruitSpawn");
		spawnCount = sp.Length;
		if(fruitMax > spawnCount) fruitMax = spawnCount;

		StartCoroutine(Spawn());
	}
	
	public void Update() {
	}
	
	private IEnumerator Spawn(){
		int[] seq = Enumerable.Range(0, spawnCount).ToArray();
		RandomizeIntArray(seq);
		
		int i=0;
		while(fruitCount < fruitMax){
			CreateFruit(seq[i]);
			yield return new WaitForSeconds(3); // wait 3 seconds
			++i;
		}
	}
	
	public void CreateFruit(int index){
		Rigidbody fruit = Instantiate(fruitToCollect, sp[index].transform.position, sp[index].transform.rotation) as Rigidbody;
		fruitCount++;
	}
	
	public void DestroyFruit(Rigidbody fruit){
		Destroy(fruit.gameObject);
		fruitCount--;
	}
		
	private void RandomizeIntArray(int[] arr){
		for (int i = arr.Length - 1; i > 0; i--) {
	        int r = UnityEngine.Random.Range(0,i);
	        int tmp = arr[i];
	        arr[i] = arr[r];
	        arr[r] = tmp;
	    }
	}
}
