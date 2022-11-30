using System;
using System.IO;

namespace Pickaxe.Messages;

public struct PlayerData : IBinarySerialize
{
	public string Name;
	public string SteamID;

	public void Deserialize( ref BinaryReader reader )
	{
		Name = reader.ReadString();
		SteamID = reader.ReadString();
	}

	public void Serialize( ref BinaryWriter writer )
	{
		writer.Write( (int)MessageType.PlayerData );
		writer.Write( Name );
		writer.Write( SteamID );
	}
}
