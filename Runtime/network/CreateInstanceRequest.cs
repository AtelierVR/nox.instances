using Newtonsoft.Json.Linq;
using Nox.CCK.Utils;

namespace Nox.Instances.Runtime.Networks {
	/// <summary>
	/// Request body sent to <c>PUT /instances</c> to create a new instance.
	/// </summary>
	public struct CreateInstanceRequest : INoxObject {
		/// <summary>
		/// World identifier (NoxIdentifier format), required.
		/// </summary>
		public string World;

		/// <summary>
		/// Maximum number of players (0 = world default, ushort.MaxValue = unlimited).
		/// </summary>
		public ushort Capacity;

		/// <summary>
		/// Short unique slug name ([a-z0-9-_.]{3,8}), optional.
		/// </summary>
		public string Name;

		/// <summary>
		/// Human readable title, optional.
		/// </summary>
		public string Title;

		/// <summary>
		/// Description, optional.
		/// </summary>
		public string Description;

		/// <summary>
		/// Tags, optional.
		/// </summary>
		public string[] Tags;

		public JObject ToJson() {
			var obj = new JObject {
				["world"]    = World,
				["capacity"] = Capacity
			};

			if (!string.IsNullOrEmpty(Name))
				obj["name"] = Name;

			if (!string.IsNullOrEmpty(Title))
				obj["title"] = Title;

			if (!string.IsNullOrEmpty(Description))
				obj["description"] = Description;

			if (Tags is { Length: > 0 })
				obj["tags"] = JArray.FromObject(Tags);

			return obj;
		}
	}
}
