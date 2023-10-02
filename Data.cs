using Newtonsoft.Json;

namespace HorizonBot
{
    public class Data
    {
        public VerificationData Verification { get; set; }
        public RoleSelectData RoleSelect { get; set; }

        public class VerificationData
        {
            public ulong MessageID { get; set; }
        }

        public class RoleSelectData
        {
            public ulong MessageID { get; set; }
        }

        public static Data Load()
        {
            string path = "data.json";

            if (!File.Exists(path))
            {
                var defaultData = new Data
                {
                    Verification = new VerificationData
                    {
                        MessageID = 0,
                    },
                    RoleSelect = new RoleSelectData
                    {
                        MessageID = 0
                    }
                };
                var defaultDataJson = JsonConvert.SerializeObject(defaultData, Formatting.Indented);
                File.WriteAllText(path, defaultDataJson);
            }

            string json = File.ReadAllText(path);
            return JsonConvert.DeserializeObject<Data>(json);
        }
    }
}