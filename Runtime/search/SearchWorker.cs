using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using Nox.CCK.Instances;
using Nox.Search;
using Nox.Worlds;
using Logger = Nox.CCK.Utils.Logger;

namespace Nox.Instances.Runtime.Search {
	public class SearchWorker : IWorker {
		public string Title;
		public string Server;

		public string[] TitleArguments
			=> new[] { Title };

		public float Ratio
			=> 4f / 3f;

		public async UniTask<IResult> Fetch(IFetchOptions options) {
			if (string.IsNullOrEmpty(Server))
				return new SearchResult { Error = "Invalid server address." };
			var data = await Main.Instance.Network.Search(
				new SearchRequest {
					Server = Server,
					Query  = options.Query,
					Offset = options.Page * options.Limit,
					Limit  = options.Limit,
				}
			);
			if (data == null) return new SearchResult { Error = "Error fetching instances." };
			return new SearchResult {
				Response      = data,
				Worlds        = await FetchWorlds(data.Items),
				ServerAddress = Server,
				Error         = null
			};
		}

		/// <summary>
		/// Fetches the worlds referenced by the given instances so an instance missing a
		/// title or a thumbnail can fall back on the ones of its world.
		/// Only the worlds actually needed are fetched, once each.
		/// </summary>
		private static async UniTask<Dictionary<uint, IWorld>> FetchWorlds(Instance[] instances) {
			var worlds = new Dictionary<uint, IWorld>();
			var api    = Main.WorldAPI;
			if (api == null || instances == null)
				return worlds;

			var identifiers = instances
				.Where(i => i != null && (string.IsNullOrEmpty(i.Title) || string.IsNullOrEmpty(i.Thumbnail)))
				.Select(i => i.World)
				.Where(w => w.IsValid())
				.GroupBy(w => w.NumericId)
				.Select(g => g.First())
				.ToArray();

			await UniTask.WhenAll(
				identifiers.Select(
					async identifier => {
						try {
							var world = await api.Fetch(identifier);
							if (world != null)
								worlds[identifier.NumericId] = world;
						} catch (Exception e) {
							Logger.LogError($"Failed to fetch world {identifier} for instance search: {e.Message}");
						}
					}
				)
			);

			return worlds;
		}
	}
}