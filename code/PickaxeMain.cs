using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using Microsoft.Win32;
using Sandbox;
using Tools;
using Tools.MapDoc;
using Tools.MapEditor;
using Tools.NodeEditor;

namespace Pickaxe;
public class PickaxeClient
{
	public static bool IsServer { get; set; } = false;
	private static PlayerClient _client { get; set; } = new PlayerClient( 4096 );
	public static PlayerClient LocalClient
	{
		get
		{
			if ( IsServer )
			{
				return null;
			}
			if ( _client == null )
			{
				_client = new PlayerClient( 4096 );
			}
			return _client;
		}
		set
		{
			if ( _client != null )
			{
				_client.Disconnect();
			}
			_client = value;
		}
	}
	private static Telepathy.Server _server { get; set; } = new Telepathy.Server( 4096 );
	public static Telepathy.Server LocalServer
	{
		get
		{
			if ( !IsServer )
			{
				return null;
			}
			if ( _server == null )
			{
				_server = new Telepathy.Server( 4096 );
			}
			return _server;
		}
		set
		{
			if ( _server != null )
			{
				_server.Stop();
			}
			_server = value;
		}
	}

	public static Dictionary<int, PlayerClient> Players = new();

	public const int SERVERPORT = 7777;

	public enum ServerStatusCodes
	{
		NotConnected,
		Connected,
		Connecting,
		Started,
		Stopped,
		Failed,
		Unknown
	}

	public static ServerStatusCodes ServerStatus { get; internal set; } = ServerStatusCodes.NotConnected;

	private static ServerStatusCodes _lastServerStatus = ServerStatusCodes.NotConnected;

	//
	// An Editor menu will put the option in the main editor's menu
	//
	[Menu( "Hammer", "Pickaxe/Client" )]
	public static void ExampleMenuOption()
	{
		Log.Info( "Menu option selected! Well done!" );

		CheckStatus();
		// Create a new window
		var window = ClientWindow.Instance;
		window.Show();
		window.Focus();

	}

	private static void CheckStatus()
	{
		if ( IsServer )
		{
			if ( LocalServer.Active )
				ServerStatus = ServerStatusCodes.Started;
			else
			{
				ServerStatus = ServerStatusCodes.Stopped;
			}
		}
		else
		{
			if ( LocalClient.Connected )
			{
				ServerStatus = ServerStatusCodes.Connected;
			}
			else if ( LocalClient.Connecting )
			{
				ServerStatus = ServerStatusCodes.Connecting;
			}
			else
			{
				ServerStatus = ServerStatusCodes.NotConnected;
			}

		}
		if ( _lastServerStatus != ServerStatus )
		{
			_lastServerStatus = ServerStatus;
			UpdateUI();
		}
	}
	public static RealTimeSince LastUpdate = 0;
	public static RealTimeSince LastMapUpdate = 0;

	[Event.Frame]
	public static void UpdateClient()
	{
		if ( !Hammer.Open || Hammer.ActiveMap == null )
		{
			return;
		}
		if ( LastMapUpdate > 0.5f )
		{
			LastMapUpdate = 0;
			UpdateMapDocument();
		}
		if ( LastUpdate < 0.1 ) return;
		if ( !IsServer )
		{
			LocalClient.Tick( 100 );
		}
		else
		{
			LocalServer.Tick( 100 );

		}
		LastUpdate = 0;
		CheckStatus();
	}




	[Event.Hotload]
	public static void OnHotload()
	{
		Log.Info( "Hotloaded! Recreating Server" );
		if ( IsServer )
		{
			LocalServer.Stop();
			LocalServer.Start( SERVERPORT );
			ClearPlayers();
		}
		else
		{
			LocalClient.Disconnect();
			delayReconnect();
		}

	}

	[Event( "hammer.rendermapviewhud" )]
	public static void DisplayInfo()
	{
		if ( ServerStatus == ServerStatusCodes.NotConnected ) return;
		if ( IsServer )
		{
			ToolRender.DrawScreenText( $"PickaxeServer: {ServerStatus}", new( 20, 20 ), Color.White );
			int index = 0;
			foreach ( var item in Players )
			{
				ToolRender.DrawScreenText( $"{item.Key}: {item.Value.Name}", new( 25, 40 + (index * 20) ), Color.White );
				index++;
			}
		}
		else
		{
			ToolRender.DrawScreenText( $"PickaxeClient: {ServerStatus}", new( 20, 20 ), Color.White );
		}
	}

