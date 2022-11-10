﻿using DailyDuty.Interfaces;
using DailyDuty.UserInterface.Components;
using DailyDuty.UserInterface.Components.InfoBox;
using System;
using System.Linq;
using DailyDuty.Addons;
using DailyDuty.Configuration.Components;
using DailyDuty.Localization;
using DailyDuty.Utilities;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Utility.Signatures;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace DailyDuty.Modules;

public class MaskedCarnivaleSettings : GenericSettings
{
    public TrackedMaskedCarnivale[] TrackedTasks =
    {
        new(CarnivaleTask.Novice, new Setting<bool>(true), false),
        new(CarnivaleTask.Moderate, new Setting<bool>(true), false),
        new(CarnivaleTask.Advanced, new Setting<bool>(true), false)
    };
}

internal class MaskedCarnivale : IModule
{
    public ModuleName Name => ModuleName.MaskedCarnivale;
    public IConfigurationComponent ConfigurationComponent { get; }
    public IStatusComponent StatusComponent { get; }
    public ILogicComponent LogicComponent { get; }
    public ITodoComponent TodoComponent { get; }
    public ITimerComponent TimerComponent { get; }

    private static MaskedCarnivaleSettings Settings => Service.ConfigurationManager.CharacterConfiguration.MaskedCarnivale;
    public GenericSettings GenericSettings => Settings;

    public MaskedCarnivale()
    {
        ConfigurationComponent = new ModuleConfigurationComponent(this);
        StatusComponent = new ModuleStatusComponent(this);
        LogicComponent = new ModuleLogicComponent(this);
        TodoComponent = new ModuleTodoComponent(this);
        TimerComponent = new ModuleTimerComponent(this);
    }

    public void Dispose()
    {
        LogicComponent.Dispose();
    }

    private class ModuleConfigurationComponent : IConfigurationComponent
    {
        public IModule ParentModule { get; }
        public ISelectable Selectable => new ConfigurationSelectable(ParentModule, this);

        public ModuleConfigurationComponent(IModule parentModule)
        {
            ParentModule = parentModule;
        }

        public void Draw()
        {
            InfoBox.DrawGenericSettings(this);

            InfoBox.Instance
                .AddTitle(Strings.Module.GrandCompany.TrackedJobs)
                .AddList(Settings.TrackedTasks)
                .Draw();
            
            InfoBox.DrawNotificationOptions(this);
        }
    }

    private class ModuleStatusComponent : IStatusComponent
    {
        public IModule ParentModule { get; }

        public ISelectable Selectable => new StatusSelectable(ParentModule, this, ParentModule.LogicComponent.GetModuleStatus);

        public ModuleStatusComponent(IModule parentModule)
        {
            ParentModule = parentModule;
        }

        public void Draw()
        {
            if (ParentModule.LogicComponent is not ModuleLogicComponent logicModule) return;

            var moduleStatus = logicModule.GetModuleStatus();

            InfoBox.Instance
                .AddTitle(Strings.Status.Label)
                .BeginTable()
                .BeginRow()
                .AddString(Strings.Status.ModuleStatus)
                .AddString(moduleStatus.GetTranslatedString(), moduleStatus.GetStatusColor())
                .EndRow()
                .EndTable()
                .Draw();
            
            if (Settings.TrackedTasks.Any(row => row.Tracked.Value))
            {
                InfoBox.Instance
                    .AddTitle(Strings.Status.ModuleStatus)
                    .BeginTable()
                    .AddRows(Settings.TrackedTasks.Where(row => row.Tracked.Value))
                    .EndTable()
                    .Draw();
            }
            else
            {
                InfoBox.Instance
                    .AddTitle(Strings.Status.ModuleStatus)
                    .AddString(Strings.Module.MaskedCarnivale.NoTasksTracked, Colors.Orange)
                    .Draw();
            }
        }
    }

    private unsafe class ModuleLogicComponent : ILogicComponent
    {
        public IModule ParentModule { get; }

