using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[CreateAssetMenu(
    fileName = "Dialogue Sheet Converter",
    menuName = "Dialogue/Google Sheet Converter"
)]
public class DialogueSheetConverterAsset : ScriptableObject
{
    [Header("Google Sheet")]
    [SerializeField] private string spreadsheetId;

    [Tooltip("Google Sheets API key. Needed to get all sheet tabs automatically.")]
    [SerializeField] private string googleApiKey;

    [Header("Ignored Sheets")]
    [SerializeField]
    private List<string> ignoredSheetNames = new()
    {
        "Master",
        "master",
        "マスター"
    };

    [Header("Output")]
    [SerializeField] private string outputYarnPath = "Assets/Dialogues/GeneratedDialogue.yarn";

    [Header("Columns")]
    [SerializeField] private string nodeColumn = "Node";
    [SerializeField] private string lineIdColumn = "LineID";
    [SerializeField] private string speakerColumn = "Speaker";
    [SerializeField] private string textColumn = "Text_JP";
    [SerializeField] private string choiceColumn = "ChoiceText_JP";
    [SerializeField] private string nextNodeColumn = "NextNode";
    [SerializeField] private string commandColumn = "Command";

    [Header("Settings")]
    [SerializeField] private bool addLineTags = true;
    [SerializeField] private bool addSheetNameAsComment = true;
    [SerializeField] private bool refreshAssetDatabaseAfterGenerate = true;

#if UNITY_EDITOR
    public void DownloadAndGenerateYarn()
    {
        if (string.IsNullOrWhiteSpace(spreadsheetId))
        {
            Debug.LogError("Spreadsheet ID is empty.");
            return;
        }

        if (string.IsNullOrWhiteSpace(googleApiKey))
        {
            Debug.LogError("Google API Key is empty.");
            return;
        }

        try
        {
            List<GoogleSheetInfo> sheets = GetSheetInfos();

            if (sheets.Count == 0)
            {
                Debug.LogError("No sheets found.");
                return;
            }

            StringBuilder finalYarn = new();

            foreach (GoogleSheetInfo sheet in sheets)
            {
                if (ShouldIgnoreSheet(sheet.title))
                {
                    Debug.Log($"Skipped sheet: {sheet.title}");
                    continue;
                }

                Debug.Log($"Downloading sheet: {sheet.title}");

                string csv = DownloadSheetCsv(sheet.sheetId);
                string yarn = ConvertCsvToYarn(csv, sheet.title);

                finalYarn.AppendLine(yarn);
            }

            string fullPath = Path.GetFullPath(outputYarnPath);
            string directory = Path.GetDirectoryName(fullPath);

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(fullPath, finalYarn.ToString(), new UTF8Encoding(false));

            if (refreshAssetDatabaseAfterGenerate)
            {
                AssetDatabase.Refresh();
            }

            Debug.Log($"Yarn file generated successfully:\n{outputYarnPath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to generate Yarn file:\n{e}");
        }
    }
#endif

