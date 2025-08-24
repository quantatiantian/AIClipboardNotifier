using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace AIClipboardNotifier
{
    public class ConfigLoader
    {
        public static string endpoint_ollama = "http://localhost:11434/api/generate";//ollama
        public static string endpoint_openwebui = "http://localhost:3000/ollama/api/chat";//openwebui
        public static string endpoint_openai = "http://localhost:3000/api/chat/completions";//openai
        public static string apiKey_openwebui = "sk_";//open-webui
        public static string apiKey_openai = "sk_";//openai-like
        public static string transLang = "chinese";//translation
        public static string prompt = "";
        public static int promptIndex = -1;
        public static double timeout = 30;
        public static string[] promptList = { };
        public static string[] promptTitle = { };
        public static string[] modelList = { };
        public static string[] modelTitle = { };
        public static bool isMonitoring = true;

        public static bool isTranslateEnabled = false;
        public static bool isFormaterEnabled = true;
        public static bool isAIAnythingEnabled = false;

        public static bool useOllama = false;
        public static bool useOpenWebui = false;
        public static bool useOpenAI = false;
        public static string modelForTranslate = "";
        public static int modelIndex = -1;
        public static void LoadConfig()
        {
            try
            {
                string exeDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                Environment.CurrentDirectory = exeDir;
                string filePath = "config.txt";
                string[] lines = File.ReadAllLines(filePath);
                string currentSection = "";

                promptTitle = new string[] { };
                promptList = new string[] { };
                modelTitle = new string[] { };
                modelList = new string[] { };

                foreach (string line in lines)
                {
                    string trimmedLine = line.Trim();

                    if (trimmedLine.StartsWith("#"))
                    {
                        currentSection = trimmedLine;
                    }
                    else if (!string.IsNullOrEmpty(trimmedLine))
                    {
                        string[] keyValue = trimmedLine.Split(new[] { ':' }, 2);
                        if (keyValue.Length == 2)
                        {
                            string key = keyValue[0].Trim();
                            string value = keyValue[1].Trim();

                            switch (currentSection)
                            {
                                case "#ollama":
                                    if (key == "endpoint")
                                    {
                                        endpoint_ollama = value;
                                    }
                                    else if (key == "enable")
                                    {
                                        useOllama = value.Equals("true");
                                    }
                                    break;
                                case "#open-webui":
                                    if (key == "endpoint")
                                    {
                                        endpoint_openwebui = value;
                                    }
                                    else if (key == "apikey")
                                    {
                                        apiKey_openwebui = value;
                                    }
                                    else if (key == "enable")
                                    {
                                        useOpenWebui = value.Equals("true");
                                    }
                                    break;
                                case "#openai-like":
                                    if (key == "endpoint")
                                    {
                                        endpoint_openai = value;
                                    }
                                    else if (key == "apikey")
                                    {
                                        apiKey_openai = value;
                                    }
                                    else if (key == "enable")
                                    {
                                        useOpenAI = value.Equals("true");
                                    }
                                    break;
                                case "#prompt-list":
                                    promptTitle = promptTitle.Append(key).ToArray();
                                    promptList = promptList.Append(value).ToArray();
                                    break;
                                case "#model-list":
                                    modelTitle = modelTitle.Append(key).ToArray();
                                    modelList = modelList.Append(value).ToArray();
                                    break;
                                case "#params":
                                    if (key == "timeout")
                                    {
                                        if (double.TryParse(value, out double parsedTimeout))
                                        {
                                            timeout = parsedTimeout;
                                        }
                                    }
                                    else if (key == "taget language")
                                    {
                                        transLang = value;
                                    }
                                    break;
                                case "#default-settings":
                                    if (key == "provider")
                                    {
                                        if (value.Equals("ollama", StringComparison.OrdinalIgnoreCase))
                                        {
                                            useOllama = true;
                                            useOpenWebui = false;
                                            useOpenAI = false;
                                        }
                                        else if (value.Equals("open-webui", StringComparison.OrdinalIgnoreCase))
                                        {
                                            useOllama = false;
                                            useOpenWebui = true;
                                            useOpenAI = false;
                                        }
                                        else if (value.Equals("openai-like", StringComparison.OrdinalIgnoreCase))
                                        {
                                            useOllama = false;
                                            useOpenWebui = false;
                                            useOpenAI = true;
                                        }
                                    }
                                    else if (key == "model-anything")
                                    {
                                        for (int i = 0; i < modelTitle.Length; i++)
                                        {
                                            if (modelTitle[i].Equals(value, StringComparison.OrdinalIgnoreCase))
                                            {
                                                modelIndex = i;
                                                break;
                                            }
                                        }
                                    }
                                    else if (key == "model-translate")
                                    {
                                        modelForTranslate = value;
                                    }
                                    else if (key == "prompt")
                                    {
                                        for (int i = 0; i < promptTitle.Length; i++)
                                        {
                                            if (promptTitle[i].Equals(value, StringComparison.OrdinalIgnoreCase))
                                            {
                                                promptIndex = i;
                                                break;
                                            }
                                        }
                                    }
                                    else if (key == "AI")
                                    {
                                        if (value.Equals("translate", StringComparison.OrdinalIgnoreCase))
                                        {
                                            isTranslateEnabled = true;
                                            isAIAnythingEnabled = false;
                                        }
                                        else if (value.Equals("anything", StringComparison.OrdinalIgnoreCase))
                                        {
                                            isTranslateEnabled = false;
                                            isAIAnythingEnabled = true;
                                        }
                                    }
                                    break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error reading config file: " + ex.Message);
            }
        }

        public static bool checkAIConfig()
        {
            if (ConfigLoader.modelIndex == -1)
            {
                MessageBox.Show("Please select a model from the Model List menu.");
                return false;
            }

            if ((ConfigLoader.useOllama && !string.IsNullOrEmpty(ConfigLoader.endpoint_ollama)) ||
                (ConfigLoader.useOpenWebui && !string.IsNullOrEmpty(ConfigLoader.endpoint_openwebui)) ||
                (ConfigLoader.useOpenAI && !string.IsNullOrEmpty(ConfigLoader.endpoint_openai)))
            {
                return true;
            }

            return false;
        }
    }
}
