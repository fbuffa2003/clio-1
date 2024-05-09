using System;
using System.Reflection;
using ATF.Repository.Providers;
using Autofac;
using Clio.Command;
using Clio.Command.ApplicationCommand;
using Clio.Command.PackageCommand;
using Clio.Command.SqlScriptCommand;
using Clio.Common;
using Clio.Common.db;
using Clio.Common.K8;
using Clio.Common.ScenarioHandlers;
using Clio.Package;
using Clio.Querry;
using Clio.Requests;
using Clio.Requests.Validators;
using Clio.Utilities;
using Clio.YAML;
using Creatio.Client;
using k8s;
using MediatR;
using MediatR.Extensions.Autofac.DependencyInjection;
using MediatR.Extensions.Autofac.DependencyInjection.Builder;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;
using FileSystem = System.IO.Abstractions.FileSystem;
using IFileSystem = System.IO.Abstractions.IFileSystem;

namespace Clio;

public class BindingsModule
{

	#region Fields: Private

	private readonly IFileSystem _fileSystem;

	#endregion

	#region Constructors: Public

	public BindingsModule(IFileSystem fileSystem = null){
		_fileSystem = fileSystem;
		ContainerBuilder = new ContainerBuilder();
	}

	#endregion

	#region Properties: Public

	private ContainerBuilder ContainerBuilder {get; set;}

	#endregion

	#region Methods: Public

	public IContainer Register(EnvironmentSettings settings = null, bool registerNullSettingsForTest = false, Action<ContainerBuilder> customRegistrations = null){
		ContainerBuilder
			.RegisterAssemblyTypes(Assembly.GetExecutingAssembly())
			.AsImplementedInterfaces();
		
		if (settings != null || registerNullSettingsForTest) {
			ContainerBuilder.RegisterInstance(settings);
			if (!registerNullSettingsForTest) {
				IApplicationClient creatioClientInstance = new ApplicationClientFactory().CreateClient(settings);
				ContainerBuilder.RegisterInstance(creatioClientInstance).As<IApplicationClient>();
				IDataProvider provider = string.IsNullOrEmpty(settings.Login) switch {
					true => new RemoteDataProvider(settings.Uri, settings.AuthAppUri, settings.ClientId,
						settings.ClientSecret, settings.IsNetCore),
					false => new RemoteDataProvider(settings.Uri, settings.Login, settings.Password, settings.IsNetCore)
				};
				ContainerBuilder.RegisterInstance(provider).As<IDataProvider>();
			}
		}

		try {
			KubernetesClientConfiguration config = KubernetesClientConfiguration.BuildConfigFromConfigFile();
			IKubernetes k8Client = new Kubernetes(config);
			ContainerBuilder.RegisterInstance(k8Client).As<IKubernetes>();
			ContainerBuilder.RegisterType<k8Commands>();
			ContainerBuilder.RegisterType<InstallerCommand>();
		} catch { }

		if (_fileSystem is not null) {
			ContainerBuilder.RegisterInstance(_fileSystem).As<IFileSystem>();
		} else {
			ContainerBuilder.RegisterType<FileSystem>().As<IFileSystem>();
		}

		IDeserializer deserializer = new DeserializerBuilder()
			.WithNamingConvention(UnderscoredNamingConvention.Instance)
			.IgnoreUnmatchedProperties()
			.Build();

		ISerializer serializer = new SerializerBuilder()
			.WithNamingConvention(UnderscoredNamingConvention.Instance)
			.ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitDefaults |
				DefaultValuesHandling.OmitEmptyCollections)
			.Build();

		#region Epiremental CreatioCLient

		if (settings is not null) {
			CreatioClient creatioClient = string.IsNullOrEmpty(settings.ClientId)
				? new CreatioClient(settings.Uri, settings.Login, settings.Password, true, settings.IsNetCore)
				: CreatioClient.CreateOAuth20Client(settings.Uri, settings.AuthAppUri, settings.ClientId,
					settings.ClientSecret, settings.IsNetCore);
			IApplicationClient clientAdapter = new CreatioClientAdapter(creatioClient);
			ContainerBuilder.RegisterInstance(clientAdapter).As<IApplicationClient>();

			ContainerBuilder.RegisterType<SysSettingsManager>();
		}

		#endregion

