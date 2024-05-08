using System.IO;
using Autofac;
using Clio.Common;
using Clio.Package;
using Clio.Tests.Infrastructure;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using IFileSystem = System.IO.Abstractions.IFileSystem;

namespace Clio.Tests.Package;

[Author("Kirill Krylov", "k.krylov@creatio.com")]
[Category("UnitTests")]
[TestFixture]
internal class ApplicationPackageListProviderCommandTests
{

	#region Setup/Teardown

	[SetUp]
	public void Setup(){
		BindingsModule bindingModule = new(_fileSystem);
		
		var envSettings = new EnvironmentSettings() {
			Uri = "http://localhost",
			Login = "Supervisor",
			Password = "Supervisor",
		};
		_container = bindingModule.Register(envSettings);
	}

	#endregion

	#region Fields: Private

	private readonly IFileSystem _fileSystem = TestFileSystem.MockExamplesFolder("deployments-manifest");
	private IContainer _container;

	#endregion

	[TestCase("ServiceModel-GetPackages.json", true)]
	[TestCase("ServiceModel-GetPackages-noGate.json", false)]
	public void GetIsGateInstalled_ReturnsTrue_WhenGateInstalled(string fileName, bool expected){
		//Arrange
		IJsonConverter converter = _container.Resolve<IJsonConverter>();
		IServiceUrlBuilder serviceUrlBuilder = _container.Resolve<IServiceUrlBuilder>();
		IApplicationClient applicationClientMock = Substitute.For<IApplicationClient>();

		string jsonContent = File.ReadAllText($"Examples/Misc-Json/{fileName}");
		applicationClientMock.ExecutePostRequest(
				url:Arg.Any<string>(), 
				requestData:Arg.Any<string>())
			.Returns(jsonContent);
		
		ApplicationPackageListProvider sut = new(applicationClientMock, converter, serviceUrlBuilder);

		//Act
		bool isGateInstalled = sut.GetIsClioGateInstalled();

		//Assert
		isGateInstalled.Should().Be(expected);
	}
}