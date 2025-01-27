using LocalRAG.Common.Models;
using System.Text.RegularExpressions;

public class MarkdownParser()
{
    public List<MarkdownSection> GetSections(string markdown)
    {
        var sections = new List<MarkdownSection>();
        var lines = markdown.Split("\r\n");
        var index = 0;
        var currentSection = "";
        var headerStack = new Stack<string>();
        var codeBlock = false;

        foreach (var line in lines)
        {
            // Track if we're inside a code block to avoid parsing headers in code
            if (line.TrimStart().StartsWith("```"))
            {
                codeBlock = !codeBlock;
                currentSection += line + "\r\n";
                continue;
            }

            // Only parse headers if we're not in a code block
            if (!codeBlock)
            {
                var headerMatch = Regex.Match(line, @"^(#+)\s(.*)");
                if (headerMatch.Success)
                {

                    var level = headerMatch.Groups[1].Length;
                    var headerText = headerMatch.Groups[2].Value;

                    // Save the previous section before starting a new one
                    if (!string.IsNullOrWhiteSpace(currentSection))
                    {
                        sections.Add(
                            new MarkdownSection(
                                currentSection.Trim(),
                                headerStack.Count > 0 ? headerStack.Peek() : "",
                                index++
                            )
                        );
                    }

                    // Pop headers of equal or higher level
                    while (headerStack.Count >= level)
                    {
                        headerStack.Pop();
                    }

                    // Add the new header
                    headerStack.Push(headerText);
                    currentSection = new string('#', level) + $" {headerText}\n";
                    continue;
                }
            }

            currentSection += line + "\r\n";
        }

        // Add the final section
        if (!string.IsNullOrWhiteSpace(currentSection))
        {
            try
            {
                sections.Add(
                      new MarkdownSection(
                          currentSection.Trim(),
                          headerStack.Count > 0 ? headerStack.Peek() : "",
                          index++
                      )
                  );
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debugger.Break();
            }
        }

        return sections;
    }
}

