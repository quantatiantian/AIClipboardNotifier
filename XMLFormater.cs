using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace AIClipboardNotifier
{
    internal static class XMLFormater
    {
        public static string FormatText(string text)
        {
            if (XMLFormater.IsXml(text))
            {
                return FormatXml(text);
            }
            else if (IsJson(text))
            {
                return FormatJson(text);
            }

            return text;
        }

        public static string FormatXml(string text)
        {
            int indentLevel = 0;
            bool inClosingTag = false;
            bool inSelfClosingTag = false;
            bool inComment = false;
            bool inCData = false;
            bool inDeclaration = false;
            bool inDoctype = false;
            bool inAttribute = false;
            char attributeQuoteChar = '\0';
            System.Text.StringBuilder formattedText = new System.Text.StringBuilder();

            for (int i = 0; i < text.Length; i++)
            {
                char currentChar = text[i];

                if (!inComment && !inCData && !inDeclaration && !inDoctype)
                {
                    if (inAttribute)
                    {
                        formattedText.Append(currentChar);
                        if (currentChar == attributeQuoteChar)
                        {
                            inAttribute = false;
                        }
                        continue;
                    }
                    else if ((currentChar == '\'' || currentChar == '"') &&
                             (formattedText.Length > 0 && formattedText[formattedText.Length - 1] == '='))
                    {
                        inAttribute = true;
                        attributeQuoteChar = currentChar;
                        formattedText.Append(currentChar);
                        continue;
                    }
                }

                if (!inComment && !inCData && !inDeclaration && !inDoctype &&
                    currentChar == '<' && i + 3 < text.Length)
                {
                    // <!--
                    if (text[i + 1] == '!' && text[i + 2] == '-' && text[i + 3] == '-')
                    {
                        inComment = true;
                        formattedText.Append('\n');
                        formattedText.Append(' ', indentLevel * 4);
                    }
                    // <![CDATA[
                    else if (text[i + 1] == '!' && i + 8 < text.Length &&
                             text.Substring(i + 2, 7) == "[CDATA[")
                    {
                        inCData = true;
                        formattedText.Append('\n');
                        formattedText.Append(' ', indentLevel * 4);
                    }
                    // <?xml
                    else if (text[i + 1] == '?' && i + 4 < text.Length &&
                             text.Substring(i + 2, 3).ToLower() == "xml")
                    {
                        inDeclaration = true;
                        formattedText.Append('\n');
                        formattedText.Append(' ', indentLevel * 4);
                    }
                    // <!DOCTYPE
                    else if (text[i + 1] == '!' && i + 8 < text.Length &&
                             text.Substring(i + 2, 7).ToUpper() == "DOCTYPE")
                    {
                        inDoctype = true;
                        formattedText.Append('\n');
                        formattedText.Append(' ', indentLevel * 4);
                    }
                }

                if (inComment && currentChar == '-' && i + 2 < text.Length &&
                    text[i + 1] == '-' && text[i + 2] == '>')
                {
                    inComment = false;
                    formattedText.Append("-->");
                    i += 2;
                    formattedText.Append('\n');
                    formattedText.Append(' ', indentLevel * 4);
                    continue;
                }
                else if (inCData && currentChar == ']' && i + 2 < text.Length &&
                         text[i + 1] == ']' && text[i + 2] == '>')
                {
                    inCData = false;
                    formattedText.Append("]]>");
                    i += 2;
                    formattedText.Append('\n');
                    formattedText.Append(' ', indentLevel * 4);
                    continue;
                }
                else if (inDeclaration && currentChar == '?' && i + 1 < text.Length &&
                         text[i + 1] == '>')
                {
                    inDeclaration = false;
                    formattedText.Append("?>");
                    i += 1;
                    formattedText.Append('\n');
                    formattedText.Append(' ', indentLevel * 4);
                    continue;
                }
                else if (inDoctype && currentChar == '>')
                {
                    inDoctype = false;
                    formattedText.Append('>');
                    formattedText.Append('\n');
                    formattedText.Append(' ', indentLevel * 4);
                    continue;
                }

                if (inComment || inCData || inDeclaration || inDoctype)
                {
                    formattedText.Append(currentChar);
                    continue;
                }

                if (currentChar == '<')
                {
                    if (i + 1 < text.Length && text[i + 1] == '/')
                    {
                        inClosingTag = true;
                        indentLevel = Math.Max(0, indentLevel - 1);
                        formattedText.Append('\n');
                        formattedText.Append(' ', indentLevel * 4);
                    }
                    else
                    {
                        inSelfClosingTag = IsSelfClosingTag(text, i);

                        formattedText.Append('\n');
                        formattedText.Append(' ', indentLevel * 4);

                        if (!inSelfClosingTag)
                        {
                            indentLevel++;
                        }
                    }
                }
                else if (currentChar == '>')
                {
                    if (inSelfClosingTag)
                    {
                        inSelfClosingTag = false;
                    }

                    if (!inSelfClosingTag && !inClosingTag)
                    {
                        formattedText.Append(currentChar);
                        formattedText.Append('\n');
                        formattedText.Append(' ', Math.Max(0, indentLevel) * 4);
                        continue;
                    }

                    inClosingTag = false;
                }

                formattedText.Append(currentChar);
            }

            string result = RemoveEmptyLines(formattedText.ToString());
            return result;
        }

        public static bool IsSelfClosingTag(string text, int startIndex)
        {
            for (int i = startIndex; i < text.Length; i++)
            {
                if (text[i] == '>')
                {
                    return text[i - 1] == '/';
                }
            }
            return false;
        }
        public static string RemoveEmptyLines(string text)
        {
            string[] lines = text.Split(new[] { '\n' }, StringSplitOptions.None);
            System.Text.StringBuilder result = new System.Text.StringBuilder();

            foreach (string line in lines)
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    result.AppendLine(line.TrimEnd());
                }
            }

            return result.ToString();
        }

        public static bool IsJson(string text)
        {
            try
            {
                JsonConvert.DeserializeObject(text);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static string FormatJson(string text)
        {
            try
            {
                var obj = JsonConvert.DeserializeObject(text);
                return JsonConvert.SerializeObject(obj, Newtonsoft.Json.Formatting.Indented);
            }
            catch
            {
                return text;
            }
        }

        public static bool IsXml(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return false;
            }

            string trimmedInput = input.Trim();

            if (!trimmedInput.StartsWith("<") || !trimmedInput.EndsWith(">"))
            {
                return false;
            }

            Stack<string> tagStack = new Stack<string>();
            int index = 0;

            try
            {
                while (index < trimmedInput.Length)
                {
                    if (trimmedInput[index] != '<')
                    {
                        index++;
                        continue;
                    }

                    // Check comment、CDATA、DOCTYPE
                    if (index + 3 < trimmedInput.Length && trimmedInput.Substring(index, 4) == "<!--")
                    {
                        // skip comment
                        int commentEnd = trimmedInput.IndexOf("-->", index + 4);
                        if (commentEnd == -1) return false;
                        index = commentEnd + 3;
                        continue;
                    }
                    else if (index + 8 < trimmedInput.Length && trimmedInput.Substring(index, 9) == "<![CDATA[")
                    {
                        // skip CDATA
                        int cdataEnd = trimmedInput.IndexOf("]]>", index + 9);
                        if (cdataEnd == -1) return false;
                        index = cdataEnd + 3;
                        continue;
                    }
                    else if (index + 8 < trimmedInput.Length && trimmedInput.Substring(index, 9).ToUpper() == "<!DOCTYPE")
                    {
                        // skip DOCTYPE
                        int doctypeEnd = trimmedInput.IndexOf(">", index + 9);
                        if (doctypeEnd == -1) return false;
                        index = doctypeEnd + 1;
                        continue;
                    }
                    else if (index + 1 < trimmedInput.Length && trimmedInput[index + 1] == '?')
                    {
                        // skip namespace declare
                        int declarationEnd = trimmedInput.IndexOf("?>", index + 2);
                        if (declarationEnd == -1) return false;
                        index = declarationEnd + 2;
                        continue;
                    }

                    // deal with end tag
                    if (index + 1 < trimmedInput.Length && trimmedInput[index + 1] == '/')
                    {
                        int tagEnd = trimmedInput.IndexOf('>', index + 2);
                        if (tagEnd == -1) return false;

                        string tagName = ExtractTagName(trimmedInput.Substring(index + 2, tagEnd - (index + 2)));

                        if (tagStack.Count == 0 || tagStack.Pop() != tagName)
                        {
                            return false;
                        }

                        index = tagEnd + 1;
                    }
                    // deal with self-close tag
                    else if (trimmedInput.IndexOf("/>", index) > 0 && trimmedInput.IndexOf("/>", index) < trimmedInput.IndexOf('>', index))
                    {
                        int tagEnd = trimmedInput.IndexOf("/>", index);
                        string tagContent = trimmedInput.Substring(index + 1, tagEnd - (index + 1));
                        string tagName = ExtractTagName(tagContent);

                        index = tagEnd + 2;
                    }
                    // deal with end tag
                    else
                    {
                        int tagEnd = trimmedInput.IndexOf('>', index + 1);
                        if (tagEnd == -1) return false;

                        string tagContent = trimmedInput.Substring(index + 1, tagEnd - (index + 1));
                        string tagName = ExtractTagName(tagContent);

                        tagStack.Push(tagName);
                        index = tagEnd + 1;
                    }
                }

                return tagStack.Count == 0;
            }
            catch
            {
                return false;
            }
        }

        public static string ExtractTagName(string tagContent)
        {
            StringBuilder tagName = new StringBuilder();
            bool inNamespace = false;

            foreach (char c in tagContent)
            {
                if (char.IsWhiteSpace(c)) break;
                if (c == ':')
                {
                    tagName.Clear();
                    inNamespace = true;
                    continue;
                }
                if (inNamespace) continue;

                tagName.Append(c);
            }

            return tagName.ToString();
        }
    }
}