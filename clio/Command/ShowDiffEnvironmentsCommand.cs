﻿using ATF.Repository.Providers;
using ATF.Repository;
using Clio.Common;
using CommandLine;
using CreatioModel;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using Clio.UserEnvironment;
using DocumentFormat.OpenXml.Drawing;
using k8s.Models;
using YamlDotNet.Serialization;

namespace Clio.Command
{
	[Verb("show-diff", Aliases = new[] {"diff", "compare"},
		HelpText = "Show difference in settings for two Creatio intances")]
	internal class ShowDiffEnvironmentsOptions : EnvironmentNameOptions
	{

		[Option("source", Required = true, HelpText = "Source environment name")]
		public string Source { get; internal set; }

		[Option("target", Required = true, HelpText = "Target environment name")]
		public string Target { get; internal set; }

		[Option("file", Required = false, HelpText = "Diff file name")]
		public string FileName { get; internal set; }

		[Option("overwrite", Required = false, HelpText = "Overwrite existing file", Default = true)]
		public bool Overwrite { get; internal set; }

		[Option("working-directory", Required = false, HelpText = "Working directory ")]
		public string WorkingDirectory { get; internal set; }

	}

	internal class ShowDiffEnvironmentsCommand : BaseDataContextCommand<ShowDiffEnvironmentsOptions>
	{
		private readonly IEnvironmentManager _environmentManager;
		private readonly IWorkingDirectoriesProvider _workingDirectoriesProvider;
		private readonly ISerializer _serializer;
		private readonly ISettingsRepository _settingsRepository;

		public ShowDiffEnvironmentsCommand() { }

		public ShowDiffEnvironmentsCommand(IEnvironmentManager environmentManager, IDataProvider provider,
			ILogger logger, IWorkingDirectoriesProvider workingDirectoriesProvider,
			ISerializer serializer, ISettingsRepository settingsRepository)
			: base(provider, logger){
			_environmentManager = environmentManager;
			_workingDirectoriesProvider = workingDirectoriesProvider;
			_serializer = serializer;
			_settingsRepository = settingsRepository;
		}

		public string Target { get; private set; }
		public string Source { get; private set; }
		public string FileName { get; private set; }
		public bool Overwrite { get; private set; }

		public override int Execute(ShowDiffEnvironmentsOptions options){
			if(options.Target == options.Source) {
				_logger.WriteInfo("No differences found.");
				return 0;
			}
			Target = options.Target;
			Source = options.Source;
			FileName = options.FileName;
			Overwrite = options.Overwrite;
			if (string.IsNullOrEmpty(options.WorkingDirectory)) {
				_workingDirectoriesProvider.CreateTempDirectory(tempDirectory => InternalExecute(tempDirectory));
			}else {
				InternalExecute(options.WorkingDirectory);
			}
			_logger.WriteInfo("Done");
			return 0;
		}

		private void InternalExecute(string workingDirectory) {
			var manifestFileName = FileName ?? $"diff-{Source}-{Target}.yaml";
			string sourceName = $"source-{Source}-manifest.yaml";
			string targetName = $"target-{Target}-manifest.yaml";
			var sourceFilePath = System.IO.Path.Combine(workingDirectory, sourceName);
			var targetFilePath = System.IO.Path.Combine(workingDirectory, targetName);

			SaveEnvironmentManifest(Source, sourceFilePath);
			SaveEnvironmentManifest(Target, targetFilePath);

			EnvironmentManifest sourceManifest = _environmentManager.LoadEnvironmentManifestFromFile(sourceFilePath);
			EnvironmentManifest targetManifest = _environmentManager.LoadEnvironmentManifestFromFile(targetFilePath);
			EnvironmentManifest diffManifest = _environmentManager.GetDiffManifest(sourceManifest, targetManifest);

			if (string.IsNullOrEmpty(FileName)) {
				_logger.WriteInfo("Result diff manifest:");
				var result = _serializer.Serialize(diffManifest);
				if (string.IsNullOrEmpty(result) || result.Trim() == "{}") {
					_logger.WriteInfo("No differences found.");
				} else {
					_logger.WriteInfo(_serializer.Serialize(diffManifest));
				}
			} else {
				_logger.WriteInfo($"Diff manifest saved to {manifestFileName}");
				_environmentManager.SaveManifestToFile(manifestFileName, diffManifest, Overwrite);
			}
		}

		private void SaveEnvironmentManifest(string environmentName, string manifestFilePath){
			_logger.WriteInfo($"Loading environments manifest from {environmentName}");
			var sourceEnv = _settingsRepository.GetEnvironment(environmentName);
			var container = new BindingsModule().Register(sourceEnv);
			var command = container.Resolve<SaveSettingsToManifestCommand>();
			command.Execute(new SaveSettingsToManifestOptions() {
				EnvironmentName = environmentName,
				ManifestFileName = manifestFilePath,
				Overwrite = true,
				SkipDone = true
			});
		}

	}
}