	public static void ClearPlayers()
	{
		foreach ( var item in Players )
		{
			item.Value.Dispose();
		}
		Players.Clear();
	}

	private static async void delayReconnect()
	{
		await Task.Delay( 1000 );
		LocalClient.Connect( LocalClient.ConnectedIP, SERVERPORT );
	}




	public static void StartServer()
	{
		IsServer = true;
		ClearPlayers();
		LocalServer.OnConnected += Server_OnConnected;
		LocalServer.OnData += Server_OnData;
		LocalServer.OnDisconnected += Server_OnDisconnected;
		LocalServer.Start( SERVERPORT );
		ServerStatus = ServerStatusCodes.Started;
		UpdateUI();
	}

	private static void Server_OnDisconnected( int obj )
	{
		Log.Info( $"Client {obj} disconnected" );
		if ( Players.ContainsKey( obj ) )
			Players[obj].Dispose();
		Players.Remove( obj );
		UpdateUI();
	}

	private static void Server_OnData( int arg1, ArraySegment<byte> arg2 )
	{
		var Player = Players[arg1];
		Player.PlayerClient_Server_OnData( arg2 );
		UpdateUI();
	}

	private static void Server_OnConnected( int obj )
	{
		if ( Players.ContainsKey( obj ) )
		{
			Log.Info( $"Client {obj} reconnected" );
			return;
		}
		Players[obj] = new PlayerClient( 4096 );
		UpdateUI();
	}

	[Event( "app.exit" )]
	public static void StopServer()
	{
		LocalServer.Stop();
		ClearPlayers();
		LocalServer.OnConnected -= Server_OnConnected;
		LocalServer.OnData -= Server_OnData;
		LocalServer.OnDisconnected -= Server_OnDisconnected;
		IsServer = false;
		ServerStatus = ServerStatusCodes.Stopped;
		UpdateUI();
	}



	public static void UpdateUI() => Event.Run( "serverstatus.update" );

	public static void ConnectToServer( string ipaddress )
	{
		if ( string.IsNullOrEmpty( ipaddress ) )
		{
			Log.Warning( "No IP address provided." );

			ipaddress = "localhost";
		}
		IsServer = false;
		LocalClient.ConnectedIP = ipaddress;
		LocalClient.Connect( ipaddress, SERVERPORT );
		CheckStatus();

		UpdateUI();

	}

	public static void DisconnectFromServer()
	{
		IsServer = false;
		LocalClient.Disconnect();
		ServerStatus = ServerStatusCodes.NotConnected;
		UpdateUI();
	}

	internal static void TestSendEntity()
	{
		var testent = new MapEntity
		{
			ClassName = "info_target"
		};
		testent.Position = Vector3.Random * 100;
		testent.Angles = Angles.Random;
		SendEntity( testent );
	}

	public static void SendEntity( MapEntity entity )
	{
		if ( IsServer )
		{
			foreach ( var item in Players )
			{
				LocalServer.Send( item.Key, entity.SerializeEntity() );
			}
		}
		else
		{
			LocalClient.Send( entity.SerializeEntity() );
		}
	}

	public static void SendToAll( byte[] data )
	{
		if ( IsServer )
		{
			foreach ( var item in Players )
			{
				LocalServer.Send( item.Key, data );
			}
		}
		else
		{
			LocalClient.Send( data );
		}
	}

	public static Dictionary<int, MapNode> Nodes = new();

	private static void UpdateMapDocument()
	{
		foreach ( var item in Selection.All )
		{
			if ( item is MapEntity ent )
			{
				int index = GetIndexofMapNode( item );
				if ( index == -1 ) continue;
				SendToAll( ent.SerializeEntityUpdate( index ) );
				Log.Info( $"Sending {GetIndexofMapNode( item )} to all clients" );
			}
		}
	}

	private static int GetIndexofMapNode( MapNode node )
	{

		int id = 0;
		foreach ( var item in Hammer.ActiveMap.World.Children )
		{
			if ( item is MapNode )
			{
				if ( item == node )
				{
					return id;
				}
			}
			id++;
		}
		return -1;
	}

	public static MapNode GetMapNodeFromIndex( int index )
	{
		int id = 0;
		foreach ( var item in Hammer.ActiveMap.World.Children )
		{
			if ( item is MapNode )
			{
				if ( id == index )
				{
					return item;
				}
			}
			id++;
		}
		return null;
	}

}
