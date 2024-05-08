using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Autofac;
using Clio.Command;
using Clio.Common;
using Clio.Package;
using Clio.Tests.Infrastructure;
using CreatioModel;
using FluentAssertions;
using NSubstitute;
using NUnit.Framework;
using IFileSystem = System.IO.Abstractions.IFileSystem;

namespace Clio.Tests;

[TestFixture]
internal class EnvironmentManagerTest
{

	#region Setup/Teardown

	[SetUp]
	public void Setup(){
		BindingsModule bindingModule = new BindingsModule(_fileSystem);
		_container = bindingModule.Register();
	}

	#endregion

	#region Fields: Private

	private readonly IFileSystem _fileSystem = TestFileSystem.MockExamplesFolder("deployments-manifest");
	private IContainer _container;

	#endregion

	#region Methods: Public

	[TestCase("easy-creatio-config.yaml", "CrtCustomer360",
		"//tscrm.com/dfs-ts/MyAppHub/CrtCustomer360/1.0.1/CrtCustomer360_1.0.1.zip")]
	[TestCase("easy-creatio-config.yaml", "CrtCaseManagment",
		"//tscrm.com/dfs-ts/MyAppHub/CrtCaseManagment/1.0.2/CrtCaseManagment_1.0.2.zip")]
	public void FindAppHubPath_In_FromManifest(string manifestFileName, string appName, string path){
		string resultPath = path.Replace('/', Path.DirectorySeparatorChar);
		IEnvironmentManager environmentManager = _container.Resolve<IEnvironmentManager>();
		string manifestFilePath = $"C:\\{manifestFileName}";
		SysInstalledApp app = environmentManager.FindApplicationsInAppHub(manifestFilePath)
			.Where(s => s.Name == appName).FirstOrDefault();
		Assert.AreEqual(resultPath, app.ZipFileName);
	}

	[TestCase("easy-creatio-config.yaml")]
	public void FindApplicationsFromManifest_In_AppHub(string manifestFileName){
		IEnvironmentManager environmentManager = _container.Resolve<IEnvironmentManager>();
		string manifestFilePath = $"C:\\{manifestFileName}";
		List<SysInstalledApp> applicationsFromAppHub = environmentManager.FindApplicationsInAppHub(manifestFilePath);
		Assert.AreEqual(2, applicationsFromAppHub.Count);
	}

	[TestCase("easy-creatio-config.yaml", 3)]
	[TestCase("full-creatio-config.yaml", 2)]
	public void GetApplicationsFrommanifest_if_applicationExists(string fileName, int appCount){
		IEnvironmentManager environmentManager = _container.Resolve<IEnvironmentManager>();
		string manifestFilePath = $"C:\\{fileName}";
		List<SysInstalledApp> applications = environmentManager.GetApplicationsFromManifest(manifestFilePath);
		Assert.AreEqual(appCount, applications.Count);
	}

	[TestCase(0, "CrtCustomer360", "1.0.1", "easy-creatio-config.yaml")]
	[TestCase(1, "CrtCaseManagment", "1.0.2", "easy-creatio-config.yaml")]
	[TestCase(0, "CrtCustomer360", "1.0.1", "full-creatio-config.yaml")]
	[TestCase(1, "CrtCaseManagment", "1.0.2", "full-creatio-config.yaml")]
	public void GetApplicationsFrommanifest_if_applicationExists(int appIndex, string appName, string appVersion,
		string manifestFileName){
		IEnvironmentManager environmentManager = _container.Resolve<IEnvironmentManager>();
		string manifestFilePath = $"C:\\{manifestFileName}";
		List<SysInstalledApp> applications = environmentManager.GetApplicationsFromManifest(manifestFilePath);
		Assert.AreEqual(appName, applications[appIndex].Name);
		Assert.AreEqual(appVersion, applications[appIndex].Version);
	}

	[TestCase("easy-creatio-config.yaml", "https://preprod.creatio.com",
		"https://preprod.creatio.com/0/ServiceModel/AuthService.svc/Login")]
	[TestCase("full-creatio-config.yaml", "https://production.creatio.com",
		"https://production.creatio.com/0/ServiceModel/AuthService.svc/Login")]
	public void GetEnvironmentUrl_FromManifest(string manifestFileName, string url, string authAppUrl){
		IEnvironmentManager environmentManager = _container.Resolve<IEnvironmentManager>();
		string manifestFilePath = $"C:\\{manifestFileName}";
		EnvironmentSettings env = environmentManager.GetEnvironmentFromManifest(manifestFilePath);
		Assert.AreEqual(url, env.Uri);
		Assert.AreEqual(authAppUrl, env.AuthAppUri);
	}

