using System;
using Newtonsoft.Json;
using Nox.CCK.Convertors;
using Nox.CCK.Utils;
using Nox.Instances;
using Nox.Users;

namespace api.nox.instance {
	[Serializable]
	public class InstancePlayer : IInstancePlayer {
		[JsonProperty("name"), JsonConverter(typeof(StringToIdentifierConverter))]
		public Identifier Identifier { get; private set; }

		[JsonProperty("display")]
		public string Display { get; private set; }
	}
}