/*
Stephen Aldriedge - 2011
UDP Sockets
	-------------------------
		Host: 127.0.0.1
		Port: 29129
*/
using UnityEngine;
using System.Collections;
using System;
using System.IO;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

public class UDPSend: MonoBehaviour {
	
	UdpClient sender;
	private int sendToPort = 8080;		// remote port where data will be sent
	private int sendFromPort = 8081;	// local port where data is sent from
	private IPEndPoint broadcastEndPoint;	// remote end point where data will be sent
	private IPEndPoint localEndPoint;	// local end point where data is sent from
	
    public void Start(){
		broadcastEndPoint = new IPEndPoint(IPAddress.Broadcast, sendToPort);
		localEndPoint = new IPEndPoint(IPAddress.Any, sendFromPort);
		sender = new UdpClient(localEndPoint);
    }
	
	void Update(){
	}
	
	public void sendUDP(string msg, IPAddress ip) {
		try {
			byte[] data = Encoding.UTF8.GetBytes(msg);
			sender.Send(data, data.Length, new IPEndPoint(ip,sendToPort));
		} catch (Exception err) {
			print(err.ToString());
		}
	}
	
	public void broadcastUDP(string msg) {
		try {
			byte[] data = Encoding.UTF8.GetBytes(msg);
			sender.Send(data, data.Length, broadcastEndPoint);
		} catch (Exception err) {
			print(err.ToString());
		}
	}
}