	[TestCase("setting-creatio-config.yaml", 7)]
	public void GetSettingsFromManifest(string manifestFileName, int count){
		//Arrange
		IEnvironmentManager environmentManager = _container.Resolve<IEnvironmentManager>();
		string manifestFilePath = $"C:\\{manifestFileName}";

		//Act
		IEnumerable<CreatioManifestSetting> settings = environmentManager.GetSettingsFromManifest(manifestFilePath);
		//Assert
		settings.Count().Should().Be(count);

		List<CreatioManifestSetting> expected = [
			new CreatioManifestSetting {
				Code = "IntSysSettingsATF",
				Value = "10"
			},
			new CreatioManifestSetting {
				Code = "FloatSysSettingsATF",
				Value = "0.5"
			},

			new CreatioManifestSetting {
				Code = "StringSettingsATF",
				Value = "ATF"
			},

			new CreatioManifestSetting {
				Code = "DateTimeSettingsATF",
				Value = "2021-01-01T00:00:00"
			},

			new CreatioManifestSetting {
				Code = "GuidSettingsATF",
				Value = "00000000-0000-0000-0000-000000000001"
			},

			new CreatioManifestSetting {
				Code = "LookupSettingsATF",
				Value = "TextLookupValue"
			},
			new CreatioManifestSetting {
				Code = "BooleanSettingsATF",
				Value = "false",
				UserValues = new Dictionary<string, string> {
					{"Supervisor", "true"},
					{"System administrators", "false"},
					{"Developer", "true"},
					{"2nd-line support", "true"}
				}
			}
		];
		settings.Should().BeEquivalentTo(expected);
	}

	[TestCase("setting-creatio-config-broken.yaml", 7)]
	public void GetSettingsFromManifest_Throws_When_YAML_CodeNull(string manifestFileName, int count){
		//Arrange
		IEnvironmentManager environmentManager = _container.Resolve<IEnvironmentManager>();
		string manifestFilePath = $"C:\\{manifestFileName}";

		//Act + Assert
		Action act = () => environmentManager.GetSettingsFromManifest(manifestFilePath);
		act.Should().Throw<Exception>("null values should throw")
			.WithMessage("Setting code cannot be null or empty. Found invalid values on lines *");
	}

	[TestCase("setting-creatio-config-broken.yaml", 7)]
	public void GetSettingsFromManifest_Throws_When_YAML_ValueNull(string manifestFileName, int count){
		//Arrange
		IEnvironmentManager environmentManager = _container.Resolve<IEnvironmentManager>();
		string manifestFilePath = $"C:\\{manifestFileName}";

		//Act + Assert
		Action act = () => environmentManager.GetSettingsFromManifest(manifestFilePath);
		act.Should().Throw<Exception>("null values should throw")
			.WithMessage("*Setting value cannot be null for: [IntSysSettingsATF]");
	}

	[TestCase("web-services-creatio.yaml", 2)]
	public void GetWebServicesFromManifest(string manifestFileName, int count){
		//Arrange
		IEnvironmentManager environmentManager = _container.Resolve<IEnvironmentManager>();
		string manifestFilePath = $"C:\\{manifestFileName}";

		//Act
		IEnumerable<CreatioManifestWebService> webservices
			= environmentManager.GetWebServicesFromManifest(manifestFilePath);
		//Assert
		webservices.Count().Should().Be(count);
		List<CreatioManifestWebService> expected = [
			new CreatioManifestWebService {
				Name = "WebService1",
				Url = "https://preprod.creatio.com/0/ServiceModel/EntityDataService.svc"
			},
			new CreatioManifestWebService {
				Name = "WebService2",
				Url = "https://preprod.creatio.com/0/ServiceModel/EntityDataService.svc"
			}
		];
		webservices.Should().BeEquivalentTo(expected);
	}

	[TestCase("sections-without-items-creatio.yaml")]
	public void GetWebServicesFromManifest_WhenExistsSectionButNotExistsItems(string manifestFileName){
		//Arrange
		IEnvironmentManager environmentManager = _container.Resolve<IEnvironmentManager>();
		string manifestFilePath = $"C:\\{manifestFileName}";

		//Act
		IEnumerable<CreatioManifestWebService> webservices
			= environmentManager.GetWebServicesFromManifest(manifestFilePath);
		IEnumerable<Feature> features = environmentManager.GetFeaturesFromManifest(manifestFilePath);
		IEnumerable<CreatioManifestSetting> settings = environmentManager.GetSettingsFromManifest(manifestFilePath);
		//Assert
		webservices.Count().Should().Be(0);
		features.Count().Should().Be(0);
		settings.Count().Should().Be(0);
	}

	[TestCase("feature-creatio-config.yaml", 3)]
	public void ParsesYamlAndReturnsStructure(string manifestFileName, int count){
		IEnvironmentManager environmentManager = _container.Resolve<IEnvironmentManager>();
		string manifestFilePath = $"C:\\{manifestFileName}";
		IEnumerable<Feature> features = environmentManager.GetFeaturesFromManifest(manifestFilePath);
		features.Count().Should().Be(count);

		List<Feature> expected = [
			new Feature {
				Code = "Feature1",
				Value = true
			},

			new Feature {
				Code = "Feature2",
				Value = false,
				UserValues = new Dictionary<string, bool> {
					{"Supervisor", true},
					{"System administrators", false},
					{"Developer", true},
					{"2nd-line support", true}
				}
			},

			new Feature {
				Code = "Feature3",
				Value = false
			}
		];
		features.Should().BeEquivalentTo(expected);
	}

	

	
	#endregion

}