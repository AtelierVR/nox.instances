using System;
using System.Linq;
using api.nox.instance.network;
using Nox.Search;

namespace api.nox.instance.search {
	public class SearchResult : IResult {
		public string Error { get; internal set; }
		public SearchResponse Response;
		public string ServerAddress;
		public int MenuId;

		public bool IsError
			=> !string.IsNullOrEmpty(Error);

		public bool HasNext()
			=> !IsError && Response.HasNext();

		public IResultData[] Data
			=> Response != null
				? Response.Items
					.Select(x => new SearchData { Reference = x })
					.Cast<IResultData>()
					.ToArray()
				: Array.Empty<IResultData>();
	}
}