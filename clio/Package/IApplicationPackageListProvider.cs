namespace Clio.Package
{
	using System.Collections.Generic;

	#region Interface: IApplicationPackageListProvider
	
	public interface IApplicationPackageListProvider
	{

		#region Methods: Public

		IEnumerable<PackageInfo> GetPackages();
		IEnumerable<PackageInfo> GetPackages(string scriptData);
		
		/// <summary>
		/// Determines if cliogate is installed
		/// </summary>
		/// <returns><c>true</c> if cliogate is installed, <c>false</c> otherwise</returns>
		bool GetIsClioGateInstalled();

		#endregion

	}

	#endregion

}