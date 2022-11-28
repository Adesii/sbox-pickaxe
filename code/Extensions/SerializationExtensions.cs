using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Pickaxe;
using Pickaxe.Messages;
using Tools.MapDoc;
using Tools.MapEditor;

public static class SerializationExtensions
{

	public static byte[] SerializeEntity( this MapEntity entity )
	{
		var data = new EntityData
		{
			Position = entity.Position,
			Rotation = entity.Angles,
			ClassName = entity.ClassName
		};
		using var stream = new MemoryStream();
		var writer = new BinaryWriter( stream );
		data.Serialize( ref writer );
		writer.Dispose();
		return stream.ToArray();
	}

	public static byte[] SerializeEntityUpdate( this MapEntity entity, int id )
	{
		var props = TypeLibrary.GetPropertyDescriptions( TypeLibrary.GetDescription( entity.ClassName ).TargetType );
		List<EntityUpdate> updates = new();
		foreach ( var prop in props )
		{
			var value = entity.GetKeyValue( prop.Name );
			if ( value is not null )
			{
				updates.Add( new EntityUpdate
				{
					Key = prop.Name,
					Value = value.ToString()
				} );
			}
		}

		updates.Add( new EntityUpdate
		{
			Key = "origin",
			Value = entity.Position.ToString()
		} );

		updates.Add( new EntityUpdate
		{
			Key = "angles",
			Value = $"{entity.Angles.pitch} {entity.Angles.yaw} {entity.Angles.roll}"
		} );

		updates.Add( new EntityUpdate
		{
			Key = "scale",
			Value = entity.Scale.ToString()
		} );



		using var stream = new MemoryStream();
		var writer = new BinaryWriter( stream );

		writer.Write( ((int)MessageType.EntityUpdate) );
		writer.Write( id );
		writer.Write( updates.Count );
		foreach ( var update in updates )
		{
			update.Serialize( ref writer );
		}
		writer.Dispose();
		return stream.ToArray();
	}

	public static void DeserializeEntityUpdate( ref BinaryReader br )
	{
		var id = br.ReadInt32();
		var count = br.ReadInt32();
		int entid = -1000;
		MapEntity ent = null;
		for ( int i = 0; i < count; i++ )
		{
			var update = IBinarySerialize.FromReader<EntityUpdate>( ref br );
			//if ( entid != update.EntityID )
			//{
			ent = PickaxeClient.GetMapNodeFromIndex( id ) as MapEntity;
			if ( ent is null )
			{
				Log.Warning( "Entity not found" );
				return;
			}
			//	entid = update.EntityID;
			//}
			if ( update.Key == "origin" )
			{
				ent.Position = Vector3.Parse( update.Value );
			}
			else if ( update.Key == "angles" )
			{
				ent.Angles = Angles.Parse( update.Value );
			}
			else if ( update.Key == "scale" )
			{
				ent.Scale = Vector3.Parse( update.Value );
			}
			else
			{
				ent.SetKeyValue( update.Key, update.Value );
			}
		}
		br.Dispose();
	}


}
