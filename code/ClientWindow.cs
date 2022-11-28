using System;
using Sandbox;
using Tools;
using Tools.Graphic;

namespace Pickaxe;

public class ClientWindow : Window
{
	private static ClientWindow _instance;
	public static ClientWindow Instance
	{
		get
		{
			if ( _instance == null || !_instance.IsValid() )
				_instance = new ClientWindow();
			return _instance;
		}
	}

	public ClientWindow()
	{
		Title = "Pickaxe Client";
		Size = new Vector2( 400, 400 );
		MinimumSize = new Vector2( 400, 400 );
		Visible = true;
		CreateUI();
	}

	public override void Show()
	{
		base.Show();
		//recreate ui.
		CreateUI();
	}
	[Event.Hotload, Event( "serverstatus.update" )]
	private void CreateUI()
	{
		if ( !Visible ) return;
		DestroyChildren();
		var canvas = new Widget( null );
		canvas.SetLayout( LayoutMode.TopToBottom );
		canvas.Layout.Spacing = 8;
		canvas.Layout.Margin = 8;
		Canvas = canvas;
		//Layout.Add( serverstatus );
		var serverstatus = new Label( "Server Status: " + PickaxeClient.ServerStatus.ToString().ToTitleCase() );
		canvas.Layout.Add( serverstatus );

		switch ( PickaxeClient.ServerStatus )
		{
			case PickaxeClient.ServerStatusCodes.NotConnected:
			case PickaxeClient.ServerStatusCodes.Stopped:
			case PickaxeClient.ServerStatusCodes.Failed:

				var StartServer = new Button( "Start Server" );
				StartServer.Clicked += () =>
				{
					PickaxeClient.StartServer();
				};
				canvas.Layout.Add( StartServer );


				var connectgroup = new Widget( null );
				connectgroup.SetLayout( LayoutMode.TopToBottom );
				connectgroup.Layout.Spacing = 8;
				connectgroup.Layout.Margin = 8;

				//var displayname = new TextEdit
				//{
				//	PlaceholderText = "Display Name",
				//	MaximumHeight = 32,
				//};
				var ipaddress = new TextEdit
				{
					PlaceholderText = "IP Address",
					MaximumHeight = 32,
				};
				ipaddress.SetSizeMode( SizeMode.Default, SizeMode.CanShrink );
				var connectbutton = new Button( "Connect" );
				connectbutton.Clicked += () =>
				{
					PickaxeClient.ConnectToServer( ipaddress.PlainText );
				};
				connectgroup.Layout.AddStretchCell( 10 );
				//connectgroup.Layout.Add( displayname );
				connectgroup.Layout.Add( ipaddress );
				connectgroup.Layout.Add( connectbutton );
				canvas.Layout.Add( connectgroup );


				break;

			case PickaxeClient.ServerStatusCodes.Connected:
				var disconnectbutton = new Button( "Disconnect" );
				disconnectbutton.Clicked += () =>
				{
					PickaxeClient.DisconnectFromServer();
				};
				canvas.Layout.Add( disconnectbutton );
				canvas.Layout.AddStretchCell();
				break;
			case PickaxeClient.ServerStatusCodes.Started:
				var stopserver = new Button( "Stop Server" );
				stopserver.Clicked += () =>
				{
					PickaxeClient.StopServer();
				};
				canvas.Layout.Add( stopserver );


				var clientlist = new Widget( null );
				clientlist.SetLayout( LayoutMode.TopToBottom );
				clientlist.Layout.Spacing = 8;
				clientlist.Layout.Margin = 8;
				var ConnectedClientsLabel = new Label( "Connected Clients:" );
				clientlist.Layout.Add( ConnectedClientsLabel );
				foreach ( var client in PickaxeClient.Players )
				{
					var Player = client.Value;
					var clientwidget = new Widget( null );
					clientwidget.SetLayout( LayoutMode.LeftToRight );
					clientwidget.Layout.Spacing = 8;
					clientwidget.Layout.Margin = 8;
					var clientname = new Label( client.Key + " : " + Player.Name );
					clientwidget.Layout.Add( clientname );
					clientlist.Layout.Add( clientwidget );
				}
				clientlist.Layout.AddStretchCell();
				canvas.Layout.Add( clientlist );
				canvas.Layout.AddStretchCell();
				break;
		}

		var testbutton = new Button( "Test" );
		testbutton.Clicked += () =>
		{
			Log.Info( "Test Button Clicked" );
			PickaxeClient.TestSendEntity();
		};
		canvas.Layout.Add( testbutton );


	}

}
