using System.CommandLine;
using System.Runtime.Serialization.Formatters;
using System.Text;

var supportedLanguages = new Dictionary<string, string> {
    {"python",".py" },
    {"csharp",".cs" },
    {"java",".java" },
    {"sql",".sql" },
    {"js",".js" },
    {"html",".html" },
    {"c++",".cpp" },
    {"css",".css" },
    {"all","null" }
};
var rootCommand = new RootCommand("bundle files");
var bundleCommand = new Command("bundle", "bundle files into one file");
var createRspCommand = new Command("create-rsp", "create response file");
var sortOption = new Option<string>("--sort", "Sort files by 'name' or 'type' (default is 'name')")
{
    IsRequired = false,
    Arity = ArgumentArity.ZeroOrOne
}.FromAmong("name", "type");
var authorOption = new Option<string>("--author", "Name of the author of the code file") { IsRequired = false };
var noteOption = new Option<bool>("--note", "to put the source code in the output file") { IsRequired = false };
var outputFileOption = new Option<FileInfo>("--output", "file path and name") { IsRequired = true };
var removeEmptyLinesOption = new Option<bool>("--remove-empty-lines", "remove empty lines befor bundling");
var languageFileOption = new Option<string[]>("--language", "choose languages or all") { AllowMultipleArgumentsPerToken = true }.FromAmong(supportedLanguages.Keys.ToArray());
bundleCommand.AddOption(languageFileOption);
bundleCommand.AddOption(removeEmptyLinesOption);
bundleCommand.AddOption(outputFileOption);
bundleCommand.AddOption(noteOption);
bundleCommand.AddOption(sortOption);
bundleCommand.AddOption(authorOption);
bundleCommand.SetHandler((output, languages, includeNote, sortBy, isAuthor, removeLines) =>
{
    try
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        var allfile = Directory.GetFiles(currentDirectory);

        var selectedExtensions = languages.Contains("all") ? supportedLanguages.Values.ToList() : languages
        .Where(lang => supportedLanguages.ContainsKey(lang))
        .Select(lang => supportedLanguages[lang])
        .ToList();

        var filteredFiles = allfile
           .Where(file => selectedExtensions.Any(ext => file.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
           .ToList();

        filteredFiles = sortBy switch
        {
            "type" => filteredFiles.OrderBy(file => Path.GetExtension(file)).ToList(),
            _ => filteredFiles.OrderBy(file => Path.GetFileName(file)).ToList(),
        };

        var allContent = new StringBuilder();
        if (isAuthor != null)
        {
            allContent.Append($"/*author: {isAuthor}*/\n\n");
        }

        foreach (var file in filteredFiles)
        {
            try
            {
                string[] contentFile = File.ReadAllLines(file);
                if (removeLines)
                    contentFile = contentFile.Where(line => !string.IsNullOrWhiteSpace(line)).ToArray();
                allContent.Append(string.Join('\n', contentFile) + "\n \n");
                if (includeNote != null)
                {
                    allContent.Append($"/* file name: {Path.GetFileName(file)} \n source: {Path.GetRelativePath(currentDirectory, file)} */\n\n");
                }
                allContent.Append(contentFile + "\n \n");
            }
            catch (IOException ex)
            {
                Console.WriteLine($"Error reading file {file}: {ex.Message}");
            }
        }

        try
        {
            File.WriteAllText(output.FullName, allContent.ToString());
            Console.WriteLine("file bundled successfully!");
        }
        catch (IOException ex)
        {
            Console.WriteLine($"Error writing to output file: {ex.Message}");
        }

    }
    catch (DirectoryNotFoundException ex)
    {
        Console.WriteLine($"Directory not found: {ex.Message}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"An unexpected error occurred: {ex.Message}");
    }
}, outputFileOption, languageFileOption, noteOption, sortOption, authorOption, removeEmptyLinesOption);

createRspCommand.SetHandler(() =>
{
    try
    {
        Console.WriteLine("What's the File name for output?");
        string fileName = Console.ReadLine();

        Console.WriteLine("Would you like to include all files? Insert Y or N");
        bool allFiles = Console.ReadLine().Equals("Y", StringComparison.OrdinalIgnoreCase);

        Console.WriteLine("If you don't want to include all files, which languages to include? (separate by space)");
        string languages = Console.ReadLine();

        Console.WriteLine("Would you like to put a note Y or N");
        bool note = Console.ReadLine().Equals("Y", StringComparison.OrdinalIgnoreCase);

        Console.WriteLine("Would you like to sort? Insert Y or N");
        string sort = Console.ReadLine();

        Console.WriteLine("Would you like to remove empty lines? Insert Y or N");
        bool removeLines = Console.ReadLine().Equals("Y", StringComparison.OrdinalIgnoreCase);

        Console.WriteLine("Would you like to put the author? Insert Y or N");
        string aouthor = Console.ReadLine();

        var sb = new StringBuilder();
        sb.Append($"bundle --output \"{fileName}\" ");

        if (allFiles)
        {
            sb.Append("--language all ");
        }
        else
        {
            sb.Append($"--language {languages} ");
        }

        if (removeLines)
        {
            sb.Append("--remove-empty-lines ");
        }
        if (note)
        {
            sb.Append("--note ");
        }
        if (aouthor != null)
        {
            sb.Append($"--author {aouthor}");
        }

        string fullCommand = sb.ToString();
        Console.WriteLine("What's the File name for the response file?");
        string rspName = Console.ReadLine();
        string responseFilePath = $"{rspName}.rsp";
        File.WriteAllText(responseFilePath, fullCommand);
        Console.WriteLine($"Response file created: {responseFilePath}");
        Console.WriteLine($"Ok! all is done😊.\n Your job is to type the command: fm @{responseFilePath}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"An error occurred while creating the response file: {ex.Message}");
    }
});

outputFileOption.AddAlias("--o");
languageFileOption.AddAlias("--l");
noteOption.AddAlias("--n");
removeEmptyLinesOption.AddAlias("--rm");
authorOption.AddAlias("--a");

rootCommand.Add(bundleCommand);
rootCommand.Add(createRspCommand);

rootCommand.InvokeAsync(args);
