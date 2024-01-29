using Godot;
using System;

public partial class Server : Node
{

	private ENetMultiplayerPeer peer = new ENetMultiplayerPeer();

	[Export]
	public PackedScene PlayerScene {get;set;}
	private bool serverIsReady;
	[Export]
	public NodePath SpawnLocation;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if(serverIsReady) peer.Poll();
	}

	private void _on_host_button_down(){
		var error = peer.CreateServer(8910);
		if(error != Error.Ok){
			GD.Print("server has failed to start " + error);
		}

		Multiplayer.MultiplayerPeer = peer;

		Node player = PlayerScene.Instantiate();
		GetNode<Node>(SpawnLocation).AddChild(player);
		player.Name = "1";
		player.GetNode<AudioManager>("AudioManager").SetupAudio(1);
		serverIsReady = true;
	}
}
