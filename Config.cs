using Newtonsoft.Json;

namespace HorizonBot
{
    public class Config
    {
        public string Token { get; set; }
        public string Prefix { get; set; }
        public VerificationSettings Verification { get; set; }
        public RoleSelectSettings RoleSelect { get; set; }
        public WordFilterSettings WordFilter { get; set; }

        public class VerificationSettings
        {
            public bool Enabled { get; set; }
            public ulong ChannelID { get; set; }
            public string MessageTitle { get; set; }
            public string MessageDescription { get; set; }
            public ulong RoleID { get; set; }
        }

        public class RoleSelectSettings
        {
            public bool Enabled { get; set; }
            public ulong ChannelID { get; set; }
            public string MessageTitle { get; set; }
            public string MessageDescription { get; set; }
            public Dictionary<string, ulong> Roles { get; set; }
            public bool RemoveReactionRevokesRole { get; set; }
        }

        public class WordFilterSettings
        {
            public bool Enabled { get; set; }
            public bool DeleteMessage { get; set; }
            public bool BanUser { get; set; }
            
            public int DaysOfMessagesToDelete { get; set; }
            public string BanReason { get; set; }
            public HashSet<string> WordList { get; set; }
            public ulong LogChannelID { get; set; }
        }

        public static Config Load()
        {
            string path = "config.json";

            if (!File.Exists(path))
            {
                Console.WriteLine("Config file does not exist. Creating default config file...");
                var defaultConfig = new Config
                {
                    Token = "YOUR_DISCORD_BOT_TOKEN",
                    Prefix = "!",
                    Verification = new VerificationSettings
                    {
                        Enabled = false,
                        ChannelID = 0,
                        MessageTitle = "Welcome to {0}!",
                        MessageDescription = "Please read through the rules and select the :white_check_mark: reaction below to verify.\n\nRules: <rules, etc. here>",
                        RoleID = 0
                    },
                    RoleSelect = new RoleSelectSettings
                    {
                        Enabled = false,
                        ChannelID = 0,
                        MessageTitle = "Thanks for joining {0}!",
                        MessageDescription = "Feel free to select any of the reactions below to assign roles to yourself.\n\n<Role list & info here>",
                        Roles = new Dictionary<string, ulong>()
                        {
                            [":cook:"] = 0,
                            [":skateboard:"] = 0
                        },
                        RemoveReactionRevokesRole = true
                    },
                    WordFilter = new WordFilterSettings
                    {
                        Enabled = false,
                        DeleteMessage = true,
                        BanUser = true,
                        DaysOfMessagesToDelete = 0,
                        BanReason = "Auto-Ban: Language",
                        WordList = new HashSet<string>() { "example1", "example2" },
                        LogChannelID = 0
                    }
                };
                var defaultConfigJson = JsonConvert.SerializeObject(defaultConfig, Formatting.Indented);
                File.WriteAllText(path, defaultConfigJson);

                Console.WriteLine("Please fill out the config.json file with your own information, then restart the bot. Press any key to exit.");
                Console.ReadKey();
                Environment.Exit(0);
            }

            string json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<Config>(json);
        }
    }
}