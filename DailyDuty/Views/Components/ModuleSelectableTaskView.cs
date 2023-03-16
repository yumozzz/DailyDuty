﻿using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using DailyDuty.Abstracts;
using DailyDuty.Models;
using DailyDuty.Models.Attributes;
using DailyDuty.System.Localization;
using Dalamud.Interface;
using ImGuiNET;
using KamiLib.Caching;
using Lumina.Excel.GeneratedSheets;
using Action = System.Action;

namespace DailyDuty.Views.Components;

public static class ModuleSelectableTaskView
{
    public static void DrawConfig(FieldInfo? field, ModuleConfigBase moduleConfig, Action saveAction)
    {
        if (field is null) return;
        
        ImGui.Text("Task Selection");
        ImGui.Separator();

        ImGuiHelpers.ScaledIndent(15.0f);

        if (field.FieldType.IsGenericType && field.FieldType.GetGenericTypeDefinition() == typeof(List<>))
        {
            // If the type inside the list is generic
            if (field.FieldType.GetGenericArguments()[0] is { IsGenericType: true } listType)
            {
                // If the list contains a LuminaTaskConfig
                if (listType.IsGenericType && listType.GetGenericTypeDefinition() == typeof(LuminaTaskConfig<>))
                {
                    var configType = listType.GetGenericArguments()[0];

                    // If the contained type is ContentsNote
                    if (configType == typeof(ContentsNote))
                    {
                        var list = (List<LuminaTaskConfig<ContentsNote>>) field.GetValue(moduleConfig)!;

                        foreach (var category in LuminaCache<ContentsNoteCategory>.Instance.Where(category => category.CategoryName.ToString() != string.Empty))
                        {
                            if (ImGui.CollapsingHeader(category.CategoryName.ToString()))
                            {
                                foreach (var option in list)
                                {
                                    var luminaData = LuminaCache<ContentsNote>.Instance.GetRow(option.RowId)!;
                                    if (luminaData.ContentType != category.RowId) continue;

                                    var enabled = option.Enabled;
                                    if (ImGui.Checkbox(luminaData.Name.ToString(), ref enabled))
                                    {
                                        option.Enabled = enabled;
                                        saveAction.Invoke();
                                    }
                                }
                            }
                        }
                    }
                    else if (configType == typeof(ContentRoulette))
                    {
                        var list = (List<LuminaTaskConfig<ContentRoulette>>) field.GetValue(moduleConfig)!;

                        foreach (var option in list)
                        {
                            var luminaData = LuminaCache<ContentRoulette>.Instance.GetRow(option.RowId)!;

                            var enabled = option.Enabled;
                            if (ImGui.Checkbox(luminaData.Name.ToString(), ref enabled))
                            {
                                option.Enabled = enabled;
                                saveAction.Invoke();
                            }
                        }
                    }
                }
            }

            ImGuiHelpers.ScaledDummy(10.0f);
            ImGuiHelpers.ScaledIndent(-15.0f);
        }
    }
    
    public static void DrawData(FieldInfo? field, ModuleDataBase moduleData)
    {
        if (field is null) return;
        
        ImGui.Text("Task Data");
        ImGui.Separator();
        ImGuiHelpers.ScaledIndent(15.0f);

        if (ImGui.BeginTable("##TaskDataTable", 2, ImGuiTableFlags.SizingStretchSame))
        {
            if (field.FieldType.IsGenericType && field.FieldType.GetGenericTypeDefinition() == typeof(List<>))
            {
                if (field.FieldType.GetGenericArguments()[0] is { IsGenericType: true } listType)
                {
                    // If the list contains a LuminaTaskConfig
                    if (listType.IsGenericType && listType.GetGenericTypeDefinition() == typeof(LuminaTaskData<>))
                    {
                        var configType = listType.GetGenericArguments()[0];

                        // If the contained type is ContentsNote
                        if (configType == typeof(ContentsNote))
                        {
                            var list = (List<LuminaTaskData<ContentsNote>>) field.GetValue(moduleData)!;

                            foreach (var data in list)
                            {
                                var luminaData = LuminaCache<ContentsNote>.Instance.GetRow(data.RowId)!;

                                ImGui.TableNextColumn();
                                ImGui.Text(luminaData.Name.ToString());

                                ImGui.TableNextColumn();
                                var color = data.Complete ? KnownColor.Green.AsVector4() : KnownColor.Orange.AsVector4();
                                var text = data.Complete ? Strings.Complete : Strings.Incomplete;
                                ImGui.TextColored(color, text);
                            }
                        }
                        else if (configType == typeof(ContentRoulette))
                        {
                            var list = (List<LuminaTaskData<ContentRoulette>>) field.GetValue(moduleData)!;

                            foreach (var data in list)
                            {
                                ImGui.TableNextColumn();
                                ImGui.Text(LuminaCache<ContentRoulette>.Instance.GetRow(data.RowId)!.Name.ToString());

                                ImGui.TableNextColumn();
                                var color = data.Complete ? KnownColor.Green.AsVector4() : KnownColor.Orange.AsVector4();
                                var text = data.Complete ? Strings.Complete : Strings.Incomplete;
                                ImGui.TextColored(color, text);
                            }
                        }
                    }
                }
            }
            ImGui.EndTable();
        }

        ImGuiHelpers.ScaledDummy(10.0f);
        ImGuiHelpers.ScaledIndent(-15.0f);
    }
}
