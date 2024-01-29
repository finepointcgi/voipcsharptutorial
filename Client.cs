using Godot;
using System;

public partial class Client : Node
{
	private ENetMultiplayerPeer peer = new ENetMultiplayerPeer();

	[Export]
	public PackedScene PlayerScene {get;set;}
	private bool clientConnected;
	[Export]
	public NodePath SpawnLocation;
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Multiplayer.PeerConnected += peerConnected;
		Multiplayer.PeerDisconnected += peerDisconnected;

	}

    private void peerDisconnected(long id)
    {
        GD.Print("player disconnected!" + id.ToString());
    }


    private void peerConnected(long id)
    {
        GD.Print("player connected!" + id.ToString());

		Node player = PlayerScene.Instantiate();
		GetNode<Node>(SpawnLocation).AddChild(player);
		player.Name = id.ToString();
		player.GetNode<AudioManager>("AudioManager").SetupAudio(id);
		//clientConnected = true;
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
	{
		if(clientConnected) peer.Poll();
	}

	public void _on_connect_button_down(){
		peer.CreateClient("127.0.0.1", 8910);
		Multiplayer.MultiplayerPeer = peer;

		Node player = PlayerScene.Instantiate();
		GetNode<Node>(SpawnLocation).AddChild(player);
		player.Name = Multiplayer.GetUniqueId().ToString();
		player.GetNode<AudioManager>("AudioManager").SetupAudio(Multiplayer.GetUniqueId());
		clientConnected = true;
	}
}
