using System;
using Autofac;
using Clio.Command;
using Clio.Common;
using Clio.Tests.Infrastructure;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using IFileSystem = System.IO.Abstractions.IFileSystem;

namespace Clio.Tests.Command;

[Author("Kirill Krylov", "k.krylov@creatio.com")]
[Category("UnitTests")]
[TestFixture]
public class RequiresGateCommandTests
{
	
	#region Setup/Teardown

	[OneTimeSetUp]
	public void OneTimeSetUp(){
		BindingModule = new BindingsModule(_fileSystem);
		Container = BindingModule.Register(EnvironmentSettings, false, _customRegistrations);
		
	}
	
	#endregion

	#region Fields: Private

	private static readonly IApplicationClient ApplicationClientMock = Substitute.For<IApplicationClient>();

	private readonly Action<ContainerBuilder> _customRegistrations = cb => {
		cb.RegisterInstance(ApplicationClientMock).As<IApplicationClient>();
		cb.RegisterInstance(EnvironmentSettings).As<EnvironmentSettings>();
	};

	private readonly IFileSystem _fileSystem = TestFileSystem.MockExamplesFolder("deployments-manifest");
	

	#endregion

	#region Properties: Private

	private static EnvironmentSettings EnvironmentSettings => new() {
			Uri = "http://localhost",
			Login = "Supervisor",
			Password = "Supervisor",
			IsNetCore = true
			
		};

	private BindingsModule BindingModule { get; set; }
	private IContainer Container {get;set;}

	#endregion

	[Test]
	public void TestCommand(){
		//Arrange
		PingAppOptions options = new() {
			Uri = EnvironmentSettings.Uri,
			IsNetCore = EnvironmentSettings.IsNetCore
		};

		ApplicationClientMock.ExecutePostRequest(Arg.Any<string>(), Arg.Any<string>())
			.Returns("OK THIS IS A POST RESPONSE");
		ApplicationClientMock
			.ExecuteGetRequest(Arg.Is<string>(s => s.StartsWith(EnvironmentSettings.Uri)))
			.Returns("OK THIS IS A GET RESPONSE");

		PingAppCommand pingCommand = Container.Resolve<PingAppCommand>();
		int result = pingCommand.Execute(options);
		result.Should().Be(0);
	}
}