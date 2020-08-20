using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Pop
{
	[System.Serializable]
	public class UnityEvent_PacketBinary : UnityEngine.Events.UnityEvent<byte[]> { };

	[System.Serializable]
	public class UnityEvent_PacketString : UnityEngine.Events.UnityEvent<string> { };
}

public class PopPacketFileStreamParser : MonoBehaviour
{
	public string Filename;
	public Pop.UnityEvent_PacketBinary OnPacketBinary;
	public Pop.UnityEvent_PacketString OnPacketString;

	List<byte> PendingData;

	//	gr: queue up data, don't process now, so we can throttle, or allow multithread access
	public void OnFileChunk(byte[] Data)
	{
		if (PendingData == null)
			PendingData = new List<byte>();
		PendingData.AddRange(Data);
	}

	readonly byte[] PacketDelin = new byte[] { (byte)'P', (byte)'o', (byte)'p', (byte)'\n' };
	
	int GetNextPacketStart(int FromPosition=0)
	{
		//	gr: this needs to be a tight loop.
		//		can we cast to uint32 and compare with one op?
		//	gr: is the list[] accessor slow? should PendingData turn into an array of byte[] ?
		//	gr: iirc there's a speedup caching .count...
		for (; FromPosition< PendingData.Count - 4; FromPosition++)
		{
			if (PendingData[FromPosition + 0] != PacketDelin[0]) continue;
			if (PendingData[FromPosition + 1] != PacketDelin[1]) continue;
			if (PendingData[FromPosition + 2] != PacketDelin[2]) continue;
			if (PendingData[FromPosition + 3] != PacketDelin[3]) continue;
			return FromPosition;
		}

		throw new System.Exception("Found no next packet marker");
	}

	byte[] PopNextPacket()
	{
		var NextPacketStartPosition = GetNextPacketStart();
		var NextPacketEndPosition = GetNextPacketStart(NextPacketStartPosition+ PacketDelin.Length);
		var Packet = new byte[NextPacketEndPosition - NextPacketStartPosition];
		var SourceStart = NextPacketStartPosition + PacketDelin.Length;
		var TargetStart = 0;
		PendingData.CopyTo(SourceStart, Packet, TargetStart, Packet.Length);

		//	delete all the data up to next packet
		PendingData.RemoveRange(0, NextPacketEndPosition);
		return Packet;
	}

	//	return if more data to process
	bool ProcessNextData()
	{
		var Packet = PopNextPacket();
		if (Packet == null)
			return false;

		try
		{
			ParsePacket(Packet);
		}
		catch(System.Exception e)
		{
			Debug.LogError("Error parsing packet " + e.Message);
		}
		return true;
	}

	void ParsePacket(byte[] Data)
	{
		//	work out if its json meta or h264 packet and call event

	}

	void Update()
	{
		while (true)
		{
			if (!ProcessNextData())
				break;
		}
	}
}
