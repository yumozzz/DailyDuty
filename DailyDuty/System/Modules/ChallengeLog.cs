﻿using System.Collections.Generic;
using System.Linq;
using DailyDuty.Abstracts;
using DailyDuty.Models;
using DailyDuty.Models.Attributes;
using DailyDuty.Models.Enums;
using DailyDuty.System.Helpers;
using DailyDuty.System.Localization;
using Lumina.Excel.GeneratedSheets;
using ClientStructs = FFXIVClientStructs.FFXIV.Client.Game.UI;

namespace DailyDuty.System;

public class ChallengeLogConfig : ModuleConfigBase
{
    [SelectableTasks]
    public List<LuminaTaskConfig<ContentsNote>> Tasks = new();
}

public class ChallengeLogData : ModuleDataBase
{
    [SelectableTasks]
    public List<LuminaTaskData<ContentsNote>> Tasks = new();
}

public unsafe class ChallengeLog : Module.WeeklyModule
{
    public override ModuleName ModuleName => ModuleName.ChallengeLog;
    
    public override ModuleDataBase ModuleData { get; protected set; } = new ChallengeLogData();
    public override ModuleConfigBase ModuleConfig { get; protected set; } = new ChallengeLogConfig();
    private ChallengeLogData Data => ModuleData as ChallengeLogData ?? new ChallengeLogData();
    private ChallengeLogConfig Config => ModuleConfig as ChallengeLogConfig ?? new ChallengeLogConfig();

    public override void Load()
    {
        base.Load();

        var luminaUpdater = new LuminaTaskUpdater<ContentsNote>(this, (row) => row.RequiredAmount is not 0);
        luminaUpdater.UpdateConfig(Config.Tasks);
        luminaUpdater.UpdateData(Data.Tasks);
    }
    
    public override void Update()
    {
        var anyUpdate = false;
        
        foreach (var task in Data.Tasks)
        {
            var taskStatus = ClientStructs.ContentsNote.Instance()->IsContentNoteComplete((int) task.RowId);

            if (task.Complete != taskStatus)
            {
                task.Complete = taskStatus;
                anyUpdate = true;
            }
        }

        if (anyUpdate)
        {
            SaveData();
        }
    }

    public override void Reset()
    {
        foreach (var task in Data.Tasks)
        {
            task.Complete = false;
        }
        
        base.Reset();
    }

    protected override ModuleStatus GetModuleStatus() => GetIncompleteCount() == 0 ? ModuleStatus.Complete : ModuleStatus.Incomplete;

    protected override StatusMessage GetStatusMessage() => new()
    {
        Message = $"{GetIncompleteCount()} {Strings.TasksIncomplete}",
    };
    
    private int GetIncompleteCount()
    {
        var taskData = from config in Config.Tasks
            join data in Data.Tasks on config.RowId equals data.RowId
            where config.Enabled
            where !data.Complete
            select new
            {
                config.RowId,
                config.Enabled,
                data.Complete
            };

        return taskData.Count();
    }
}