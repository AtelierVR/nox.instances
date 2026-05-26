using Nox.CCK.Utils;
using ISearchRequest = Nox.Instances.ISearchRequest;

namespace Nox.CCK.Instances {
	public class SearchRequest : ISearchRequest, INoxObject {
		public string Server { get; set; } = Identifier.LOCAL_SERVER;

		public string Query { get; set; } = null;

		public Identifier World { get; set; } = Identifier.Invalid;

		public Identifier Owner { get; set; } = Identifier.Invalid;

		public uint Offset { get; set; } = 0;

		public uint Limit { get; set; } = 0;

		public override string ToString() {
			var text = "";
			if (!string.IsNullOrEmpty(Server) && Server != Identifier.LOCAL_SERVER)
				text += (text.Length > 0 ? "&" : "") + $"server={Server}";
			if (!string.IsNullOrEmpty(Query))
				text += (text.Length > 0 ? "&" : "") + $"query={Query}";
			if (World.IsValid())
				text += (text.Length > 0 ? "&" : "") + $"world={World.ToShortString()}";
			if (Owner.IsValid())
				text += (text.Length > 0 ? "&" : "") + $"owner={Owner.ToShortString()}";
			if (Offset > 0)
				text += (text.Length > 0 ? "&" : "") + $"offset={Offset}";
			if (Limit > 0)
				text += (text.Length > 0 ? "&" : "") + $"limit={Limit}";
			return string.IsNullOrEmpty(text) ? "" : "?" + text;
		}

		public static SearchRequest From(ISearchRequest identifier)
			=> new() {
				Server = identifier.Server,
				Query  = identifier.Query,
				World  = identifier.World,
				Owner  = identifier.Owner,
				Offset = identifier.Offset,
				Limit  = identifier.Limit
			};
	}
}