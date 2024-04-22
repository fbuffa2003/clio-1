using System;

namespace Clio.Utilities
{

	#region Interface: IOSPlatformChecker

	public interface IOSPlatformChecker
	{

		#region Methods: Public

		bool IsWindowsEnvironment { get; }

		#endregion

	}

	#endregion

	#region Class: OSPlatformChecker

	public class OSPlatformChecker : IOSPlatformChecker
	{


		#region Properties: Public

		public bool IsWindowsEnvironment => GetIsWindowsEnvironment();

		#endregion

		#region Methods: Public

		public static bool GetIsWindowsEnvironment() {
			switch (Environment.OSVersion.Platform) {
				case PlatformID.Win32NT:
				case PlatformID.Win32S:
				case PlatformID.Win32Windows:
				case PlatformID.WinCE:
					return true;
				default:
					return false;
			}
		}
		public static bool GetIsMacOSEnvironment() {
			switch (Environment.OSVersion.Platform) {
				case PlatformID.MacOSX:
					return true;
				default:
					return false;
			}
		}
		public static bool GetIsLinuxEnvironment() {
			switch (Environment.OSVersion.Platform) {
				case PlatformID.Unix:
					return true;
				default:
					return false;
			}
		}

		#endregion

	}

	#endregion

}