using System;
using System.IO;

namespace Pickaxe.Messages;

public interface IBinarySerialize
{
	void Serialize( ref BinaryWriter writer );

	void Deserialize( ref BinaryReader reader );

	public static void FromBytes<T>( byte[] bytes, out T obj ) where T : IBinarySerialize
	{
		using var ms = new MemoryStream( bytes );
		var br = new BinaryReader( ms );
		obj = Activator.CreateInstance<T>();
		obj.Deserialize( ref br );
		br.Dispose();
	}
	public static T FromReader<T>( ref BinaryReader reader ) where T : IBinarySerialize
	{
		var obj = Activator.CreateInstance<T>();
		obj.Deserialize( ref reader );
		return obj;
	}


}



public enum MessageType
{
	PlayerData = 0,
	EntityData = 1,
	EntityUpdate = 2,
	EntityDelete = 3,

}
