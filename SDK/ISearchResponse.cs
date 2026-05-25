using Cysharp.Threading.Tasks;
using Nox.CCK.Utils;

namespace Nox.Instances {
	/// <summary>
	/// Represents the response from a search query for instances.
	/// </summary>
	public interface ISearchResponse {
		
		/// <summary>
		/// The original query string that was used to perform the search.
		/// </summary>
		public string Query { get; }

		/// <summary>
		/// The identifier of the owner of the instances in the search results.
		/// </summary>
		public Identifier Owner { get; }
		
		
		public Identifier World { get; }

		public IInstance[] Items { get; }

		public uint Total { get; }

		public uint Limit { get; }

		public uint Offset { get; }

		public bool HasNext();

		public bool HasPrevious();

		public UniTask<ISearchResponse> Next();

		public UniTask<ISearchResponse> Previous();
	}
}