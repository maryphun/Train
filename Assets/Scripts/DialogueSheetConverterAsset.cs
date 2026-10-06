using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
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
        "master"
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
            if (IsInstructionRow(row))
                continue;

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
            if (ContainsJapaneseCharacters(nodeName))
            {
                Debug.Log($"Skipped Node '{nodeName}' in sheet '{sheetName}' because its name contains Japanese characters.");
                continue;
            }

            StringBuilder nodeBody = new();

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
                    AppendCommands(nodeBody, command, 0);
                }

                if (!string.IsNullOrWhiteSpace(text))
                {
                    AppendDialogueLine(nodeBody, speaker, text, lineId);
                }

                if (!string.IsNullOrWhiteSpace(choiceText))
                {
                    AppendChoice(nodeBody, choiceText, lineId, nextNode);
                }
            }

            if (nodeBody.Length == 0)
            {
                Debug.Log($"Skipped empty Node '{nodeName}' in sheet '{sheetName}'.");
                continue;
            }

            yarn.AppendLine($"title: {nodeName}");
            yarn.AppendLine("---");
            yarn.Append(PrepareStageCommandsInText(PrepareCharacterSetupInText(nodeBody.ToString())));
            yarn.AppendLine("===");
            yarn.AppendLine();
        }

        return yarn.ToString();
    }

    private static bool ContainsJapaneseCharacters(string value)
    {
        for (int index = 0; index < value.Length; index++)
        {
            int codePoint = value[index];
            if (char.IsSurrogatePair(value, index))
            {
                codePoint = char.ConvertToUtf32(value, index);
                index++;
            }

            // Hiragana, Katakana (including half-width/supplementary forms),
            // Japanese iteration marks, and CJK ideographs used for Kanji.
            if ((codePoint >= 0x3040 && codePoint <= 0x30FF)
                || (codePoint >= 0x31F0 && codePoint <= 0x31FF)
                || (codePoint >= 0xFF66 && codePoint <= 0xFF9F)
                || (codePoint >= 0x1AFF0 && codePoint <= 0x1AFFF)
                || (codePoint >= 0x1B000 && codePoint <= 0x1B16F)
                || (codePoint >= 0x3005 && codePoint <= 0x3007)
                || (codePoint >= 0x3400 && codePoint <= 0x4DBF)
                || (codePoint >= 0x4E00 && codePoint <= 0x9FFF)
                || (codePoint >= 0xF900 && codePoint <= 0xFAFF)
                || (codePoint >= 0x20000 && codePoint <= 0x2EE5F)
                || (codePoint >= 0x2F800 && codePoint <= 0x2FA1F)
                || (codePoint >= 0x30000 && codePoint <= 0x3FFFF))
                return true;
        }

        return false;
    }

    private bool IsInstructionRow(Dictionary<string, string> row)
    {
        string value = GetCell(row, nodeColumn);

        if (!value.StartsWith("[") || !value.EndsWith("]"))
            return false;

        foreach (KeyValuePair<string, string> cell in row)
        {
            if (string.Equals(cell.Key, nodeColumn, StringComparison.Ordinal))
                continue;

            if (!string.IsNullOrWhiteSpace(cell.Value))
                return false;
        }

        return true;
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
    }

    private void AppendCommands(StringBuilder yarn, string commandCell, int indentLevel)
    {
        string indent = new string(' ', indentLevel * 4);
        List<string> commands = new();

        foreach (string rawCommand in GetCommandEntries(commandCell))
        {
            string command = StripCommandDelimiters(rawCommand);

            if (string.IsNullOrWhiteSpace(command))
                continue;

            if (command.StartsWith("<<") && command.EndsWith(">>"))
            {
                commands.Add(command);
            }
            else
            {
                commands.Add(FormatCommand(command));
            }
        }

        foreach (string command in PrepareCharacterSetupCommands(commands))
        {
            yarn.AppendLine(indent + command);
        }
    }

    // Fold immediate placement in a command block into the preceding show, before its fade.
    // A dialogue line, wait, timed motion, or other non-setup command remains a timing boundary.
    internal static List<string> PrepareCharacterSetupCommands(IReadOnlyList<string> commands)
    {
        return PrepareCharacterSetupCommands(commands, false);
    }

    private static List<string> PrepareCharacterSetupCommands(IReadOnlyList<string> commands, bool coordinateStage)
    {
        List<List<string>> outputTokens = new();
        List<string> output = new();
        Dictionary<string, int> shows = new(StringComparer.OrdinalIgnoreCase);
        foreach (string command in commands)
        {
            List<string> tokens = ReadLiteralCharacterCommand(command);
            string action = tokens != null ? tokens[1].Trim('"').ToLowerInvariant() : string.Empty;
            string id = tokens != null ? ReadCharacterTokenValue(tokens[2]) : string.Empty;
            if ((action == "show" || action == "add") && tokens.Count >= 4)
            {
                shows[id] = output.Count;
                output.Add(command);
                outputTokens.Add(tokens);
                continue;
            }

            if (tokens != null && TryGetInitialTransform(tokens, action, out int targetIndex, out string value))
            {
                if (shows.TryGetValue(id, out int showIndex))
                {
                    List<string> show = outputTokens[showIndex];
                    string[] defaults = { "0.5", "\"instant\"", "false", "\"keep\"", "\"keep\"" };
                    while (show.Count < 9)
                    {
                        show.Add(defaults[show.Count - 4]);
                    }
                    show[targetIndex] = value;
                    if (targetIndex == 4)
                    {
                        if (show.Count < 10)
                            show.Add("\"instant\"");
                        else
                            show[9] = "\"instant\"";
                    }
                    output[showIndex] = "<<" + string.Join(" ", show) + ">>";
                }
                else
                {
                    // Without a preceding show, let the runtime handle this transform.
                    output.Add(command);
                    outputTokens.Add(tokens);
                }
                continue;
            }

            // Within a coordinated stage, other effect families cannot delay
            // a new character's initial pose until after its reveal.
            List<string> otherTokens = coordinateStage ? ReadLiteralCommand(command) : null;
            bool immediateCharacterEffect = coordinateStage && tokens != null && (action == "flash" || action == "shake");
            if (!immediateCharacterEffect && (otherTokens == null || otherTokens[0] == "char" || !IsStageEffectKind(otherTokens[0])))
                shows.Clear();
            output.Add(command);
            outputTokens.Add(tokens);
        }
        return output;
    }

    internal static string PrepareCharacterSetupInText(string text)
    {
        return PrepareCommandBlocksInText(text, PrepareCharacterSetupCommands);
    }

    internal static string PrepareStageCommandsInText(string text)
    {
        return PrepareCommandBlocksInText(text, PrepareStageCommands);
    }

    private static string PrepareCommandBlocksInText(string text, Func<IReadOnlyList<string>, List<string>> prepare)
    {
        StringBuilder result = new();
        List<string> commands = new();
        string indent = string.Empty;
        string lineEnding = Environment.NewLine;
        string lastEnding = lineEnding;
        void FlushCommands()
        {
            List<string> prepared = prepare(commands);
            for (int index = 0; index < prepared.Count; index++)
                result.Append(indent).Append(prepared[index]).Append(index == prepared.Count - 1 ? lastEnding : lineEnding);
            commands.Clear();
        }

        foreach (Match match in Regex.Matches(text, @"[^\r\n]+(?:\r\n|\n|\r|$)|\r\n|\n|\r"))
        {
            string line = match.Value.TrimEnd('\r', '\n');
            string ending = match.Value[line.Length..];
            string trimmed = line.TrimStart();
            string currentIndent = line[..(line.Length - trimmed.Length)];
            if (trimmed.StartsWith("<<") && trimmed.EndsWith(">>"))
            {
                if (commands.Count > 0 && currentIndent != indent)
                    FlushCommands();
                if (commands.Count == 0)
                {
                    indent = currentIndent;
                    lineEnding = ending.Length > 0 ? ending : Environment.NewLine;
                }
                commands.Add(trimmed);
                lastEnding = ending;
            }
            else
            {
                FlushCommands();
                result.Append(match.Value);
            }
        }
        FlushCommands();
        return result.ToString();
    }

    private static List<string> ReadLiteralCharacterCommand(string command)
    {
        List<string> tokens = ReadLiteralCommand(command);
        return tokens != null && tokens.Count >= 3 && tokens[0] == "char" ? tokens : null;
    }

    private static List<string> ReadLiteralCommand(string command)
    {
        if (!command.StartsWith("<<") || !command.EndsWith(">>"))
            return null;
        List<string> tokens = new();
        foreach (Match token in Regex.Matches(command[2..^2], @"""(?:\\.|[^""\\])*""|\S+"))
        {
            if (token.Value[0] != '"' && token.Value.IndexOfAny(new[] { '$', '(', ')', '{', '}' }) >= 0)
                return null;
            tokens.Add(token.Value);
        }
        return tokens.Count > 0 ? tokens : null;
    }

    private static List<string> PrepareStageCommands(IReadOnlyList<string> commands)
    {
        List<string> output = new();
        List<string> original = new();
        List<DialogueStageCommand> stage = new();
        HashSet<string> shownIds = new(StringComparer.OrdinalIgnoreCase);
        bool hasBackground = false;
        void Flush()
        {
            List<string> prepared = PrepareCharacterSetupCommands(original, true);
            stage.Clear();
            foreach (string command in prepared)
            {
                List<string> tokens = ReadLiteralCommand(command);
                string[] args = new string[tokens.Count - 1];
                for (int index = 1; index < tokens.Count; index++)
                    args[index - 1] = ReadCharacterTokenValue(tokens[index]);
                stage.Add(new DialogueStageCommand { kind = tokens[0], args = args });
            }
            if (stage.Count > 1)
            {
                string json = JsonUtility.ToJson(new DialogueStageSequence { commands = stage.ToArray() });
                // Yarn interprets JSON braces as expressions, even inside command quotes.
                string payload = Convert.ToBase64String(Encoding.UTF8.GetBytes(json));
                output.Add("<<dialogue_stage \"" + payload + "\">>");
            }
            else
                output.AddRange(prepared);
            original.Clear();
            stage.Clear();
            shownIds.Clear();
            hasBackground = false;
        }

        foreach (string command in commands)
        {
            List<string> tokens = ReadLiteralCommand(command);
            string kind = tokens != null ? tokens[0] : string.Empty;
            bool background = kind == "background" || kind == "bg";
            string action = tokens != null && tokens.Count > 1 ? ReadCharacterTokenValue(tokens[1]).ToLowerInvariant() : string.Empty;
            bool clear = action == "clear" || action == "hide_all" || action == "remove_all";
            bool hide = action == "hide" || action == "remove";
            bool show = action == "show" || action == "add";
            bool supported = IsStageEffectKind(kind) && (kind == "shake" || tokens.Count > 1)
                || kind == "char" && (clear || tokens.Count > 2 && (hide || show
                    || action == "move" || action == "position" || action == "face" || action == "sprite"
                    || action == "variation" || action == "flip" || action == "tint" || action == "color"
                    || action == "flash" || action == "shake"
                    || action == "scale" || action == "size" || action == "order"));
            if (!supported)
            {
                Flush();
                output.Add(command);
                continue;
            }

            string id = kind == "char" && tokens.Count > 2 ? ReadCharacterTokenValue(tokens[2]) : string.Empty;
            // Preserve temporary show -> hide/clear sequences and distinct background transitions.
            if (background && hasBackground || kind == "char" && (hide && shownIds.Contains(id) || clear && shownIds.Count > 0))
                Flush();
            original.Add(command);
            if (background) hasBackground = true;
            if (kind == "char" && show) shownIds.Add(id);
        }
        Flush();
        return output;
    }

    private static bool IsStageEffectKind(string kind)
    {
        return kind == "background" || kind == "bg" || kind == "bgm" || kind == "se"
            || kind == "shake" || kind == "fade" || kind == "dialogue" || kind == "wait";
    }

    private static string ReadCharacterTokenValue(string token)
    {
        return token.Length >= 2 && token[0] == '"' && token[^1] == '"'
            ? token[1..^1].Replace("\\\"", "\"").Replace("\\\\", "\\")
            : token;
    }

    private static bool TryGetInitialTransform(List<string> tokens, string action, out int targetIndex, out string value)
    {
        targetIndex = -1;
        value = null;
        if (action == "flip")
        {
            value = tokens.Count > 3 ? tokens[3] : "true";
            string flip = value.Trim('"').ToLowerInvariant();
            if (flip != "true" && flip != "false" && flip != "flip" && flip != "flipped"
                && flip != "left" && flip != "right" && flip != "normal" && flip != "none")
                return false;
            targetIndex = 6;
            return true;
        }
        if (tokens.Count < 4 || (action != "move" && action != "position" && action != "scale" && action != "size"))
            return false;
        string duration = tokens.Count > 4 ? tokens[4].Trim('"') : "instant";
        bool instant = string.Equals(duration, "instant", StringComparison.OrdinalIgnoreCase)
            || string.Equals(duration, "none", StringComparison.OrdinalIgnoreCase)
            || (float.TryParse(duration, NumberStyles.Float, CultureInfo.InvariantCulture, out float seconds)
                && !float.IsNaN(seconds) && !float.IsInfinity(seconds) && seconds <= 0f);
        if (!instant || !float.TryParse(tokens[3].Trim('"'), NumberStyles.Float, CultureInfo.InvariantCulture, out float number)
            || float.IsNaN(number) || float.IsInfinity(number))
            return false;
        targetIndex = action == "move" || action == "position" ? 4 : 8;
        value = tokens[3];
        return true;
    }

    private List<string> GetCommandEntries(string commandCell)
    {
        List<string> bracketCommands = ExtractBracketCommands(commandCell);

        if (bracketCommands.Count > 0)
            return bracketCommands;

        return SplitCommandLines(commandCell);
    }

    private List<string> ExtractBracketCommands(string commandCell)
    {
        List<string> commands = new();
        StringBuilder currentCommand = new();
        bool insideCommand = false;

        foreach (char c in commandCell)
        {
            if (c == '[' && !insideCommand)
            {
                insideCommand = true;
                currentCommand.Clear();
                continue;
            }

            if (c == ']' && insideCommand)
            {
                AddCommandEntry(commands, currentCommand.ToString());
                currentCommand.Clear();
                insideCommand = false;
                continue;
            }

            if (insideCommand)
            {
                currentCommand.Append(c);
            }
        }

        if (insideCommand)
        {
            Debug.LogWarning($"Unclosed command bracket in Command cell: {commandCell}");
            AddCommandEntry(commands, currentCommand.ToString());
        }

        return commands;
    }

    private List<string> SplitCommandLines(string commandCell)
    {
        List<string> commands = new();
        string[] lines = commandCell.Split(
            new[] { '\n', '\r' },
            StringSplitOptions.RemoveEmptyEntries
        );

        foreach (string line in lines)
        {
            AddCommandEntry(commands, line);
        }

        return commands;
    }

    private void AddCommandEntry(List<string> commands, string command)
    {
        command = command.Trim();

        if (!string.IsNullOrWhiteSpace(command))
        {
            commands.Add(command);
        }
    }

    private string StripCommandDelimiters(string command)
    {
        command = command.Trim();

        if (command.StartsWith("[") && command.EndsWith("]"))
        {
            command = command[1..^1].Trim();
        }

        return command;
    }

    private string FormatCommand(string command)
    {
        List<string> parts = SplitCommandParts(command);

        if (parts.Count == 0)
            return "";

        string commandName = parts[0];

        if (parts.Count == 1)
            return $"<<{commandName}>>";

        List<string> args = new();

        for (int i = 1; i < parts.Count; i++)
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

    private List<string> SplitCommandParts(string command)
    {
        List<string> parts = new();
        StringBuilder currentPart = new();

        foreach (char c in command)
        {
            if (c == ':' || char.IsWhiteSpace(c))
            {
                AddCommandPart(parts, currentPart);
                continue;
            }

            currentPart.Append(c);
        }

        AddCommandPart(parts, currentPart);
        return parts;
    }

    private void AddCommandPart(List<string> parts, StringBuilder currentPart)
    {
        if (currentPart.Length == 0)
            return;

        parts.Add(currentPart.ToString());
        currentPart.Clear();
    }

    private bool IsNumber(string value)
    {
        return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out _);
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
