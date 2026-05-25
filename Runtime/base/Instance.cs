using System;
using System.Linq;
using Newtonsoft.Json;
using Nox.CCK.Convertors;
using Nox.CCK.Utils;
using Nox.Instances;

namespace api.nox.instance {
	[Serializable]
	public class Instance : IInstance, INoxObject {

		[JsonProperty("id")]
		public uint Id { get; private set; }

		[JsonProperty("server")]
		public string Server { get; private set; }

		[JsonProperty("name")]
		public string Name { get; private set; }

		[JsonProperty("title")]
		public string Title { get; private set; }

		[JsonProperty("description")]
		public string Description { get; private set; }

		[JsonProperty("thumbnail")]
		public string Thumbnail { get; private set; }

		[JsonProperty("owner"), JsonConverter(typeof(StringToIdentifierConverter))]
		public Identifier Owner { get; private set; }

		[JsonProperty("world"), JsonConverter(typeof(StringToIdentifierConverter))]
		public Identifier World { get; private set; }

		[JsonProperty("tags")]
		public string[] Tags { get; private set; }

		[JsonProperty("connection")]
		public Connection Connection { get; private set; }

		IConnection IInstance.Connection 
			=> Connection;

		[JsonProperty("client_count")]
		public ushort ClientCount { get; private set; }

		[JsonProperty("players")]
		public InstancePlayer[] Players { get; private set; }

		IInstancePlayer[] IInstance.Players
			=> Players.ToArray<IInstancePlayer>();

		[JsonProperty("capacity")]
		public ushort Capacity { get; private set; }
		
		public Identifier Identifier
			=> new("i", Id, null, Server);
	}
}