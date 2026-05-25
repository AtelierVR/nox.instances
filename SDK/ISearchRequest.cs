using Nox.CCK.Utils;

namespace Nox.Instances {
	/// <summary>
	/// Object 
	/// </summary>
	public interface ISearchRequest {
		public string Server { get; set; }
		
		public string Query { get; set; }
		
		public Identifier Owner { get; set; }
		
		public Identifier World { get; set; }
		
		public uint Offset { get; set; }
		
		public uint Limit { get; set; }
		
		
	}
}