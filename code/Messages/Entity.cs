using System;
using System.IO;
using Sandbox;

namespace Pickaxe.Messages;

public struct EntityData : IBinarySerialize
{
	public Vector3 Position;
	public Angles Rotation;
	public string ClassName;

	public void Deserialize( ref BinaryReader reader )
	{
		Position = reader.ReadVector3();
		Rotation = reader.ReadAngles();
		ClassName = reader.ReadString();
	}

	public void Serialize( ref BinaryWriter writer )
	{
		writer.Write( (int)MessageType.EntityData );
		writer.Write( Position );
		writer.Write( Rotation );
		writer.Write( ClassName );
	}
}

public struct EntityUpdate : IBinarySerialize
{
	public string Key, Value;

	public void Deserialize( ref BinaryReader reader )
	{
		Key = reader.ReadString();
		Value = reader.ReadString();
	}

	public void Serialize( ref BinaryWriter writer )
	{
		writer.Write( Key ?? "" );
		writer.Write( Value ?? "" );
	}
}
