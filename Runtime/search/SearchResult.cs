using System;
using System.Collections.Generic;
using System.Linq;
using Nox.Instances.Runtime.Networks;
using Nox.Instances.Runtime.search;
using Nox.Search;
using Nox.Worlds;

namespace Nox.Instances.Runtime.Search {
	public class SearchResult : IResult {
		public string Error { get; internal set; }
		public SearchResponse Response;
		public Dictionary<uint, IWorld> Worlds = new();
		public string ServerAddress;
		public int MenuId;

		public bool IsError
			=> !string.IsNullOrEmpty(Error);

		public bool HasNext()
			=> !IsError && Response.HasNext();

		public IResultData[] Data
			=> Response?.Items != null
				? Response.Items
					.Select(x => new SearchData {
						Reference = x,
						World     = GetWorld(x),
					})
					.Cast<IResultData>()
					.ToArray()
				: Array.Empty<IResultData>();

		private IWorld GetWorld(Instance instance) {
			if (instance == null || !instance.World.IsValid())
				return null;
			return Worlds.TryGetValue(instance.World.NumericId, out var world)
				? world
				: null;
		}
	}
}