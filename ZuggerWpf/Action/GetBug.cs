using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Reflection;
using log4net;

namespace ZuggerWpf
{
    class GetBug : ActionBase, IActionBase
    {
        private static readonly ILog logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        ZuggerObservableCollection<BugItem> itemsList = null;

        public GetBug(ZuggerObservableCollection<BugItem> zItems)
        {
            itemsList = zItems;
        }

        public override bool Action()
        {
            bool isSuccess = false;
            NewItemCount = 0;

            try
            {
                ApplicationConfig appconfig = IOHelper.LoadIsolatedData();

                string json = WebTools.Download(string.Format("{0}&{1}={2}", appconfig.GetBugUrl, SessionName, SessionID));

                if (!string.IsNullOrEmpty(json) && IsNewJson(json))
                {
                    ItemCollectionBackup.AddRange(itemsList.Select(f => f.ID));
                    itemsList.Clear();

                    var jsObj = JsonConvert.DeserializeObject(json) as JObject;

                    if (jsObj != null && jsObj["status"].Value<string>() == "success")
                    {
                        json = jsObj["data"].Value<string>();

                        jsObj = JsonConvert.DeserializeObject(json) as JObject;

                        if (jsObj["bugs"] != null)
                        {
                            JArray jsArray = (JArray)JsonConvert.DeserializeObject(jsObj["bugs"].ToString());

                            foreach (var j in jsArray)
                            {
                                BugItem bi = new BugItem()
                                {
                                    priority = j["pri"].Value<string>()
                                    ,
                                    Severity = j["severity"].Value<string>()
                                    ,
                                    ID = j["id"].Value<int>()
                                    ,
                                    Title = Util.EscapeXmlTag(j["title"].Value<string>())
                                    ,
                                    OpenDate = j["openedDate"].Value<string>()
                                    ,
                                    LastEdit = j["lastEditedDate"].Value<string>()
                                    ,
                                    Tip = "Bug"
                                };

                                if (!ItemCollectionBackup.Contains(bi.ID))
                                {
                                    NewItemCount = NewItemCount == 0 ? bi.ID : (NewItemCount > 0 ? -2 : NewItemCount - 1);
                                }

                                itemsList.Add(bi);                                
                            }

                            if (OnNewItemArrive != null
                                && NewItemCount != 0)
                            {
                                OnNewItemArrive(ItemType.Bug, NewItemCount);
                            }                            
                        }

                        isSuccess = true;

                        ItemCollectionBackup.Clear();
                    }
                }
            }
            catch (Exception exp)
            {
                logger.Error(string.Format("GetBug Error: {0}", exp.ToString()));                
            }

            return isSuccess;
        }

        #region ActionBaseInterface Members

        public event NewItemArrive OnNewItemArrive;

        #endregion
    }
}