		ContainerBuilder.RegisterInstance(deserializer).As<IDeserializer>();
		ContainerBuilder.RegisterInstance(serializer).As<ISerializer>();
		ContainerBuilder.RegisterType<FeatureCommand>();
		ContainerBuilder.RegisterType<SysSettingsCommand>();
		ContainerBuilder.RegisterType<BuildInfoCommand>();
		ContainerBuilder.RegisterType<PushPackageCommand>();
		ContainerBuilder.RegisterType<InstallApplicationCommand>();
		ContainerBuilder.RegisterType<OpenCfgCommand>();
		ContainerBuilder.RegisterType<InstallGatePkgCommand>();
		ContainerBuilder.RegisterType<PingAppCommand>();
		ContainerBuilder.RegisterType<SqlScriptCommand>();
		ContainerBuilder.RegisterType<CompressPackageCommand>();
		ContainerBuilder.RegisterType<PushNuGetPackagesCommand>();
		ContainerBuilder.RegisterType<PackNuGetPackageCommand>();
		ContainerBuilder.RegisterType<RestoreNugetPackageCommand>();
		ContainerBuilder.RegisterType<InstallNugetPackageCommand>();
		ContainerBuilder.RegisterType<SetPackageVersionCommand>();
		ContainerBuilder.RegisterType<GetPackageVersionCommand>();
		ContainerBuilder.RegisterType<CheckNugetUpdateCommand>();
		ContainerBuilder.RegisterType<DeletePackageCommand>();
		ContainerBuilder.RegisterType<GetPkgListCommand>();
		ContainerBuilder.RegisterType<RestoreWorkspaceCommand>();
		ContainerBuilder.RegisterType<CreateWorkspaceCommand>();
		ContainerBuilder.RegisterType<PushWorkspaceCommand>();
		ContainerBuilder.RegisterType<LoadPackagesToFileSystemCommand>();
		ContainerBuilder.RegisterType<LoadPackagesToDbCommand>();
		ContainerBuilder.RegisterType<UploadLicensesCommand>();
		ContainerBuilder.RegisterType<HealthCheckCommand>();
		ContainerBuilder.RegisterType<AddPackageCommand>();
		ContainerBuilder.RegisterType<UnlockPackageCommand>();
		ContainerBuilder.RegisterType<LockPackageCommand>();
		ContainerBuilder.RegisterType<DataServiceQuerry>();
		ContainerBuilder.RegisterType<RestoreFromPackageBackupCommand>();
		ContainerBuilder.RegisterType<Marketplace>();
		ContainerBuilder.RegisterType<GetMarketplacecatalogCommand>();
		ContainerBuilder.RegisterType<CreateUiProjectCommand>();
		ContainerBuilder.RegisterType<CreateUiProjectOptionsValidator>();
		ContainerBuilder.RegisterType<DownloadConfigurationCommand>();
		ContainerBuilder.RegisterType<DeployCommand>();
		ContainerBuilder.RegisterType<InfoCommand>();
		ContainerBuilder.RegisterType<ExtractPackageCommand>();
		ContainerBuilder.RegisterType<ExternalLinkCommand>();
		ContainerBuilder.RegisterType<PowerShellFactory>();
		ContainerBuilder.RegisterType<RegAppCommand>();
		ContainerBuilder.RegisterType<RestartCommand>();
		ContainerBuilder.RegisterType<SetFsmConfigCommand>();
		ContainerBuilder.RegisterType<TurnFsmCommand>();
		ContainerBuilder.RegisterType<ScenarioRunnerCommand>();
		ContainerBuilder.RegisterType<CompressAppCommand>();
		ContainerBuilder.RegisterType<Scenario>();
		ContainerBuilder.RegisterType<ConfigureWorkspaceCommand>();
		ContainerBuilder.RegisterType<CreateInfrastructureCommand>();
		ContainerBuilder.RegisterType<OpenInfrastructureCommand>();
		ContainerBuilder.RegisterType<CheckWindowsFeaturesCommand>();
		ContainerBuilder.RegisterType<ManageWindowsFeaturesCommand>();
		ContainerBuilder.RegisterType<CreateTestProjectCommand>();
		ContainerBuilder.RegisterType<ListenCommand>();
		ContainerBuilder.RegisterType<ShowPackageFileContentCommand>();
		ContainerBuilder.RegisterType<CompilePackageCommand>();
		ContainerBuilder.RegisterType<SwitchNugetToDllCommand>();
		ContainerBuilder.RegisterType<NugetMaterializer>();
		ContainerBuilder.RegisterType<PropsBuilder>();
		ContainerBuilder.RegisterType<UninstallAppCommand>();
		ContainerBuilder.RegisterType<DownloadAppCommand>();
		ContainerBuilder.RegisterType<DeployAppCommand>();
		ContainerBuilder.RegisterType<ApplicationManager>();
		ContainerBuilder.RegisterType<RestoreDbCommand>();
		ContainerBuilder.RegisterType<DbClientFactory>().As<IDbClientFactory>();
		ContainerBuilder.RegisterType<SetWebServiceUrlCommand>();
		ContainerBuilder.RegisterType<ListInstalledAppsCommand>();
		ContainerBuilder.RegisterType<GetCreatioInfoCommand>();
		ContainerBuilder.RegisterType<SetApplicationVersionCommand>();
		ContainerBuilder.RegisterType<ApplyEnvironmentManifestCommand>();
		ContainerBuilder.RegisterType<EnvironmentManager>();
		ContainerBuilder.RegisterType<GetWebServiceUrlCommand>();
		MediatRConfiguration configuration = MediatRConfigurationBuilder
			.Create(typeof(BindingsModule).Assembly)
			.WithAllOpenGenericHandlerTypesRegistered()
			.Build();
		ContainerBuilder.RegisterMediatR(configuration);

		ContainerBuilder.RegisterGeneric(typeof(ValidationBehaviour<,>)).As(typeof(IPipelineBehavior<,>));
		ContainerBuilder.RegisterType<ExternalLinkOptionsValidator>();
		ContainerBuilder.RegisterType<SetFsmConfigOptionsValidator>();
		ContainerBuilder.RegisterType<UnzipRequestValidator>();
		ContainerBuilder.RegisterType<GitSyncCommand>();
		ContainerBuilder.RegisterType<DeactivatePackageCommand>();
		ContainerBuilder.RegisterType<PublishWorkspaceCommand>();
		ContainerBuilder.RegisterType<ActivatePackageCommand>();
		ContainerBuilder.RegisterType<PackageHotFixCommand>();
		ContainerBuilder.RegisterType<PackageEditableMutator>();
		ContainerBuilder.RegisterType<SaveSettingsToManifestCommand>();

		customRegistrations?.Invoke(ContainerBuilder);
		return ContainerBuilder.Build();
	}
	

	#endregion

}