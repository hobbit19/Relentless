using System;
using Newtonsoft.Json;

namespace Loom.ZombieBattleground.BackendCommunication
{
    public class LoginData
    {
        [JsonProperty(PropertyName = "accessToken")]
        public string accessToken;
    }

    public class RegisterData
    {
        [JsonProperty(PropertyName = "accessToken")]
        public string accessToken;
    }
}
