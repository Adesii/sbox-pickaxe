using System;
using System.IO;
using Pickaxe.Messages;
using Telepathy;
using Tools.MapDoc;
using Tools.MapEditor;

namespace Pickaxe;

public class PlayerClient : Client
{
	public PlayerClient( int MaxMessageSize ) : base( MaxMessageSize )
	{
		this.OnData += PlayerClient_OnData;
		this.OnConnected += PlayerClient_OnConnected;
		this.OnDisconnected += PlayerClient_OnDisconnected;
	}

	~PlayerClient()
	{
		Dispose();
	}

	public void Dispose()
	{
		this.OnData -= PlayerClient_OnData;
		this.OnConnected -= PlayerClient_OnConnected;
		this.OnDisconnected -= PlayerClient_OnDisconnected;
	}

	public void PlayerClient_OnDisconnected()
	{
		PickaxeClient.UpdateUI();

	}

	public void PlayerClient_OnConnected()
	{
		Log.Info( "Connected to server" );
		GetPlayerData();
		SendPlayerData();
		PickaxeClient.UpdateUI();
	}

	public string Name { get; set; } = "Connecting....";
	public string SteamID { get; set; } = "STEAM_0:0:0";

	public string ConnectedIP { get; set; } = "";

	public void PlayerClient_OnData( ArraySegment<byte> msg )
	{
		var reader = new BinaryReader( new MemoryStream( msg.Array, msg.Offset, msg.Count ) );
		var type = (MessageType)reader.ReadInt32();
		Log.Info( $"Received message type: {type}" );
		switch ( type )
		{
			case MessageType.PlayerData:
				var data = IBinarySerialize.FromReader<PlayerData>( ref reader );
				Log.Info( $"Received data from {data.Name} ({data.SteamID})" );
				Name = data.Name;
				SteamID = data.SteamID;
				break;
			case MessageType.EntityData:
				var entityData = IBinarySerialize.FromReader<EntityData>( ref reader );
				Log.Info( $"Received entity data for {entityData.ClassName}" );
				_ = new MapEntity()
				{
					Position = entityData.Position,
					Angles = entityData.Rotation,
					ClassName = entityData.ClassName
				};
				break;
			case MessageType.EntityUpdate:
				SerializationExtensions.DeserializeEntityUpdate( ref reader );
				break;
		}
	}

	public void PlayerClient_Server_OnData( ArraySegment<byte> msg )
	{
		PlayerClient_OnData( msg );
		PickaxeClient.UpdateUI();

	}

	private void GetPlayerData()
	{
		try
		{
			var steaminstall = Microsoft.Win32.Registry.GetValue( "HKEY_CURRENT_USER\\Software\\Valve\\Steam", "SteamPath", "No Steam installed" ) as string;
			if ( steaminstall == "No Steam installed" )
			{
				Log.Error( "Steam is not installed." );
				return;
			}
			var config = System.IO.File.ReadAllText( $"{steaminstall}/config/loginusers.vdf" );
			var steamid = config.IndexOf( "\"", config.IndexOf( "users" ) + 7 ) + 1;
			var username = config.IndexOf( "\"", config.IndexOf( "PersonaName" ) + 13 ) + 1;
			var steamidend = config.IndexOf( "\"", steamid );
			var usernameend = config.IndexOf( "\"", username );
			var steamidstring = config[steamid..steamidend];
			var usernamestring = config[username..usernameend];
			SteamID = steamidstring;
			Name = usernamestring;
		}
		catch ( System.Exception )
		{

			Log.Error( "Failed to get Steam data." );
			Name = "MISSINGNO";
			SteamID = "=0==0===0=";
		}

	}


	private void SendPlayerData()
	{
		var data = new PlayerData
		{
			Name = Name,
			SteamID = SteamID
		};
		using var stream = new MemoryStream();
		var writer = new BinaryWriter( stream );
		data.Serialize( ref writer );
		Send( stream.ToArray() );
		Log.Info( $"Sent data to server: {data.Name} ({data.SteamID})" );

		writer.Dispose();
	}
}