        private AgentInterface* AozContentBriefingAgentInterface => Framework.Instance()->GetUiModule()->GetAgentModule()->GetAgentByInternalId(AgentId.AozContentBriefing);
        
        private delegate byte IsWeeklyCompleteDelegate(AgentInterface* agent, byte index);
        [Signature("4C 8B C1 80 FA 03")] private readonly IsWeeklyCompleteDelegate isWeeklyCompleted = null!;
        
        public ModuleLogicComponent(IModule parentModule)
        {
            ParentModule = parentModule;
            
            SignatureHelper.Initialise(this);
            
            Service.AddonManager.Get<AOZContentResultAddon>().Setup += OnResultSetup;
            Service.AddonManager.Get<AOZContentBriefingAddon>().Setup += OnBriefingSetup;
        }
        
        public void Dispose()
        {
            Service.AddonManager.Get<AOZContentResultAddon>().Setup -= OnResultSetup;
            Service.AddonManager.Get<AOZContentBriefingAddon>().Setup -= OnBriefingSetup;
        }
        
        private void OnResultSetup(object? sender, AOZContentResultArgs e)
        {
            switch (e.CompletionType)
            {
                // Novice
                case 0 when e.Successful:
                    foreach (var task in Settings.TrackedTasks)
                    {
                        if (task.Task == CarnivaleTask.Novice)
                        {
                            task.State = true;
                            Service.ConfigurationManager.Save();
                        }
                    }
                    break;
                
                // Moderate 
                case 1 when e.Successful:
                    foreach (var task in Settings.TrackedTasks)
                    {
                        if (task.Task == CarnivaleTask.Moderate)
                        {
                            task.State = true;
                            Service.ConfigurationManager.Save();
                        }
                    }
                    break;
                
                // Advanced
                case 2 when e.Successful:
                    foreach (var task in Settings.TrackedTasks)
                    {
                        if (task.Task == CarnivaleTask.Advanced)
                        {
                            task.State = true;
                            Service.ConfigurationManager.Save();
                        }
                    }
                    break;
                    
                // Non-Weekly bonus task
                case 3:
                    break;
            }
        }
        
        private void OnBriefingSetup(object? sender, EventArgs e)
        {
            foreach (var task in Settings.TrackedTasks)
            {
                task.State = isWeeklyCompleted(AozContentBriefingAgentInterface, (byte) task.Task) != 0;
                Service.ConfigurationManager.Save();
            }
        }

        public string GetStatusMessage() => $"{GetIncompleteCount()} {Strings.Module.MaskedCarnivale.AllowancesRemaining}";

        public DalamudLinkPayload? DalamudLinkPayload => null;

        public DateTime GetNextReset() => Time.NextWeeklyReset();

        public void DoReset()
        {
            foreach (var task in Settings.TrackedTasks)
            {
                task.State = false;
            }
        }

        public ModuleStatus GetModuleStatus() => GetIncompleteCount() == 0 ? ModuleStatus.Complete : ModuleStatus.Incomplete;

        private int GetIncompleteCount() => Settings.TrackedTasks.Where(task => task.Tracked.Value && !task.State).Count();
    }

    private class ModuleTodoComponent : ITodoComponent
    {
        public IModule ParentModule { get; }
        public CompletionType CompletionType => CompletionType.Weekly;
        public bool HasLongLabel => false;

        public ModuleTodoComponent(IModule parentModule)
        {
            ParentModule = parentModule;
        }

        public string GetShortTaskLabel() => Strings.Module.MaskedCarnivale.Label;

        public string GetLongTaskLabel() => Strings.Module.MaskedCarnivale.Label;
    }

    private class ModuleTimerComponent : ITimerComponent
    {
        public IModule ParentModule { get; }

        public ModuleTimerComponent(IModule parentModule)
        {
            ParentModule = parentModule;
        }

        public TimeSpan GetTimerPeriod() => TimeSpan.FromDays(7);
        public DateTime GetNextReset() => Time.NextWeeklyReset();
    }
}