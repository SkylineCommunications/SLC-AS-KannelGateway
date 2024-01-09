namespace SendAlarmNotifications_1
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
    using System.Net.Http;
    using System.Text;
    using Newtonsoft.Json;
    using Skyline.DataMiner.Automation;
    using Skyline.DataMiner.Net;
    using Skyline.DataMiner.Net.Messages;

    /// <summary>
    /// Represents a DataMiner Automation script.
    /// </summary>
    public class Script
    {
        private GetUserInfoResponseMessage userInfoResponseMessage;
        private List<string> allTelephoneNumbers;
        private AlarmEventMessage alarmDetails;

        private string ipAddress;
        private string port;
        private string username;
        private string password;

        /// <summary>
        /// The script entry point.
        /// </summary>
        /// <param name="engine">Link with SLAutomation process.</param>
        public void Run(IEngine engine)
        {
            try
            {
                Initialize(engine);

                var alarmInfo = engine.GetScriptParam(65006).Value;
                var alarm = CorrelatedAlarmInfo.FromCorrelatedInfo(alarmInfo);
                engine.GenerateInformation($"DEBUG: {JsonConvert.SerializeObject(alarm, Formatting.Indented)}");

                GetAlarmDetailsMessage alarmMsg = new GetAlarmDetailsMessage(-1, -1, new int[] { alarm.AlarmId });
                var response = engine.SendSLNetMessage(alarmMsg);
                alarmDetails = (response.Length > 0 ? response[0] as AlarmEventMessage : null) ??
                    throw new Exception("Couldn't retrieve the alarm details");
                engine.GenerateInformation($"DEBUG:The alarm description is \"{alarmDetails.Description}\"");

                RetrieveUserInfo(engine);
                FilterOutTelephoneNumbers();
                ComposeUrl(engine);
                //SendGetRequest();
            }
            catch (Exception ex)
            {
                engine.GenerateInformation($"EXCEPTION:{ex.Message}");
            }
        }

        private void Initialize(IEngine engine)
        {
            ipAddress = engine.GetScriptParam("IP Address").Value;
            port = engine.GetScriptParam("Port").Value;
            username = engine.GetScriptParam("Username").Value;
            password = engine.GetScriptParam("Password").Value;

            if (string.IsNullOrWhiteSpace(ipAddress) || string.IsNullOrWhiteSpace(port) || string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                engine.GenerateInformation($"DEBUG: One of the input parameters is not valid");
                return;
            }
        }

        private void RetrieveUserInfo(IEngine engine)
        {
            DMSMessage dmsMessage = engine.SendSLNetSingleResponseMessage(new GetInfoMessage(InfoType.SecurityInfo));

            userInfoResponseMessage = dmsMessage as GetUserInfoResponseMessage;
        }

        private void FilterOutTelephoneNumbers()
        {
            allTelephoneNumbers = new List<string>();
            foreach (var user in userInfoResponseMessage.Users)
            {
                if (!string.IsNullOrWhiteSpace(user.PhoneNumber))
                {
                    allTelephoneNumbers.Add(user.PhoneNumber);
                }
            }
        }

        private string ComposeMessage()
        {
            return $@"Active alarm: {alarmDetails.Severity} - {alarmDetails.ElementName} - {alarmDetails.Description} - {alarmDetails.Value} - {alarmDetails.RootTime}";
        }

        private string ConcatenateTelephoneNumbers()
        {
            return string.Join("%20", allTelephoneNumbers);
        }

        private string ComposeUrl(IEngine engine)
        {
            string telephoneNumbers = ConcatenateTelephoneNumbers();
            string message = ComposeMessage();

            string url = $"http://{ipAddress}:{port}/cgi-bin/sendsms?username={username}&password={password}&to={telephoneNumbers}&text={message}";

            engine.GenerateInformation($"DEBUG:The alarm description is \"{alarmDetails.Description}\"");

            return url;
        }

        //private void SendGetRequest()
        //{
        //    using (var client = new HttpClient())
        //    {
        //        var composedUri = ComposeUrl();

        //        var endPoint = new Uri(composedUri);
        //        var result = client.GetAsync(endPoint).Result;
        //        var temp = result.Content.ReadAsStringAsync().Result;
        //    }
        //}
    }
}