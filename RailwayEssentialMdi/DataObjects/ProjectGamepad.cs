using Newtonsoft.Json.Linq;

namespace RailwayEssentialMdi.DataObjects
{
    public class ProjectGamepad
    {
        public string Guid { get; set; }

        public ProjectGamepad()
        {
            Guid = "--";
        }

        public bool Parse(JToken tkn)
        {
            var o = tkn as JObject;
            if (o == null)
                return false;

            if (o["guid"] != null)
                Guid = o["guid"].ToString();
            
            return true;
        }

        public JObject ToJson()
        {
            var o = new JObject
            {
                ["guid"] = Guid
            };
            return o;
        }
    }
}
