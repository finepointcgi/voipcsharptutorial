using Godot;
using System;
using System.IO;
using System.IO.Compression;

public partial class AudioManager : Node
{

	private AudioStreamPlayer input;
	private int index;
	private AudioEffectCapture effect;
	private AudioStreamGeneratorPlayback playback;
	[Export]
	public NodePath AudioOutputPath {get; set;}
	[Export]
	public float InputThreashold = 0.005f;
	private Godot.Collections.Array<float> receiveBuffer = new Godot.Collections.Array<float>();

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		SetupAudio(1);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if(IsMultiplayerAuthority())
			processMic();
		processVoice();
	}

	public void SetupAudio(long id){
		input = GetNode<AudioStreamPlayer>("Input");
		
		SetMultiplayerAuthority(Convert.ToInt32(id));

		if(IsMultiplayerAuthority()){
			input.Stream = new AudioStreamMicrophone();
			input.Play();
			index = AudioServer.GetBusIndex("Record");
			effect = (AudioEffectCapture)AudioServer.GetBusEffect(index, 0);
		}
		
		AudioStreamPlayback player = GetNode<AudioStreamPlayer2D>(AudioOutputPath).GetStreamPlayback();
		playback = player as AudioStreamGeneratorPlayback;
		
	}

	private void processMic(){
		var sterioData = effect.GetBuffer(effect.GetFramesAvailable());

		if(sterioData.Length > 0){
			var data = new float[sterioData.Length];

			float maxAmplitude = 0.0f;
			for(int i = 0 ; i < sterioData.Length; i++){
				var value = (sterioData[i].X + sterioData[i].Y) / 2;
				maxAmplitude = Math.Max(value, maxAmplitude);
				data[i] = value;

			}

			if(maxAmplitude < InputThreashold){
				return;
			}
			//sendData(data);
			Rpc("sendData" , CompressFloatArray(data));
		}
	}

	private void processVoice(){
		if(receiveBuffer.Count <= 0) return;

		for(int i = 0; i < Math.Min(playback.GetFramesAvailable(), receiveBuffer.Count); i++){
			playback.PushFrame(new Vector2(receiveBuffer[0], receiveBuffer[0]));
			receiveBuffer.RemoveAt(0);
		}
	}

	[Rpc(mode: MultiplayerApi.RpcMode.AnyPeer, CallLocal = false, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
	private void sendData(byte[] data){
		
		receiveBuffer.AddRange(DecompressFloatArray(data));
	}

	public byte[] CompressFloatArray(float[] floatArray){
		byte[] byteArray = new byte[floatArray.Length * 4];
		Buffer.BlockCopy(floatArray, 0, byteArray, 0, byteArray.Length);

		using(var memoryStream = new MemoryStream()){
			using(var gZipStream = new GZipStream(memoryStream, CompressionMode.Compress)){
				gZipStream.Write(byteArray, 0, byteArray.Length);
			}
			return memoryStream.ToArray();
		}
	}

	public float[] DecompressFloatArray(byte[] compressedArray){
		using(var memoryStream = new MemoryStream(compressedArray))
		using(var gZipStream = new GZipStream(memoryStream, CompressionMode.Decompress))
		using(var resultStream = new MemoryStream()){
			gZipStream.CopyTo(resultStream);
			byte[] 	byteArray = resultStream.ToArray();
			float[] floatArray = new float[byteArray.Length / 4];
			Buffer.BlockCopy(byteArray, 0, floatArray, 0, byteArray.Length);
			return floatArray;	
		}
	}
}
