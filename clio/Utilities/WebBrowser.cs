using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Text;

namespace Clio.Utilities
{
	static class WebBrowser
	{
		public static bool Enabled => OSPlatformChecker.GetIsWindowsEnvironment();

		public static bool CheckUrl(string url) {
			UriBuilder uriBuilder = new UriBuilder(url);
			var request = HttpWebRequest.Create(uriBuilder.Uri);
			var response = (HttpWebResponse)request.GetResponse();
			return response.StatusCode == HttpStatusCode.OK && response.ResponseUri == request.RequestUri;
		}

		public static void OpenUrl(string url) {
			if (OSPlatformChecker.GetIsWindowsEnvironment()) {
				Console.WriteLine($"Open {url}...");
				Process.Start(new ProcessStartInfo("cmd", $"/c start {url}") { CreateNoWindow = true });
			} 
			else if(OSPlatformChecker.GetIsMacOSEnvironment()) {
				Console.WriteLine($"Open {url}...");
				Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
			} 
			else if(OSPlatformChecker.GetIsLinuxEnvironment()) {
				Console.WriteLine($"Open {url}...");
				Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
			}
			else {
				throw new NotFiniteNumberException("Command not supported for current platform...");
			}
		}
	}
}
