using System.Runtime.Serialization;

namespace RegisterTaskWithOutlook.Entities
{
    [DataContract]
    public class IncomingWebhookPayload
    {
        
        [DataMember(Name="text")]
        public string Text {get;set;}
    }
}