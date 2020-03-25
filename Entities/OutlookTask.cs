using Microsoft.WindowsAzure.Storage.Table;

namespace RegisterTaskWithOutlook.Entities
{

    public class OutlookTask : TableEntity
    {
        public string Name { get; set; }
        public bool IsCurrentTask { get; set; }
    }
}