    private bool ShouldIgnoreSheet(string sheetName)
    {
        foreach (string ignoredName in ignoredSheetNames)
        {
            if (string.Equals(sheetName, ignoredName, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private List<GoogleSheetInfo> GetSheetInfos()
    {
        string url =
            $"https://sheets.googleapis.com/v4/spreadsheets/{spreadsheetId}" +
            $"?fields=sheets.properties(title,sheetId)" +
            $"&key={googleApiKey}";

        string json;

        using (WebClient client = new WebClient())
        {
            client.Encoding = Encoding.UTF8;
            json = client.DownloadString(url);
        }

        GoogleSpreadsheetMetadata metadata =
            JsonUtility.FromJson<GoogleSpreadsheetMetadata>(json);

        List<GoogleSheetInfo> result = new();

        if (metadata == null || metadata.sheets == null)
            return result;

        foreach (GoogleSheetWrapper sheet in metadata.sheets)
        {
            if (sheet?.properties == null)
                continue;

            result.Add(new GoogleSheetInfo
            {
                title = sheet.properties.title,
                sheetId = sheet.properties.sheetId
            });
        }

        return result;
    }

    private string DownloadSheetCsv(int sheetId)
    {
        string url =
            $"https://docs.google.com/spreadsheets/d/{spreadsheetId}/export" +
            $"?format=csv&gid={sheetId}";

        using (WebClient client = new WebClient())
        {
            client.Encoding = Encoding.UTF8;
            return client.DownloadString(url);
        }
    }

    private string ConvertCsvToYarn(string csv, string sheetName)
    {
        List<Dictionary<string, string>> rows = ParseCsvToRows(csv);

        Dictionary<string, List<Dictionary<string, string>>> nodes = new();
        List<string> nodeOrder = new();

        foreach (Dictionary<string, string> row in rows)
        {
            string node = GetCell(row, nodeColumn);

            if (string.IsNullOrWhiteSpace(node))
                continue;

            if (!nodes.ContainsKey(node))
            {
                nodes[node] = new List<Dictionary<string, string>>();
                nodeOrder.Add(node);
            }

            nodes[node].Add(row);
        }

        StringBuilder yarn = new();

        if (addSheetNameAsComment)
        {
            yarn.AppendLine($"// Sheet: {sheetName}");
            yarn.AppendLine();
        }

        foreach (string nodeName in nodeOrder)
        {
            yarn.AppendLine($"title: {nodeName}");
            yarn.AppendLine("---");

            foreach (Dictionary<string, string> row in nodes[nodeName])
            {
                string lineId = GetCell(row, lineIdColumn);
                string speaker = GetCell(row, speakerColumn);
                string text = GetCell(row, textColumn);
                string choiceText = GetCell(row, choiceColumn);
                string nextNode = GetCell(row, nextNodeColumn);
                string command = GetCell(row, commandColumn);

                if (!string.IsNullOrWhiteSpace(command))
                {
                    AppendCommands(yarn, command, 0);
                }

                if (!string.IsNullOrWhiteSpace(text))
                {
                    AppendDialogueLine(yarn, speaker, text, lineId);
                }

                if (!string.IsNullOrWhiteSpace(choiceText))
                {
                    AppendChoice(yarn, choiceText, lineId, nextNode);
                }
            }

            yarn.AppendLine("===");
            yarn.AppendLine();
        }

        return yarn.ToString();
    }

    private void AppendDialogueLine(StringBuilder yarn, string speaker, string text, string lineId)
    {
        string line = string.IsNullOrWhiteSpace(speaker)
            ? text
            : $"{speaker}: {text}";

        if (addLineTags && !string.IsNullOrWhiteSpace(lineId))
        {
            line += $" #line:{lineId}";
        }

        yarn.AppendLine(line);
    }

    private void AppendChoice(StringBuilder yarn, string choiceText, string lineId, string nextNode)
    {
        string line = $"-> {choiceText}";

        if (addLineTags && !string.IsNullOrWhiteSpace(lineId))
        {
            line += $" #line:{lineId}";
        }

        yarn.AppendLine(line);

        if (!string.IsNullOrWhiteSpace(nextNode))
        {
            yarn.AppendLine($"    <<jump {nextNode}>>");
        }

        yarn.AppendLine();
    }

    private void AppendCommands(StringBuilder yarn, string commandCell, int indentLevel)
    {
        string[] commands = commandCell.Split(
            new[] { '\n', '\r' },
            StringSplitOptions.RemoveEmptyEntries
        );

        string indent = new string(' ', indentLevel * 4);

        foreach (string rawCommand in commands)
        {
            string command = rawCommand.Trim();

            if (string.IsNullOrWhiteSpace(command))
                continue;

            if (command.StartsWith("<<") && command.EndsWith(">>"))
            {
                yarn.AppendLine(indent + command);
            }
            else
            {
                yarn.AppendLine(indent + FormatCommand(command));
            }
        }
    }

    private string FormatCommand(string command)
    {
        string[] parts = command.Split(
            new[] { ' ' },
            StringSplitOptions.RemoveEmptyEntries
        );

        if (parts.Length == 0)
            return "";

        string commandName = parts[0];

        if (parts.Length == 1)
            return $"<<{commandName}>>";

        List<string> args = new();

        for (int i = 1; i < parts.Length; i++)
        {
            string arg = parts[i];

            if (IsNumber(arg) || IsBool(arg))
            {
                args.Add(arg);
            }
            else
            {
                args.Add($"\"{EscapeYarnString(arg)}\"");
            }
        }

        return $"<<{commandName} {string.Join(" ", args)}>>";
    }

    private bool IsNumber(string value)
    {
        return float.TryParse(value, out _);
    }

    private bool IsBool(string value)
    {
        return value == "true" || value == "false";
    }

    private string EscapeYarnString(string value)
    {
        return value
            .Replace("\\", "\\\\")
            .Replace("\"", "\\\"");
    }

    private string GetCell(Dictionary<string, string> row, string columnName)
    {
        if (row.TryGetValue(columnName, out string value))
        {
            return value?.Trim() ?? "";
        }

        return "";
    }

    private List<Dictionary<string, string>> ParseCsvToRows(string csv)
    {
        List<List<string>> table = ParseCsv(csv);
        List<Dictionary<string, string>> rows = new();

        if (table.Count == 0)
            return rows;

        List<string> headers = table[0];

        for (int r = 1; r < table.Count; r++)
        {
            List<string> values = table[r];
            Dictionary<string, string> row = new();

            for (int c = 0; c < headers.Count; c++)
            {
                string header = headers[c].Trim();

                if (string.IsNullOrWhiteSpace(header))
                    continue;

                string value = c < values.Count ? values[c] : "";
                row[header] = value;
            }

            rows.Add(row);
        }

        return rows;
    }

    private List<List<string>> ParseCsv(string csv)
    {
        List<List<string>> rows = new();
        List<string> currentRow = new();
        StringBuilder currentCell = new();

        bool insideQuotes = false;

        for (int i = 0; i < csv.Length; i++)
        {
            char c = csv[i];

            if (insideQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < csv.Length && csv[i + 1] == '"')
                    {
                        currentCell.Append('"');
                        i++;
                    }
                    else
                    {
                        insideQuotes = false;
                    }
                }
                else
                {
                    currentCell.Append(c);
                }
            }
            else
            {
                if (c == '"')
                {
                    insideQuotes = true;
                }
                else if (c == ',')
                {
                    currentRow.Add(currentCell.ToString());
                    currentCell.Clear();
                }
                else if (c == '\n')
                {
                    currentRow.Add(currentCell.ToString());
                    currentCell.Clear();

                    rows.Add(currentRow);
                    currentRow = new List<string>();
                }
                else if (c == '\r')
                {
                    // Ignore CR
                }
                else
                {
                    currentCell.Append(c);
                }
            }
        }

        currentRow.Add(currentCell.ToString());

        if (currentRow.Count > 1 || !string.IsNullOrWhiteSpace(currentRow[0]))
        {
            rows.Add(currentRow);
        }

        return rows;
    }

    [Serializable]
    private class GoogleSpreadsheetMetadata
    {
        public GoogleSheetWrapper[] sheets;
    }

    [Serializable]
    private class GoogleSheetWrapper
    {
        public GoogleSheetProperties properties;
    }

    [Serializable]
    private class GoogleSheetProperties
    {
        public string title;
        public int sheetId;
    }

    private class GoogleSheetInfo
    {
        public string title;
        public int sheetId;
    }
}