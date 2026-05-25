using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using Newtonsoft.Json;
using Nox.CCK.Convertors;
using Nox.CCK.Instances;
using Nox.CCK.Utils;
using Nox.Instances;
using ISearchResponse = Nox.Instances.ISearchResponse;

namespace api.nox.instance.network {
	[Serializable]
	public class SearchResponse : ISearchResponse, INoxObject {
		internal SearchRequest Request;

		[JsonProperty("query")]
		public string Query { get; private set; }

		[JsonProperty("owner"), JsonConverter(typeof(StringToIdentifierConverter))]
		public Identifier Owner { get; private set; }

		[JsonProperty("world"), JsonConverter(typeof(StringToIdentifierConverter))]
		public Identifier World { get; private set; }

		[JsonProperty("items")]
		public Instance[] Items { get; private set; }

		IInstance[] ISearchResponse.Items
			=> Items.ToArray<IInstance>();

		[JsonProperty("total")]
		public uint Total { get; private set; }

		[JsonProperty("limit")]
		public uint Limit { get; private set; }

		[JsonProperty("offset")]
		public uint Offset { get; private set; }

		public bool HasNext()
			=> Offset + Limit < Total;

		public bool HasPrevious()
			=> Offset > 0;

		async UniTask<ISearchResponse> ISearchResponse.Next()
			=> await Next();

		async UniTask<ISearchResponse> ISearchResponse.Previous()
			=> await Previous();

		public async UniTask<SearchResponse> Next()
			=> HasNext()
				? await Main.Instance.Network.Search(
					new SearchRequest {
						Server = Request.Server,
						Query  = Request.Query,
						World  = Request.World,
						Owner  = Request.Owner,
						Offset = Offset + Limit,
						Limit  = Limit
					}
				)
				: null;

		public async UniTask<SearchResponse> Previous()
			=> HasPrevious()
				? await Main.Instance.Network.Search(
					new SearchRequest {
						Server = Request.Server,
						Query  = Request.Query,
						World  = Request.World,
						Owner  = Request.Owner,
						Offset = Offset - Limit,
						Limit  = Limit
					}
				)
				: null;
	}
}