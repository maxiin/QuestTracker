using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using Lumina.Excel.Sheets;
using Newtonsoft.Json;

namespace QuestTracker;

public class DataConverter
{
    public static IDalamudPluginInterface PluginInterface { get; private set; } = null!;
    public static IDataManager DataManager { get; private set; } = null!;
    public static IPluginLog PluginLog { get; private set; } = null!;

    private string dirPath = string.Empty;

    public DataConverter(IDalamudPluginInterface pluginInterface, IDataManager dataManager, IPluginLog pluginLog)
    {
        PluginInterface = pluginInterface;
        DataManager = dataManager;
        PluginLog = pluginLog;

        try
        {
            dirPath = PluginInterface.AssemblyLocation.Directory!.Parent!.Parent!.FullName + "/utils";

            ConvertRawDataTxt();
        }
        catch (Exception ex)
        {
            PluginLog.Error("Error during DataConverter initialization, are you running the plugin in the correct environment? This should only appear in a development setting.");
            PluginLog.Error($"Error: {ex.Message}");
        }
    }

    private void ConvertRawDataTxt()
    {
        try
        {
            PluginLog.Debug("Loading all quests from Lumina and filtering against data.json");

            // Load existing quest IDs from data.json so we only export missing ones
            var existingIds = new HashSet<uint>();
            try
            {
                var dataPath = Path.Combine(PluginInterface.AssemblyLocation.Directory!.Parent!.Parent!.FullName, "data.json");
                if (File.Exists(dataPath))
                {
                    var dataJson = File.ReadAllText(dataPath);
                    var existingData = JsonConvert.DeserializeObject<QuestData>(dataJson);
                    if (existingData != null)
                    {
                        void CollectIds(QuestData node)
                        {
                            if (node.Quests != null)
                            {
                                foreach (var q in node.Quests)
                                {
                                    if (q.Id == null) continue;
                                    foreach (var id in q.Id) existingIds.Add(id);
                                }
                            }

                            if (node.Categories != null)
                            {
                                foreach (var c in node.Categories) CollectIds(c);
                            }
                        }

                        CollectIds(existingData);
                    }
                }
                else
                {
                    PluginLog.Warning($"data.json not found at {PluginInterface.AssemblyLocation.Directory.Parent.Parent.FullName}");
                }
            }
            catch (Exception ex)
            {
                PluginLog.Error("Error reading data.json");
                PluginLog.Error(ex.Message);
            }

            var sheet = DataManager.GetExcelSheet<Lumina.Excel.Sheets.Quest>();
            List<Quest> allQuests = new List<Quest>();

            // IDs to exclude (obsolete, duplicates, removed, etc.)
            var obsoleteIds = new uint[] { 65616, 65692, 65695, 65732, 65734, 65841, 65860, 65863, 65871, 65910, 65918, 65934, 65940, 66000, 66033, 66034, 66288, 66351, 66352, 66356, 66383, 66390, 66407, 66413, 66417, 66432, 66461, 66462, 66490, 66507, 66510, 66575, 66578, 66713, 66715, 66717, 66718, 66719, 66720, 66721, 66722, 66723, 66885, 66887, 66890, 66891, 66893, 66985, 66986, 66987, 66990, 66991, 67097, 67098, 67653, 68629, 68727, 66023, 66582, 66964, 66965, 67635, 67752, 67819 };
            // Initial class quest IDs & others to exclude
            var otherObsolete = new uint[] {
                65603, // Initial Class quest, will only appear if not starting as class
                69577, // Resistance Weapon quest for changing stats, since it is optional it will be excluded.
                67870, // Recondition the Anima seems to have seem substituted by A Dream Fulfilled
                67927, 67926, 67925, // Squadron and Commander quests doesn't seem to track correctly if you change companies, so they will be ignored
                70180, // Seeing the Cieldalaes is the quest that unlocks visits to other's islands, the quest tracking seems to fail for this quest, so it will be ignored.
                69377, 69296, 69508, 69478, 69578, 69630 // Not found
            };

            foreach (var questRow in sheet)
            {
                // Skip entries with empty names
                var title = questRow.Name.ToString();
                if (string.IsNullOrWhiteSpace(title)) continue;
                var level = questRow.ClassJobLevel[0];

                // If this exact quest id already exists in data.json, skip it
                if (existingIds.Contains(questRow.RowId)) continue;
                PluginLog.Debug($"Processing quest row: {questRow.RowId} {questRow.Name} {level} DOESNT EXIST");

                Quest q = new Quest();
                q.Title = title;
                q.Area = questRow.IssuerLocation.ValueNullable?.Territory.ValueNullable?.PlaceName.ValueNullable?.Name.ToString() ?? "???";
                q.Id = new List<uint> { questRow.RowId };
                q.Level = level;

                if (q.Title.Contains("So You Want to Be a") && level == 1) // Initial quests for each class/job are all level 1, so we can use that to filter them out since we don't need them.
                {
                    continue;
                }

                // Remove Obsolete quest ids;
                if (q.Id.Any(id => obsoleteIds.Contains(id)))
                {
                    continue;
                }

                // Initial class quests and others;
                if (q.Id.Any(id => otherObsolete.Contains(id)))
                {
                    continue;
                }

                allQuests.Add(q);
            }

            WriteResults(allQuests);
        }
        catch (Exception e)
        {
            PluginLog.Error("Error loading quests from Lumina");
            PluginLog.Error(e.Message);
        }
    }

    private void WriteResults(Object obj)
    {
        PluginLog.Debug("Writing to results.json");

        var resultPath = Path.Combine(dirPath, "results.json");
        JsonSerializerSettings config = new JsonSerializerSettings
            { DefaultValueHandling = DefaultValueHandling.Ignore, NullValueHandling = NullValueHandling.Ignore };
        var json = JsonConvert.SerializeObject(obj, Formatting.Indented, config);
        File.WriteAllText(resultPath, json);

        PluginLog.Debug("Finishing writing to results.json");
    }
}
