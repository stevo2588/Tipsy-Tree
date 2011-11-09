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

public class UDPReceive: MonoBehaviour {
	
    Thread UDPthread; // receiving Thread
	UdpClient receiver; // udp handle
	private int localPort = 8080; 		// local port, where data will be received
	private IPEndPoint localEndPoint; 	// end point where data will be received
	private int remotePort = 8081; 		// remote port, where data will be sent from
	private IPEndPoint remoteEndPoint; 	// end point where data will be sent from
	//private ArrayList myEpList;

	public string UDPcurrent = "";
	private string debugMsg = "";
	public IPEndPoint endPointCurrent;
	private byte[] data;
	
    public void Start(){
		localEndPoint = new IPEndPoint(IPAddress.Any, localPort);
		receiver = new UdpClient(localEndPoint);
		remoteEndPoint = new IPEndPoint(IPAddress.Any, remotePort);
		//myEpList = getMyIPs();
    	startUDP();
    }
	
	void Update(){
	}

    // Create a new thread to receive incoming messages.
    private void startUDP() {
        UDPthread = new Thread(
            new ThreadStart(getUDP)); // uses getUDP as a callback to create thread
        UDPthread.IsBackground = true; // background threads do not prevent a process from terminating
        UDPthread.Start();
    }

	// receive thread 
    private void getUDP() { // callback used to create thread
        while (true) {
            try { // Receive bytes.
				print(localEndPoint.ToString());
				//foreach(IPEndPoint ep in myEpList) // make sure we don't receive from ourselves
				//	if(ep.Equals(remoteEndPoint)) return;
				data = receiver.Receive(ref remoteEndPoint); // receive from REMOTE HOST
                
                UDPcurrent = Encoding.ASCII.GetString(data);
				endPointCurrent = remoteEndPoint;
            } catch (Exception err) {
                print(err.ToString());
            }
        }
    }
	
	// return list of local end points
	private ArrayList getMyIPs() {
		IPHostEntry host;
		host = Dns.GetHostEntry(Dns.GetHostName());
		ArrayList epList = new ArrayList();
		debugMsg = "";
		foreach (IPAddress ip in host.AddressList){ // create list of local end points
			epList.Add(new IPEndPoint(ip, remotePort));
			debugMsg = debugMsg + "\n" + ip.ToString();
		}
		return epList;
	}

    public void OnApplicationQuit(){
    	if (UDPthread!=null){	
    		 UDPthread.Abort();
    	}
    }
	
	/*void OnGUI(){
		GUI.Button (new Rect (10,10,150,100), "From "+remoteEndPoint.ToString()+":\n"+UDPcurrent);
		GUI.Button (new Rect (10,120,150,100), "Debug:\n"+debugMsg);
	}*/
}