using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace SoftwareCo
{
    public sealed class EventManager
    {
        private static readonly Lazy<EventManager> lazy = new Lazy<EventManager>(() => new EventManager());

        private SoftwareCoPackage package;
        private bool showingStatusText = true;

        public static EventManager Instance { get { return lazy.Value; } }

        private EventManager()
        {
            //
        }

        public void InjectAsyncPackage(SoftwareCoPackage package)
        {
            this.package = package;
        }

        public bool IsShowingStatusText()
        {
            return showingStatusText;
        }

        public void ToggleStatusInfo()
        {
            showingStatusText = !showingStatusText;
            // update the tree view text
            package.RebuildMenuButtonsAsync();

            // update the status bar info
            SessionSummaryManager.Instance.UpdateStatusBarWithSummaryData();
        }

        public async Task CreateCodeTimeEvent(string typeVal, string nameVal, string descriptionVal)
        {
            CodeTimeEvent ctEvent = new CodeTimeEvent(typeVal, nameVal, descriptionVal);
            List<CodeTimeEvent> existingList = GetCodeTimeEventList();
            existingList.Add(ctEvent);

            // create a json array
            JsonArray jsonArray = new JsonArray(existingList.Count);
            foreach (CodeTimeEvent existingEvent in existingList)
            {
                jsonArray.Add(existingEvent.GetAsJson());
            }

            string file = SoftwareCoUtil.getCodeTimeEventsFile();

            if (SoftwareCoUtil.CodeTimeEventsFileExists())
            {
                File.SetAttributes(file, FileAttributes.Normal);
            }

            try
            {
                
                File.WriteAllText(file, jsonArray.ToString(), System.Text.Encoding.UTF8);
            }
            catch (Exception e)
            {
                //
            }
        }

        public async Task SendOfflineEvents()
        {
            List<CodeTimeEvent> existingList = GetCodeTimeEventList();
            if (existingList.Count > 0)
            {
                JsonArray jsonArray = new JsonArray(existingList.Count);
                foreach (CodeTimeEvent existingEvent in existingList)
                {
                    jsonArray.Add(existingEvent.GetAsJson());
                }
                string eventData = jsonArray.ToString();
                HttpResponseMessage response = await SoftwareHttpManager.SendRequestAsync(HttpMethod.Post, "/data/event", eventData);
                if (SoftwareHttpManager.IsOk(response))
                {
                    ClearCodeTimeEventDataSummary();
                }
            }
        }

        public List<CodeTimeEvent> GetCodeTimeEventList()
        {

            List<CodeTimeEvent> ctEvents = new List<CodeTimeEvent>();
            string file = SoftwareCoUtil.getCodeTimeEventsFile();

            if (SoftwareCoUtil.CodeTimeEventsFileExists())
            {
                File.SetAttributes(file, FileAttributes.Normal);
            }

            string eventsData = SoftwareCoUtil.getCodeTimeEventsData();

            // it'll be a map of file to FileChangeInfo objects
            JsonArray jsonArrayObj = (JsonArray)SimpleJson.DeserializeObject(eventsData);
            foreach (JsonObject jsonObj in jsonArrayObj)
            {
                CodeTimeEvent ctEvent = new CodeTimeEvent();
                try
                {
                    ctEvent.CloneFromDictionary(jsonObj);
                }
                catch (Exception e)
                {
                    //
                }

                ctEvents.Add(ctEvent);
            }

            return ctEvents;
        }

        public void ClearCodeTimeEventDataSummary()
        {
            string file = SoftwareCoUtil.getCodeTimeEventsFile();

            if (SoftwareCoUtil.CodeTimeEventsFileExists())
            {
                File.SetAttributes(file, FileAttributes.Normal);
            }

            try
            {
                File.WriteAllText(file, new JsonArray().ToString(), System.Text.Encoding.UTF8);
            }
            catch (Exception e)
            {
                //
            }
        }

    }
}
