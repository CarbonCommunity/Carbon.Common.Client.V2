using System.IO;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System;
using UnityEngine;

namespace Carbon.Client;

public enum MessageType
{
	LAST = -1,
	UNUSED = 0,

	Approval,
	Rpc,

	EntityUpdate_Full,
	EntityUpdate_Position
}


