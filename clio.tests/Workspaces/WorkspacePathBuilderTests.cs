using System;
using Autofac;
using Clio.Tests.Infrastructure;
using NUnit.Framework;
using IFileSystem = System.IO.Abstractions.IFileSystem;

namespace Clio.Tests.Workspaces;

[Author("Kirill Krylov", "k.krylov@creatio.com")]
[Category("UnitTests")]
[TestFixture]
public class WorkspacePathBuilderTests
{

	#region Fields: Private

	private readonly Action<ContainerBuilder> _customRegistrations = cb => {
		cb.RegisterInstance(EnvironmentSettings).As<EnvironmentSettings>();
	};

	private readonly IFileSystem _fileSystem = TestFileSystem.MockExamplesFolder("deployments-manifest");

	#endregion

	#region Properties: Private

	private static EnvironmentSettings EnvironmentSettings =>
		new() {
			Uri = "http://localhost",
			Login = "Supervisor",
			Password = "Supervisor",
			IsNetCore = true
		};

	private BindingsModule BindingModule { get; set; }

	private IContainer Container { get; set; }

	#endregion

	#region Methods: Public

	[OneTimeSetUp]
	public void OneTimeSetUp(){
		BindingModule = new BindingsModule(_fileSystem);
		Container = BindingModule.Register(EnvironmentSettings, false, _customRegistrations);
	}

	#endregion

}