﻿using System;
using System.Collections.Generic;

using UnityEditor;

using Codice.LogWrapper;
using Unity.PlasticSCM.Editor.Hub.Operations;

namespace Unity.PlasticSCM.Editor.Hub
{
    internal static class ProcessHubCommand
    {
        internal const string IS_PROCESS_COMMAND_ALREADY_EXECUTED_KEY =
            "PlasticSCM.ProcessHubCommand.IsAlreadyExecuted";
        internal const string IS_PLASTIC_COMMAND_KEY =
            "PlasticSCM.ProcessHubCommand.IsPlasticCommand";

        internal static void Initialize()
        {
            EditorApplication.update += RunOnceWhenAccessTokenIsInitialized;
        }

        static void RunOnceWhenAccessTokenIsInitialized()
        {
            if (string.IsNullOrEmpty(CloudProjectSettings.accessToken))
            {
                return;
            }

            EditorApplication.update -= RunOnceWhenAccessTokenIsInitialized;

            Execute(CloudProjectSettings.accessToken);
        }

        static void Execute(string unityAccessToken)
        {
            if (SessionState.GetBool(IS_PROCESS_COMMAND_ALREADY_EXECUTED_KEY, false))
            {
                return;
            }

            ProcessCommandFromArgs(Environment.GetCommandLineArgs(), unityAccessToken);

            SessionState.SetBool(IS_PROCESS_COMMAND_ALREADY_EXECUTED_KEY, true);
        }

        internal static void ProcessCommandFromArgs(
            string[] commandLineArgs,
            string unityAccessToken)
        {
            Dictionary<string, string> args = CommandLineArguments.Build(commandLineArgs);

            ParseArguments.Command command = ParseArguments.GetCommand(args);

            if (!command.IsValid())
            {
               return;
            }

            SessionState.SetBool(IS_PLASTIC_COMMAND_KEY, true);

            PlasticApp.InitializeIfNeeded();

            mLog.DebugFormat("Command line arguments: {0}", string.Join(" ", commandLineArgs));
            mLog.DebugFormat("Processing command: {0}", command.OperationType);

            OperationParams parameters = OperationParams.
                BuildFromCommand(command, unityAccessToken);

            switch (command.OperationType)
            {
                case ParseArguments.Command.Operation.CreateWorkspace:
                    CreateWorkspace.LaunchOperation(parameters);
                    return;
                case ParseArguments.Command.Operation.DownloadRepository:
                    DownloadRepository.LaunchOperation(parameters);
                    return;
            }
        }

        static readonly ILog mLog = PlasticApp.GetLogger("ProcessHubCommand");
    }
}
